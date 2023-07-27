using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RelationshipsStudio.Tools
{

    public partial class NativeMethods
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        internal delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsWindowVisible(IntPtr hWnd);


        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
            IntPtr lParam);

#pragma warning disable CA1838 // Avoid 'StringBuilder' parameters for P/Invokes
        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal unsafe static extern int SendMessageTimeout(
            IntPtr hWnd,
            uint uMsg,
            uint wParam,
            StringBuilder? lParam,
            uint fuFlags,
            uint uTimeout,
            void* lpdwResult);

        [LibraryImport("user32.dll", EntryPoint= "SendMessageW")]
        internal static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);

        [LibraryImport("user32.dll", SetLastError = true)]
        internal static partial int GetWindowTextLength(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern long GetWindowText(IntPtr hwnd, StringBuilder lpString, long cch);
#pragma warning restore CA1838 // Avoid 'StringBuilder' parameters for P/Invokes

        public const int WM_COPYDATA = 0x4A;

        [LibraryImport("user32")]
        public static partial int SendMessage(IntPtr hwnd, int wMsg, int wParam, ref COPYDATASTRUCT lParam);
    }
}

