using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadTools
{
    public interface IOperation : IDisposable
    {
        Type ReturnType { get; }
        Task Task { get; }
        object Execute();
        bool TrySetCanceled();
    }

    public class Operation<T> : IOperation
    {
        private bool _disposed;

        public Type ReturnType
        {
            get { return typeof(T); }
        }

        private Func<T> _func;
        public Func<T> TaskFunc
        {
            get { return _func; }
        }

        private TaskCompletionSource<T> _tcs;
        public TaskCompletionSource<T> CompletionSource
        {
            get { return _tcs; }
        }

        private Task<T> _task;
        public Task Task
        {
            get { return _task; }
        }
        public Task<T> TaskTyped
        {
            get { return _task; }
        }

        private Operation()
        {}

        public Operation(Func<T> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            this._func = func;
            var tcs = new TaskCompletionSource<T>();
            this._tcs = tcs;
            this._task = tcs.Task;
        }

        ~Operation()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_task != null)
                {
                    _task.Dispose();
                    _task = null;
                }
                _disposed = true;
            }
        }

        public T Execute()
        {
            if (TaskFunc == null) throw new InvalidOperationException("TaskFunc is null");

            try
            {
                var result = TaskFunc();
                _tcs.SetResult(result);
                return result;
            }
            catch (Exception ex)
            {
                bool isCanceled = _tcs.Task.IsCanceled;
                if (ex is OperationCanceledException)
                {
                    isCanceled = true;
                } 
                else if (ex is AggregateException)
                {
                    isCanceled = (ex.InnerException is OperationCanceledException);
                }

                if (isCanceled)
                {
                    _tcs.TrySetCanceled();
                }
                else
                {
                    _tcs.SetException(ex);
                }
                // Do not throw since this stops the SingleThreadWorker.Run loop
                return default(T);
            }
        }

        object IOperation.Execute()
        {
            return Execute();
        }

        bool IOperation.TrySetCanceled()
        {
            return _tcs.TrySetCanceled();
        }
    }
}
