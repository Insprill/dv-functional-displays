using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace FunctionalDisplays.Native;

public class User32
{
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.Dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    public static class Helper
    {
        public static bool TryGetRootWindowOfProcess(uint pid, out IntPtr value)
        {
            return GetRootWindows().TryGetValue(pid, out value);
        }

        public static Dictionary<uint, IntPtr> GetRootWindows()
        {
            List<IntPtr> rootWindows = GetChildWindows(IntPtr.Zero);
            Dictionary<uint, IntPtr> dsProcRootWindows = new();
            foreach (IntPtr hWnd in rootWindows)
            {
                if (hWnd == IntPtr.Zero)
                    continue;
                GetWindowThreadProcessId(hWnd, out uint lpdwProcessId);
                if (!dsProcRootWindows.ContainsKey(lpdwProcessId))
                    dsProcRootWindows.Add(lpdwProcessId, hWnd);
            }

            return dsProcRootWindows;
        }

        private static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                Win32Callback childProc = EnumWindow;
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }

            return result;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            ((List<IntPtr>)gch.Target).Add(handle);
            return true;
        }
    }
}
