// -------------------------------------------------------------------------------------
// AppSettings.cs
// Tiny per-user settings store persisted as JSON under %LocalAppData%\FBCaseTool, used
// to remember the last output folder chosen on the Fetch Case dialog.
// -------------------------------------------------------------------------------------
using System.IO;
using System.Text.Json;

namespace FBCaseTool;

#region class AppSettings -------------------------------------------------------------------------
// Remembers user choices across runs; missing or unreadable settings fall back to defaults.
sealed class AppSettings {
   #region Properties -----------------------------------------------
   // The output folder last used to fetch a case.
   public string LastFetchFolder { get; set; } = "";
   #endregion

   #region Methods --------------------------------------------------
   // Loads the settings, returning defaults when the file is absent or unreadable.
   public static AppSettings Load () {
      try {
         if (File.Exists (FilePath))
            return JsonSerializer.Deserialize<AppSettings> (File.ReadAllText (FilePath)) ?? new ();
      } catch { /* fall back to defaults on a missing or corrupt settings file */ }
      return new ();
   }

   // Writes the settings, ignoring IO errors so persistence never blocks the app.
   public void Save () {
      try {
         Directory.CreateDirectory (Path.GetDirectoryName (FilePath)!);
         File.WriteAllText (FilePath, JsonSerializer.Serialize (this, sOptions));
      } catch { /* persistence is best-effort */ }
   }
   #endregion

   #region Private data ---------------------------------------------
   static string FilePath => Path.Combine (
      Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "FBCaseTool", "settings.json");
   static readonly JsonSerializerOptions sOptions = new () { WriteIndented = true };
   #endregion
}
#endregion
