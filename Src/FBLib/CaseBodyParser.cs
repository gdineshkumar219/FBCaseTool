// -------------------------------------------------------------------------------------
// CaseBodyParser.cs
// Splits an unstructured FogBugz case body into named sections (description, steps,
// expected, actual, environment, version) used to build the structured markdown.
// -------------------------------------------------------------------------------------
using System.Text.RegularExpressions;

namespace FBLib;

#region class CaseBodyParser ----------------------------------------------------------------------
// Parses a case description into sections keyed by heading.
static class CaseBodyParser {
   #region Methods --------------------------------------------------
   // Splits the body into sections, defaulting leading text to "description".
   public static Dictionary<string, string> Parse (string body) {
      var sections = new Dictionary<string, List<string>> ();
      string curKey = "description";
      var curLines = new List<string> ();

      foreach (string line in body.Split ('\n')) {
         var m = sHeadingRe.Match (line);
         if (m.Success) {
            Flush (sections, curKey, curLines);
            curKey = sPatterns.FirstOrDefault (p => Regex.IsMatch (m.Groups[1].Value.Trim (), $"^{p.Pat}$", RegexOptions.IgnoreCase)).Key ?? "description";
            string rest = line[(m.Index + m.Length)..].Trim ();
            curLines = rest.Length > 0 ? [rest] : [];
         } else curLines.Add (line);
      }
      Flush (sections, curKey, curLines);
      return sections.ToDictionary (kv => kv.Key, kv => string.Join ("\n", kv.Value).Trim ());
   }
   #endregion

   #region Implementation -------------------------------------------
   static void Flush (Dictionary<string, List<string>> sec, string key, List<string> lines) {
      if (lines.Count == 0) return;
      sec.TryAdd (key, []);
      sec[key].AddRange (lines);
   }
   #endregion

   #region Private data ---------------------------------------------
   static readonly (string Pat, string Key)[] sPatterns = [
      (@"description",             "description"),
      (@"steps?\s+to\s+reproduce", "steps"),
      (@"steps",                   "steps"),
      (@"expected\s+results?",     "expected"),
      (@"actual\s+results?",       "actual"),
      (@"environment",             "environment"),
      (@"version",                 "version"),
      (@"attached\s+files?",       "attachments_text"),
   ];

   static readonly Regex sHeadingRe = new (
      @"^\s*(" + string.Join ("|", sPatterns.Select (p => $"(?:{p.Pat})")) + @")\s*[:\-]?\s*",
      RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
   #endregion
}
#endregion
