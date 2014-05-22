using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Diagnostics;

namespace ThreadTools
{
    [TestClass]
    public class SingleThreadWorkerUnitTest
    {
        [TestMethod]
        public void BeginAndEndInvokeShouldSucceed()
        {
            bool exceptionThrown = false;
            SingleThreadWorker testThread = null;

            try
            {
                var sw = Stopwatch.StartNew();
                var evt = new ManualResetEventSlim();
                testThread = new SingleThreadWorker();
                var ar = testThread.BeginInvoke(
                    new Action(() =>
                    {
                        Trace.WriteLine("Test 'BeginInvoke' from delegate"); 
                        testThread.Invoke(new Action(() => { Trace.WriteLine("Test 'Invoke' from inner delegate"); Thread.Sleep(1000); }), null);
                        Thread.Sleep(1000);
                        testThread.BeginInvoke(new Action(() => { Trace.WriteLine("Test 'BeginInvoke' from inner delegate"); Thread.Sleep(1000); }), null);
                        Thread.Sleep(1000);
                        testThread.Send(delegate { Trace.WriteLine("Test 'Send' from inner delegate"); Thread.Sleep(1000); }, null);
                        Thread.Sleep(1000);
                        testThread.Post(delegate { Trace.WriteLine("Test 'Post' from inner delegate"); Thread.Sleep(1000); evt.Set(); }, null);
                        Thread.Sleep(1000);
                    }), null);
                testThread.EndInvoke(ar);
                evt.Wait();
                sw.Stop();
                Trace.WriteLine(string.Format("BeginInvoke/EndInvoke took {0} msec", sw.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Trace.WriteLine(string.Format("BeginInvoke/EndInvoke threw an exception: {0} - {1}", ex.GetType().Name, ex.ToString()));
            }
            finally
            {
                if (testThread != null)
                {
                    testThread.Dispose();
                }
            }
            Assert.IsFalse(exceptionThrown, "BeginInvoke/EndInvoke from same thread should succeed");
        }

        [TestMethod]
        public void InvokeFromDifferentThreadShouldFail()
        {
            bool exceptionThrown = false;
            SingleThreadWorker testThread = null;

            try
            {
                var sw = Stopwatch.StartNew();
                testThread = new SingleThreadWorker();
                testThread.Invoke(new Action(() => { Trace.WriteLine("Test 'Invoke' from delegate"); Thread.Sleep(10000); }), null);
                sw.Stop();
                Trace.WriteLine(string.Format("Invoke took {0} msec", sw.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Trace.WriteLine(string.Format("Invoke threw an exception: {0} - {1}", ex.GetType().Name, ex.ToString()));
            }
            finally
            {
                if (testThread != null)
                {
                    testThread.Dispose();
                }
            }
            Assert.IsTrue(exceptionThrown, "Invoke from different thread should throw an exception");
        }

        [TestMethod]
        public void PostShouldDelayTenSeconds()
        {
            var sw = Stopwatch.StartNew();
            var evt = new ManualResetEventSlim();
            SingleThreadWorker testThread = new SingleThreadWorker();
            testThread.Post(delegate { Trace.WriteLine("Test 'Post' from delegate"); Thread.Sleep(10000); evt.Set(); }, null);
            evt.Wait();
            sw.Stop();
            Assert.IsTrue(sw.Elapsed >= TimeSpan.FromSeconds(10), "Post with delay should take at least 10 seconds");
            Trace.WriteLine(string.Format("Post took {0} msec", sw.ElapsedMilliseconds));
            testThread.Dispose();
        }

        [TestMethod]
        public void SendFromDifferentThreadShouldFail()
        {
            bool exceptionThrown = false;
            SingleThreadWorker testThread = null;

            try
            {
                var sw = Stopwatch.StartNew();
                testThread = new SingleThreadWorker();
                testThread.Send(delegate { Trace.WriteLine("Test 'Send' from delegate"); Thread.Sleep(10000); }, null);
                sw.Stop();
                Trace.WriteLine(string.Format("Send took {0} msec", sw.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Trace.WriteLine(string.Format("Send threw an exception: {0} - {1}", ex.GetType().Name, ex.ToString()));
            }
            finally
            {
                if (testThread != null)
                {
                    testThread.Dispose();
                }
            }
            Assert.IsTrue(exceptionThrown, "Send from different thread should throw an exception");
        }

        [TestMethod]
        public void SendFromSameThreadShouldSucceed()
        {
            bool exceptionThrown = false;
            SingleThreadWorker testThread = null;

            try
            {
                var sw = Stopwatch.StartNew();
                var evt = new ManualResetEventSlim();
                testThread = new SingleThreadWorker();
                testThread.Post(
                    delegate {
                        Trace.WriteLine("Test 'Post' from delegate"); 
                        Thread.Sleep(1000);
                        testThread.Send(delegate { Trace.WriteLine("Test 'Send' from inner delegate"); Thread.Sleep(1000); }, null);
                        Thread.Sleep(1000);
                        testThread.Post(delegate { Trace.WriteLine("Test 'Post' from inner delegate"); Thread.Sleep(1000); evt.Set(); }, null);
                        Thread.Sleep(1000);
                        //evt.Set(); // Use event from the Post operation
                    }, null);
                evt.Wait();
                sw.Stop();
                Trace.WriteLine(string.Format("Send took {0} msec", sw.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Trace.WriteLine(string.Format("Send threw an exception: {0} - {1}", ex.GetType().Name, ex.ToString()));
            }
            finally
            {
                if (testThread != null)
                {
                    testThread.Dispose();
                }
            }
            Assert.IsFalse(exceptionThrown, "Send from same thread should succeed");
        }

        [TestMethod]
        public void SendFromSameThreadShouldSucceedSTA()
        {
            bool exceptionThrown = false;
            SingleThreadWorker testThread = null;

            try
            {
                var sw = Stopwatch.StartNew();
                var evt = new ManualResetEventSlim();
                testThread = new SingleThreadWorker(ApartmentState.STA);
                testThread.Post(
                    delegate
                    {
                        Trace.WriteLine("Test 'Post' from delegate");
                        Thread.Sleep(1000);
                        testThread.Send(delegate { Trace.WriteLine("Test 'Send' from inner delegate"); Thread.Sleep(1000); }, null);
                        Thread.Sleep(1000);
                        testThread.Post(delegate { Trace.WriteLine("Test 'Post' from inner delegate"); Thread.Sleep(1000); evt.Set(); }, null);
                        Thread.Sleep(1000);
                        //evt.Set(); // Use event from the Post operation
                    }, null);
                evt.Wait();
                sw.Stop();
                Trace.WriteLine(string.Format("Send took {0} msec", sw.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Trace.WriteLine(string.Format("Send threw an exception: {0} - {1}", ex.GetType().Name, ex.ToString()));
            }
            finally
            {
                if (testThread != null)
                {
                    testThread.Dispose();
                }
            }
            Assert.IsFalse(exceptionThrown, "Send from same thread should succeed");
        }

        [TestMethod]
        public void ManualQueueFuncShouldSucceedSTA()
        {
            bool exceptionThrown = false;
            SingleThreadWorker testThread = null;

            try
            {
                var sw = Stopwatch.StartNew();
                testThread = new SingleThreadWorker(ApartmentState.STA);
                var task = testThread.QueueFunc<string>(
                    () =>
                    {
                        Trace.WriteLine("Test for manual queue Func");
                        Thread.Sleep(1000);
                        testThread.Send(delegate { Trace.WriteLine("Test 'Send' from inner delegate"); Thread.Sleep(1000); }, null);
                        Thread.Sleep(1000);
                        return null;
                    });
                task.Wait(TimeSpan.FromSeconds(10.0));
                sw.Stop();
                Trace.WriteLine(string.Format("Operation took {0} msec", sw.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Trace.WriteLine(string.Format("Exception occurred: {0} - {1}", ex.GetType().Name, ex.ToString()));
            }
            finally
            {
                if (testThread != null)
                {
                    testThread.Dispose();
                }
            }
            Assert.IsFalse(exceptionThrown, "Manual 'QueueFunc' should succeed");
        }

        [TestMethod]
        public void ManualExitThreadShouldSucceedSTA()
        {
            bool exceptionThrown = false;
            SingleThreadWorker testThread = null;

            try
            {
                var sw = Stopwatch.StartNew();
                testThread = new SingleThreadWorker(ApartmentState.STA);
                var task = testThread.QueueFunc<string>(
                    () =>
                    {
                        Trace.WriteLine("Test for manual queue Func");
                        Thread.Sleep(1000);
                        testThread.Send(delegate { Trace.WriteLine("Test 'Send' from inner delegate"); Thread.Sleep(1000); }, null);
                        Thread.Sleep(1000);
                        return null;
                    });
                task.ContinueWith(_ => { testThread.RequestShutdown(); });
                bool success = testThread.Thread.Join(TimeSpan.FromSeconds(10.0));
                sw.Stop();
                Trace.WriteLine(string.Format("Operation took {0} msec", sw.ElapsedMilliseconds));
                Assert.IsTrue(success, "Thread join failed. RequestShutdown didn't work.");
                Assert.IsFalse(testThread.Thread.IsAlive, "Thread is still alive");
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Trace.WriteLine(string.Format("Exception occurred: {0} - {1}", ex.GetType().Name, ex.ToString()));
            }
            finally
            {
                if (testThread != null)
                {
                    testThread.Dispose();
                }
            }
            Assert.IsFalse(exceptionThrown, "Manual 'QueueFunc' should succeed");
        }
    }
}
