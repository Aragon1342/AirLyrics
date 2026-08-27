using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AirLyrics.App.Native
{
    /// <summary>
    /// Utilidad para gestionar el modo Ghost (Click-Through) usando Win32 API.
    /// </summary>
    public static class WindowGhostHelper
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(hWnd, nIndex)
                : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        /// <summary>
        /// Aplica o remueve las banderas WS_EX_TRANSPARENT para permitir que los clics del mouse
        /// atraviesen la ventana hacia las aplicaciones que están por debajo.
        /// </summary>
        public static void SetGhostMode(Window window, bool isGhost)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var currentExStyle = GetWindowLong(hwnd, GWL_EXSTYLE).ToInt64();

            if (isGhost)
            {
                var newExStyle = currentExStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED;
                SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(newExStyle));
            }
            else
            {
                var newExStyle = currentExStyle & ~WS_EX_TRANSPARENT;
                SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(newExStyle));
            }
        }
    }
}
