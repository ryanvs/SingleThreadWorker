using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadTools
{
    /// <summary>
    /// Adapted from Stephen Toub at:
    /// http://blogs.msdn.com/b/pfxteam/archive/2012/01/20/10259049.aspx
    /// </summary>
    public class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>>
            m_queue = new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

        private Thread _thread;

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null) throw new ArgumentNullException("d");
            m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (_thread == Thread.CurrentThread)
                d.Invoke(state);
            else
                throw new NotSupportedException("Synchronously sending from different threads is not supported.");
        }

        public void RunOnCurrentThread()
        {
            _thread = Thread.CurrentThread;
            KeyValuePair<SendOrPostCallback, object> workItem;
            while (m_queue.TryTake(out workItem, Timeout.Infinite))
                workItem.Key(workItem.Value);
            _thread = null;
        }

        public void Complete() { m_queue.CompleteAdding(); }
    }
}
