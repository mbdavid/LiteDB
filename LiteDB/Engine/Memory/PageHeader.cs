using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent data from header area in page - will be readed from bulk copy from byte[]
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = PAGE_HEADER_SIZE, Pack = 1)]
    internal struct PageHeader
    {
        [MarshalAs(UnmanagedType.U4)] // 00 - 03
        public uint PageID;

        [MarshalAs(UnmanagedType.U1)] // 04 - 04
        public PageType PageType;

        [MarshalAs(UnmanagedType.U4)] // 05 - 08
        public uint PrevPageID;

        [MarshalAs(UnmanagedType.U4)] // 09 - 12
        public uint NextPageID;

        [MarshalAs(UnmanagedType.U1)]
        public byte BlockCount;

        [MarshalAs(UnmanagedType.U1)]
        public byte LastFreeBlock;

        [MarshalAs(UnmanagedType.U4)]
        public uint FreeBytes;

        [MarshalAs(UnmanagedType.U4)]
        public uint FragmentedBytes;

        [MarshalAs(UnmanagedType.U4)]
        public uint ColID;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 12)]
        public byte[] TransactionID;

        [MarshalAs(UnmanagedType.U1)]
        public byte IsConfirmed;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
        public byte[] Reserved;
    }
}