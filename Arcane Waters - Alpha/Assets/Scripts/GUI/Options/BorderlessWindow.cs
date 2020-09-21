using System;
using UnityEngine;
using System.Runtime.InteropServices;

public class BorderlessWindow {
   #region Public Variables

   // If has frame
   public static bool framed = true;

   // Window bar sizes
   public const uint WS_VISIBLE = 0x10000000;
   public const uint WS_POPUP = 0x80000000;
   public const uint WS_BORDER = 0x00800000;
   public const uint WS_OVERLAPPED = 0x00000000;
   public const uint WS_CAPTION = 0x00C00000;
   public const uint WS_SYSMENU = 0x00080000;
   public const uint WS_THICKFRAME = 0x00040000;
   public const uint WS_MINIMIZEBOX = 0x00020000;
   public const uint WS_MAXIMIZEBOX = 0x00010000;
   public const uint WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;

   // The windows title bar value
   public const int GWL_STYLE = -16;

   #endregion

   public static void setBorderless () {
      IntPtr hwnd = GetActiveWindow();
      SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
      framed = false;
   }

   public static void setBorder () {
      IntPtr hwnd = GetActiveWindow();
      SetWindowLong(hwnd, GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);
      framed = true;
   }

   public static void moveWindowPos (Vector2 posDelta, int newWidth, int newHeight) {
      var hwnd = GetActiveWindow();

      var windowRect = GetWindowRect(hwnd, out WinRect winRect);

      var x = winRect.left + (int) posDelta.x;
      var y = winRect.top - (int) posDelta.y;
      MoveWindow(hwnd, x, y, newWidth, newHeight, false);
   }

   #region Private Variables

   // DLL Functions
   [DllImport("user32.dll")]
   private static extern IntPtr GetActiveWindow ();
   [DllImport("user32.dll")]
   private static extern int SetWindowLong (IntPtr hWnd, int nIndex, uint dwNewLong);
   [DllImport("user32.dll")]
   private static extern bool ShowWindow (IntPtr hwnd, int nCmdShow);
   [DllImport("user32.dll")]
   private static extern bool MoveWindow (IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);
   [DllImport("user32.dll")]
   private static extern bool GetWindowRect (IntPtr hwnd, out WinRect lpRect);
   private struct WinRect { public int left, top, right, bottom; }

   #endregion
}