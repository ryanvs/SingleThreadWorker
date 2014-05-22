﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ThreadTools
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct INTERFACEINFO
    {
        [MarshalAs(UnmanagedType.IUnknown)]
        public object punk;
        public Guid iid;
        public ushort wMethod;
    }

    [ComImport, ComConversionLoss, InterfaceType((short)1),
    Guid("00000016-0000-0000-C000-000000000046")]
    public interface IMessageFilter
    {
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        uint HandleInComingCall([In] uint dwCallType, [In] IntPtr htaskCaller,
            [In] uint dwTickCount,
            [In, MarshalAs(UnmanagedType.LPArray)] INTERFACEINFO[]
            lpInterfaceInfo);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        uint RetryRejectedCall([In] IntPtr htaskCallee, [In] uint dwTickCount,
            [In] uint dwRejectType);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        uint MessagePending([In] IntPtr htaskCallee, [In] uint dwTickCount,
            [In] uint dwPendingType);
    }
}
