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
    /// Summary description for SingleThreadSynchronizationContext
    /// </summary>
    [TestClass]
    public class SingleThreadSynchronizationContextUnitTest
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
            foreach (var pair in d) Console.WriteLine(pair);
            return d;
        }

        [TestMethod]
        public void DefaultSyncContextShouldRunOnOneOrMoreThreads()
        {
            Trace.WriteLine("DefaultSyncContextShouldRunOnOneOrMoreThreads: ManagedThreadId = " + Thread.CurrentThread.ManagedThreadId);
            var t = Task.Factory.StartNew(() => { return TaskYieldProcess(); });
            t.Wait();
            var innerTask = t.Result;
            innerTask.Wait();
            var result = innerTask.Result;
            Assert.IsTrue(result.Count >= 1, "Should run on 1 or more threads (usually more than 1)");
        }

        [TestMethod]
        public void SingleThreadSyncContextShouldRunOnOneThread1()
        {
            var t = Task.Factory.StartNew(() =>
                {
                    var prevContext = SynchronizationContext.Current;
                    try
                    {
                        Trace.WriteLine("SingleThreadSyncContextShouldRunOnOneThread1: ManagedThreadId = " + Thread.CurrentThread.ManagedThreadId);
                        var context = new SingleThreadSynchronizationContext();
                        SynchronizationContext.SetSynchronizationContext(context);
                        Func<Task<Dictionary<int, int>>> func = async delegate { return await TaskYieldProcess(); };
                        var yieldTask = func();
                        yieldTask.ContinueWith(delegate { context.Complete(); }, TaskScheduler.Default);
                        context.RunOnCurrentThread();
                        bool success = yieldTask.Wait(TimeSpan.FromSeconds(10.0));
                        Assert.IsTrue(success, "Wait for yield method using SingleThreadSynchronizationContext failed");
                        return yieldTask.Result;
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(prevContext);
                    }
                });
            t.Wait();
            var result = t.Result;
            Assert.AreEqual(1, result.Count, "Should run on exactly 1 thread");
        }

        public static void Run(Func<Task> func)
        {
            var prevCtx = SynchronizationContext.Current;
            try
            {
                var syncCtx = new SingleThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncCtx);
                Trace.WriteLine("Run: ManagedThreadId = " + Thread.CurrentThread.ManagedThreadId);

                var t = func();
                t.ContinueWith(
                    delegate { syncCtx.Complete(); }, TaskScheduler.Default);

                syncCtx.RunOnCurrentThread();
                t.GetAwaiter().GetResult();
            }
            finally { SynchronizationContext.SetSynchronizationContext(prevCtx); }
        }

        [TestMethod]
        public void SingleThreadSyncContextShouldRunOnOneThread3()
        {
            Trace.WriteLine("SingleThreadSyncContextShouldRunOnOneThread3: ManagedThreadId = " + Thread.CurrentThread.ManagedThreadId);
            Dictionary<int, int> result = new Dictionary<int, int>();
            Run(async delegate
            {
                result = await TaskYieldProcess();
            });
            Assert.AreEqual(1, result.Count, "Should run on exactly 1 thread");
        }

    }
}
