// -------------------------------------------------------------------------------------
// App.xaml.cs
// Entry point for FBCaseTool: parses the case id and options, shows the main window
// (Update / Fetch modes) and returns an exit code (0 done, 1 error, 2 cancelled).
// -------------------------------------------------------------------------------------
using System.Windows;
using Microsoft.Win32;

namespace FBCaseTool;

#region class App ---------------------------------------------------------------------------------
/// <summary>WPF entry point for the FogBugz case tool (Update / Fetch modes).</summary>
public partial class App : Application {
   #region Implementation -------------------------------------------
   // Parses arguments, shows the main window and maps its result to a process exit code.
   protected override void OnStartup (StartupEventArgs e) {
      base.OnStartup (e);
      ApplyTheme ();
      var (caseId, config, draft, fetch) = ParseArgs (e.Args);
      // Both modes share the config and case id; --fetch just selects the initial mode.
      var update = new UpdateCaseViewModel (config, caseId, draft);
      var fetchVm = new FetchCaseVM (config, caseId);
      var mode = fetch ? EMode.Fetch : EMode.Update;
      var main = new MainViewModel (update, fetchVm, mode);
      var win = new MainWindow { DataContext = main };
      update.RequestClose += win.Close;
      fetchVm.RequestClose += win.Close;
      win.ShowDialog ();
      Shutdown ((int)main.Result);
   }

   // True when the app is showing its dark palette; read by MainWindow to darken the chrome.
   public static bool IsDark { get; private set; }

   // Swaps the light colour tokens for the dark set when Windows is in dark mode. Controls
   // bind tokens with DynamicResource, so replacing slot 0 restyles the whole app.
   static void ApplyTheme () {
      IsDark = IsSystemDark ();
      if (!IsDark) return;
      var dicts = Current.Resources.MergedDictionaries;
      dicts[0] = new ResourceDictionary {
         Source = new Uri ("pack://application:,,,/Themes/Colors.Dark.xaml", UriKind.Absolute)
      };
   }

   // Reads the per-user "apps use light theme" flag; treats any failure as light mode.
   static bool IsSystemDark () {
      try {
         using var key = Registry.CurrentUser.OpenSubKey (
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
         return key?.GetValue ("AppsUseLightTheme") is int light && light == 0;
      } catch { return false; }
   }

   // Extracts the case id and the optional --config / --comment-draft values and the --fetch mode flag.
   static (string? CaseId, string? Config, string? Draft, bool Fetch) ParseArgs (string[] args) {
      string? caseId = null, config = null, draft = null;
      bool fetch = false;
      for (int i = 0; i < args.Length; i++) {
         switch (args[i]) {
            case "--config" when i + 1 < args.Length: config = args[++i]; break;
            case "--comment-draft" when i + 1 < args.Length: draft = args[++i]; break;
            case "--fetch": fetch = true; break;
            default: caseId ??= args[i]; break;
         }
      }
      return (caseId, config, draft, fetch);
   }
   #endregion
}
#endregion

#region enum EExit --------------------------------------------------------------------------------
// Process exit codes reported by the application.
enum EExit { Updated = 0, Error = 1, Cancelled = 2 }
#endregion
