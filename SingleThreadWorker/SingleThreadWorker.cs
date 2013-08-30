using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadTools
{
    public class SingleThreadWorker : SynchronizationContext, IDisposable, ISynchronizeInvoke
    {
        #region Member variables

        private bool _disposed;
        private bool _initialized;
        protected readonly Thread _thread;
        protected int _threadId;
        protected DateTime _watchDog;
        private TimeSpan _threadJoinTimeOut = TimeSpan.FromSeconds(30.0);
        protected readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        protected readonly BlockingCollection<IOperation> _operations = new BlockingCollection<IOperation>();
        private volatile IOperation _currentOperation;

        #endregion

        #region Events

        public event EventHandler<EventArgs> Disposed;

        public event EventHandler<EventArgs> Exiting;

        public event EventHandler<EventArgs> Initialized;

        #endregion

        #region Properties

        public bool InvokeRequired
        {
            get
            {
                return Thread.CurrentThread.ManagedThreadId != _thread.ManagedThreadId;
            }
        }

        public bool IsDisposed
        {
            get
            {
                return _disposed;
            }
        }

        public bool IsInitialized
        {
            get
            {
                return _initialized;
            }
        }

        public int ManagedThreadId
        {
            get
            {
                return _thread.ManagedThreadId;
            }
        }

        public DateTime WatchDog
        {
            get
            {
                return _watchDog;
            }
        }

        public Thread Thread
        {
            get
            {
                return _thread;
            }
        }

        public bool HasPendingOperations
        {
            get
            {
                return (_operations != null && _operations.Count > 0);
            }
        }

        public bool IsExecuting
        {
            get { return _currentOperation != null; }
        }

        public int OperationCount
        {
            get { return (_operations == null) ? 0 : _operations.Count; }
        }

        public TimeSpan ThreadJoinTimeOut
        {
            get { return _threadJoinTimeOut; }
            set { _threadJoinTimeOut = value; }
        }

        #endregion

        #region Constructor / Destructor

        public SingleThreadWorker()
        {
            _thread = new Thread(this.Run);
            _thread.Name = this.GetType().Name;
            _thread.Start();
        }

        public SingleThreadWorker(string threadName)
        {
            _thread = new Thread(this.Run);
            _thread.Name = threadName;
            _thread.Start();
        }

        ~SingleThreadWorker()
        {
            Dispose(false);
        }

        #endregion

        #region Dispose and shutdown/cleanup

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            RequestShutdown();
            if (_thread.IsAlive)
            {
                bool result = _thread.Join(_threadJoinTimeOut);
                if (result == false)
                {
                    Trace.WriteLine("Dispose: Thread.Join failed");
                    // Disable the call to Abort while debugging?
                    if (System.Diagnostics.Debugger.IsAttached == false)
                    {
                        try
                        {
                            _thread.Abort();
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError("Dispose: Thread.Abort() failed - " + e.ToString());
                        }
                    }
                }
            }

            if (disposing && !_disposed)
            {
                _operations.Dispose();
                try { OnDispose(); }
                catch { }
                _disposed = true;
            }
        }

        public void RequestShutdown()
        {
            if (!_disposed)
            {
                Trace.WriteLine("RequestShutdown");
                _operations.CompleteAdding();
                _cancel.Cancel();

                foreach (var op in _operations)
                    if (_currentOperation != op)
                        op.TrySetCanceled();
            }
        }

        #endregion

        #region Protection and throws

        protected void ThrowIfCrossThreadCall()
        {
            if (_thread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Trace.WriteLine(string.Format("ThreadId: {0} != ManagedThreadId: {1}", _thread.ManagedThreadId, Thread.CurrentThread.ManagedThreadId));
                throw new InvalidOperationException("Cross thread operations not allowed");
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        #endregion

        #region Virtual methods

        protected virtual void OnDispose()
        {
            var handler = Disposed;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnInitialize()
        {
            var handler = Initialized;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnExiting()
        {
            var handler = Exiting;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #endregion

        #region Main thread loop - Run

        protected void Run()
        {
            var prevContext = SynchronizationContext.Current;
            try
            {
                Trace.WriteLine("Entering Run: " + Thread.CurrentThread.ManagedThreadId);
                SynchronizationContext.SetSynchronizationContext(this);

                OnInitialize();
                _initialized = true;

                _watchDog = DateTime.Now;
                IOperation workItem;
                while (_operations.TryTake(out workItem, Timeout.Infinite))
                {
                    _currentOperation = workItem;
                    workItem.Execute();
                    _currentOperation = null;
                    _watchDog = DateTime.Now;
                }

                Trace.WriteLine("Exiting Run");
                _watchDog = DateTime.Now;
                OnExiting();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevContext);
            }
        }

        #endregion

        #region SynchronizationContext

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null) throw new ArgumentNullException("d");
            var op = new Operation<object>(() => { d(state); return null; });
            _operations.Add(op);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (InvokeRequired)
            {
                throw new NotSupportedException("Synchronously sending is not supported from different threads.");
            }
            else
            {
                d(state);
            }
        }

        #endregion

        #region ISynchronizeInvoke Members

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            ThrowIfDisposed();

            var op = new Operation<object>(() => { method.DynamicInvoke(args); return null; });
            _operations.Add(op, _cancel.Token);
            return op.TaskTyped;
        }

        public object EndInvoke(IAsyncResult result)
        {
            return ((Task<object>)result).Result;
        }

        // WARN: This blocks execution!
        public object Invoke(Delegate method, object[] args)
        {
            ThrowIfDisposed();

            if (InvokeRequired)
            {
                throw new NotSupportedException("Synchronously invoke is not supported from different threads.");
            }
            else
            {
                return method.DynamicInvoke(args);
            }
        }

        #endregion
    }
}
