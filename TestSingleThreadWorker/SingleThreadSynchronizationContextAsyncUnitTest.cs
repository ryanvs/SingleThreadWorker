using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace ThreadTools
{
    /// <summary>
    /// Unit tests for SingleThreadSynchronizationContext using async methods
    /// </summary>
    [TestClass]
    public class SingleThreadSynchronizationContextAsyncUnitTest
    {
        async Task<Dictionary<int, int>> TaskYieldProcess()
        {
            Trace.WriteLine("TaskYieldProcess: ManagedThreadId = " + Thread.CurrentThread.ManagedThreadId);
            var d = new Dictionary<int, int>();
            for (int i = 0; i < 10000; i++)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                int count;
                d[id] = d.TryGetValue(id, out count) ? count + 1 : 1;

                await Task.Yield();
            }
            Trace.WriteLine("TaskYieldProcess: Thread count = " + d.Count);
            foreach (var pair in d) Trace.WriteLine(pair);
            return d;
        }


        [TestMethod]
        public async Task<Dictionary<int, int>> TestDefaultSyncContext()
        {
            var result = await TaskYieldProcess();
            Assert.IsTrue(result.Count >= 1, "Should run on more than 1 thread");
            return result;
        }

        [TestMethod]
        public async Task<Dictionary<int, int>> TestSingleThreadSyncContext()
        {
            var prevContext = SynchronizationContext.Current;
            Dictionary<int, int> result;
            try
            {
                var context = new SingleThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(context);
                var t1 = TaskYieldProcess();
                var t2 = t1.ContinueWith(delegate { context.Complete(); }, TaskScheduler.Default);
                result = await t1;
                await t2;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevContext);
            }
            Assert.AreEqual(1, result.Count, "Should run on exactly 1 thread");
            return result;
        }
    }
}
