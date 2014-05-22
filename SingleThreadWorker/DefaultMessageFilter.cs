using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadTools
{
    public class DefaultMessageFilter : IMessageFilter
    {
        private TimeSpan _retryDelay = TimeSpan.FromSeconds(1.0);
        public TimeSpan RetryDelay
        {
            get { return _retryDelay; }
            set { _retryDelay = value; }
        }

        private int _retryCount = 3;
        public int RetryCount
        {
            get { return _retryCount; }
            set { _retryCount = value; }
        }

        private uint _rejectedCount;
        public uint RejectedCount
        {
            get { return _rejectedCount; }
            set { _rejectedCount = value; }
        }

        public uint HandleInComingCall(
             uint dwCallType, IntPtr htaskCaller, uint dwTickCount,
             INTERFACEINFO[] lpInterfaceInfo)
        {
            return 1;
        }

        public uint RetryRejectedCall(
            IntPtr htaskCallee, uint dwTickCount, uint dwRejectType)
        {
            uint retVal = uint.MaxValue;
            ++_rejectedCount;
            if (IntPtr.Size == 8)
                Debug.WriteLine(string.Format("RetryRejectedCall: htaskCallee=0x{0:X8}, dwTickCount={1}, dwRejectType=0x{2:X8} - RejectedCount={3}", htaskCallee.ToInt64(), dwTickCount, dwRejectType, _rejectedCount));
            else
                Debug.WriteLine(string.Format("RetryRejectedCall: htaskCallee=0x{0:X8}, dwTickCount={1}, dwRejectType=0x{2:X8} - RejectedCount={3}", htaskCallee.ToInt32(), dwTickCount, dwRejectType, _rejectedCount));
            //if (MessageBox.Show("retry?", "Alert", MessageBoxButtons.YesNo) == DialogResult.Yes)
            //{
            //    retVal = 1;
            //}
            retVal = (uint)_retryDelay.Milliseconds;
            return retVal;
        }

        public uint MessagePending(
            IntPtr htaskCallee, uint dwTickCount, uint dwPendingType)
        {
            if (IntPtr.Size == 8)
                Debug.WriteLine(string.Format("MessagePending: htaskCallee=0x{0:X8}, dwTickCount={1}, dwPendingType=0x{2:X8} - RejectedCount={3}", htaskCallee.ToInt64(), dwTickCount, dwPendingType, _rejectedCount));
            else
                Debug.WriteLine(string.Format("MessagePending: htaskCallee=0x{0:X8}, dwTickCount={1}, dwPendingType=0x{2:X8} - RejectedCount={3}", htaskCallee.ToInt32(), dwTickCount, dwPendingType, _rejectedCount));
            return 1;
        }
    }
}
