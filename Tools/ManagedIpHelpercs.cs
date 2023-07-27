using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace RelationshipsStudio.Tools
{


    #region Managed IP Helper API

    public class TcpTable : IEnumerable<TcpRow>
    {
        #region Private Fields

        private readonly IEnumerable<TcpRow> tcpRows;

        #endregion

        #region Constructors

        public TcpTable(IEnumerable<TcpRow> tcpRows)
        {
            this.tcpRows = tcpRows;
        }

        #endregion

        #region Public Properties

        public IEnumerable<TcpRow> Rows
        {
            get { return tcpRows; }
        }

        #endregion

        #region IEnumerable<TcpRow> Members

        public IEnumerator<TcpRow> GetEnumerator()
        {
            return tcpRows.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return tcpRows.GetEnumerator();
        }

        #endregion
    }

    public class TcpRow
    {
        #region Private Fields

        private readonly IPEndPoint localEndPoint;
        private readonly IPEndPoint remoteEndPoint;
        private readonly TcpState state;
        private readonly int processId;

        #endregion

        #region Constructors

        public TcpRow(IpHelper.TcpRow tcpRow)
        {
            state = tcpRow.state;
            processId = tcpRow.owningPid;

            int localPort = (tcpRow.localPort1 << 8) + tcpRow.localPort2 + (tcpRow.localPort3 << 24) + (tcpRow.localPort4 << 16);
            long localAddress = tcpRow.localAddr;
            localEndPoint = new IPEndPoint(localAddress, localPort);

            int remotePort = (tcpRow.remotePort1 << 8) + tcpRow.remotePort2 + (tcpRow.remotePort3 << 24) + (tcpRow.remotePort4 << 16);
            long remoteAddress = tcpRow.remoteAddr;
            remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
        }

        #endregion

        #region Public Properties

        public IPEndPoint LocalEndPoint
        {
            get { return localEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return remoteEndPoint; }
        }

        public TcpState State
        {
            get { return state; }
        }

        public int ProcessId
        {
            get { return processId; }
        }

        #endregion
    }

    public static class ManagedIpHelper
    {
        #region Public Methods

        public static TcpTable GetExtendedTcpTable(bool sorted)
        {
            List<TcpRow> tcpRows = new ();

            nint tcpTable = nint.Zero;
            int tcpTableLength = 0;

            if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, sorted, IpHelper.AfInet, IpHelper.TcpTableType.OwnerPidAll, 0) != 0)
            {
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                    if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, IpHelper.AfInet, IpHelper.TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        IpHelper.TcpTable? table = (IpHelper.TcpTable?)Marshal.PtrToStructure(tcpTable, typeof(IpHelper.TcpTable));

                        nint rowPtr = (nint)((long)tcpTable + Marshal.SizeOf(table?.length));
                        for (int i = 0; i < table?.length; ++i)
                        {
#pragma warning disable CS8605 // Unboxing a possibly null value.
                            tcpRows.Add(new TcpRow((IpHelper.TcpRow)Marshal.PtrToStructure(rowPtr, typeof(IpHelper.TcpRow))));
#pragma warning restore CS8605 // Unboxing a possibly null value.
                            rowPtr = (nint)((long)rowPtr + Marshal.SizeOf(typeof(IpHelper.TcpRow)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != nint.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }
            }

            return new TcpTable(tcpRows);
        }

        public static Dictionary<int, TcpRow> GetExtendedTcpDictionary()
        {
            Dictionary<int, TcpRow> tcpRows = new();

            nint tcpTable = nint.Zero;
            int tcpTableLength = 0;

            if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, false, IpHelper.AfInet, IpHelper.TcpTableType.OwnerPidAll, 0) != 0)
            {
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                    if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, IpHelper.AfInet, IpHelper.TcpTableType.OwnerPidAll, 0) == 0)
                    {
#pragma warning disable CS8605 // Unboxing a possibly null value.
                        IpHelper.TcpTable table = (IpHelper.TcpTable)Marshal.PtrToStructure(tcpTable, typeof(IpHelper.TcpTable));
#pragma warning restore CS8605 // Unboxing a possibly null value.

                        nint rowPtr = (nint)((long)tcpTable + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
#pragma warning disable CS8605 // Unboxing a possibly null value.
                            TcpRow row = new ((IpHelper.TcpRow)Marshal.PtrToStructure(rowPtr, typeof(IpHelper.TcpRow)));
#pragma warning restore CS8605 // Unboxing a possibly null value.
                              // HACK: only add first row that is in a Listening state
                            if (row.State == TcpState.Listen)
                            {
                                if (!tcpRows.ContainsKey(row.ProcessId))
                                    tcpRows.Add(row.ProcessId, row);
                            }
                            rowPtr = (nint)((long)rowPtr + Marshal.SizeOf(typeof(IpHelper.TcpRow)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != nint.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }
            }

            return tcpRows;
        }


        #endregion
    }

    #endregion

    #region P/Invoke IP Helper API

    /// <summary>
    /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366073.aspx"/>
    /// </summary>
    public static partial class IpHelper
    {
        #region Public Fields

        public const string DllName = "iphlpapi.dll";
        public const int AfInet = 2;

        #endregion

        #region Public Methods

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa365928.aspx"/>
        /// </summary>
        [LibraryImport(DllName, SetLastError = true)]
        public static partial uint GetExtendedTcpTable(nint tcpTable, ref int tcpTableLength, [MarshalAs(UnmanagedType.Bool)] bool sort, int ipVersion, TcpTableType tcpTableType, int reserved);

        #endregion

        #region Public Enums

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366386.aspx"/>
        /// </summary>
        public enum TcpTableType
        {
            BasicListener,
            BasicConnections,
            BasicAll,
            OwnerPidListener,
            OwnerPidConnections,
            OwnerPidAll,
            OwnerModuleListener,
            OwnerModuleConnections,
            OwnerModuleAll,
        }

        #endregion

        #region Public Structs

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366921.aspx"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TcpTable
        {
            public uint length;
            public TcpRow row;
        }

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366913.aspx"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TcpRow
        {
            public TcpState state;
            public uint localAddr;
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            public uint remoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            public byte remotePort3;
            public byte remotePort4;
            public int owningPid;
        }

        #endregion

        #endregion
    }
}
