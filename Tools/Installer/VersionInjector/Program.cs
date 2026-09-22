// ╔═╦╗
// ║╬╠╬╦╗ VersionInjector
// ║╔╣╠║╣ Extracts the AssemblyVersion from FBCaseTool and injects it into the installer script
// ╚╝╚╩╩╝ ──────────────────────────────────────────────────────────────────────────────────────────
using System.Text.RegularExpressions;
namespace VersionInjector;

class Program {
   // Run from the repository root (as Ship.bat does): reads the app version, regenerates the .iss.
   static void Main () {
      if (!GetVersion ()) {
         Console.ForegroundColor = ConsoleColor.Red;
         Console.WriteLine ("\nERROR: Could not read AssemblyVersion from AssemblyInfo.cs\n");
         Console.ResetColor ();
         Environment.Exit (1);
      }
      UpdateISS ();
   }

   // Reads the AssemblyVersion (Year.Month.Build.Revision) from the app's AssemblyInfo.cs.
   static bool GetVersion () {
      var m = Regex.Match (File.ReadAllText (AssemblyInfo), @"AssemblyVersion\s*\(""(\d+)\.(\d+)\.(\d+)\.(\d+)""\)");
      if (!m.Success) return false;
      int year = int.Parse (m.Groups[1].Value), month = int.Parse (m.Groups[2].Value),
          build = int.Parse (m.Groups[3].Value), revision = int.Parse (m.Groups[4].Value);
      // Drop a zero revision so the installer version reads e.g. 2026.8.1 rather than 2026.8.1.0.
      sVersion = $"{year}.{month}.{build}" + (revision > 0 ? $".{revision}" : "");
      return true;
   }
   static string sVersion = "";

   // Regenerates the installer script from the template, substituting the {Version} placeholder.
   static void UpdateISS () {
      string text = File.ReadAllText (Template).Replace ("{Version}", sVersion);
      File.WriteAllText (Output, text);
      Console.WriteLine ($"VersionInjector: installer version {sVersion}");
   }

   const string AssemblyInfo = @"Src\FBCaseTool\Properties\AssemblyInfo.cs";
   const string Template = @"Tools\Installer\FBCaseTool.template.iss";
   const string Output = @"Tools\Installer\FBCaseTool.iss";
}
