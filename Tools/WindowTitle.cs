using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RelationshipsStudio.Tools
{
    public static class WindowTitle
    {

        //static void Main(string[] args)
        //{
        //    var p = Process.GetProcessById(3484);
        //    var h = p.MainWindowHandle;

        //    string s = GetWindowTextTimeout(h, 100 /*msec*/);

        //}




        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;




        private static List<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                NativeMethods.EnumThreadWindows(thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }


        public static string GetWindowTitle(int procId)
        {
            foreach (var handle in EnumerateProcessWindowHandles(procId))
            {
                if (NativeMethods.IsWindowVisible(handle))
                {
                    var title = GetCaptionOfWindow(handle);
                    if (title.Length > 0) return title;
                }

            }
            return "-";
        }

        /* ====================================== */

        public static string GetWindowTitleTimeout(int procId, uint timeout)
        {
            string? title = string.Empty;
            foreach (var handle in EnumerateProcessWindowHandles(procId))
            {
                try
                {
                    // if there is an issue with the window handle we just
                    // ignore it and skip to the next one in the collection
                    title = GetWindowTextTimeout(handle, timeout);
                }
                catch (Exception)
                {
                    title = string.Empty;
                }
                if (title?.Length > 0) return title;
            }
            return title ?? string.Empty;
        }


        private static unsafe string? GetWindowTextTimeout(IntPtr hWnd, uint timeout)
        {
            int length;
            if (NativeMethods.SendMessageTimeout(hWnd, WM_GETTEXTLENGTH, 0, null, 2, timeout, &length) == 0)
            {
                return null;
            }
            if (length == 0)
            {
                return null;
            }

            StringBuilder sb = new (length + 1);  // leave room for null-terminator
            if (NativeMethods.SendMessageTimeout(hWnd, WM_GETTEXT, (uint)sb.Capacity, sb, 2, timeout, null) == 0)
            {
                return null;
            }

            return sb.ToString();
        }

        private static string GetCaptionOfWindow(IntPtr hwnd)
        {
            string caption = "";
            try
            {
                StringBuilder? windowText;
                int max_length = NativeMethods.GetWindowTextLength(hwnd);
                windowText = new StringBuilder("", max_length + 5);
                NativeMethods.GetWindowText(hwnd, windowText, max_length + 2);

                if (!String.IsNullOrEmpty(windowText.ToString()) && !String.IsNullOrWhiteSpace(windowText.ToString()))
                    caption = windowText.ToString();
            }
            catch (Exception ex)
            {
                caption = ex.Message;
            }
            finally
            {
            }
            return caption;
        }

    }


}
