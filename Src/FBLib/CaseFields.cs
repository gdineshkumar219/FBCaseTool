// -------------------------------------------------------------------------------------
// CaseFields.cs
// Normalises raw FogBugz priority / status / category text to a standard set, and formats
// the API's timestamps. Shared by CaseFetcher when building a CaseDetails.
// -------------------------------------------------------------------------------------
using System.Text;

namespace FBLib;

#region class CaseFields --------------------------------------------------------------------------
// Maps raw FogBugz field text to canonical values and formats dates as yyyy-MM-dd.
static class CaseFields {
   #region Methods --------------------------------------------------
   // Maps a FogBugz priority description to the standard set: Immediate, High, Normal, Low, Optional, Later, Never.
   public static string Priority (string r) {
      foreach (EPriority p in Enum.GetValues<EPriority> ())
         if (r.Contains (p.ToString (), StringComparison.OrdinalIgnoreCase) || r.Contains (((int)p).ToString ()))
            return p.ToString ();
      return nameof (EPriority.Normal);
   }

   // Maps FogBugz status text to a canonical status, folding resolved/closed variants into "Resolved"
   // and letting unknown values pass through.
   public static string Status (string r) {
      string key = r.Replace (" ", "");
      foreach (EStatus s in Enum.GetValues<EStatus> ())
         if (key.Equals (s.ToString (), StringComparison.OrdinalIgnoreCase)) return FormatEnumName (s.ToString ());
      string l = r.ToLower ();
      if (new[] { "resolved", "fixed", "closed", "completed" }.Any (l.Contains)) return "Resolved";
      return string.IsNullOrWhiteSpace (r) ? nameof (EStatus.Active) : char.ToUpper (r[0]) + r[1..];
   }

   // Maps FogBugz category text to a canonical case type, letting unknown values pass through.
   public static string Category (string r) {
      string key = r.Replace (" ", "");
      foreach (ECategory c in Enum.GetValues<ECategory> ())
         if (key.Equals (c.ToString (), StringComparison.OrdinalIgnoreCase)) return FormatEnumName (c.ToString ());
      return string.IsNullOrWhiteSpace (r) ? nameof (ECategory.Bug) : char.ToUpper (r[0]) + r[1..];
   }

   // Formats a FogBugz timestamp as yyyy-MM-dd, falling back to the first 10 characters.
   public static string FormatDate (string raw) =>
      DateTime.TryParse (raw, out var dt) ? dt.ToString ("yyyy-MM-dd") : (raw.Length >= 10 ? raw[..10] : raw);
   #endregion

   #region Implementation -------------------------------------------
   // Renders a PascalCase enum name with spaces, e.g. "CodeComplete" -> "Code Complete".
   static string FormatEnumName (string s) {
      var sb = new StringBuilder ();
      foreach (char c in s) {
         if (char.IsUpper (c) && sb.Length > 0) sb.Append (' ');
         sb.Append (c);
      }
      return sb.ToString ();
   }
   #endregion
}
#endregion

#region Enums -------------------------------------------------------------------------------------
// The FogBugz priority scale (value = FogBugz priority number).
enum EPriority { Immediate = 1, High, Normal, Low, Optional, Later, Never }

// The FogBugz categories (PascalCase; rendered with spaces for display).
enum ECategory {
   Bug, Feature, Refinement, Inquiry, ScheduleItem, TestCase, CodeReview,
   SupportTask, Refactoring, ResearchItem, Discussion, PostTask, VerifyFeature, Documentation
}

// The FogBugz workflow statuses (PascalCase; rendered with spaces for display).
enum EStatus { Active, CodeComplete, ForReview, InternalTest, ReadyToShip, ForMerge, MachineTest }
#endregion
