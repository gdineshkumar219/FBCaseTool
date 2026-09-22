// -------------------------------------------------------------------------------------
// MainWindow.xaml.cs
// Code-behind for the main window; the mode toggle and hosted content live in MainViewModel.
// -------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FBCaseTool;

#region class MainWindow --------------------------------------------------------------------------
/// <summary>Single window that toggles between the Update Case and Fetch Case panes.</summary>
public partial class MainWindow : Window {
   public MainWindow () => InitializeComponent ();

   // Darkens the native title bar to match the dark palette; light mode keeps default chrome.
   protected override void OnSourceInitialized (EventArgs e) {
      base.OnSourceInitialized (e);
      if (!App.IsDark) return;
      nint hwnd = new WindowInteropHelper (this).Handle;
      int on = 1;
      if (DwmSetWindowAttribute (hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof (int)) != 0)
         DwmSetWindowAttribute (hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref on, sizeof (int));
   }

   const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

   [DllImport ("dwmapi.dll")]
   static extern int DwmSetWindowAttribute (nint hwnd, int attr, ref int value, int size);
}
#endregion
