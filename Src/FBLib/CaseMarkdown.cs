// -------------------------------------------------------------------------------------
// CaseMarkdown.cs
// Renders a CaseDetails into the case_details.md markdown document written by CaseFetcher.
// -------------------------------------------------------------------------------------
using System.Text;
using System.Text.RegularExpressions;

namespace FBLib;

#region class CaseMarkdown ------------------------------------------------------------------------
// Builds the markdown representation of a fetched case.
static class CaseMarkdown {
   #region Methods --------------------------------------------------
   // Builds a markdown string representing the case details.
   public static string Build (CaseDetails d) {
      var sb = new StringBuilder ();
      sb.AppendLine ($"# Case Details: CASE-{d.CaseId}").AppendLine ()
        .AppendLine ($"**Title:** {d.Title}")
        .AppendLine ($"**Reporter:** {d.Reporter}")
        .AppendLine ($"**Date Reported:** {d.DateReported}")
        .AppendLine ($"**Priority:** {d.Priority}")
        .AppendLine ($"**Status:** {d.Status}")
        .AppendLine ($"**Case type:** {d.CaseType}").AppendLine ();

      // Structured sections are an additional, extracted view of the case - shown when present.
      if (!d.IsUnstructured)
         sb.AppendLine ("## Problem Statement").AppendLine (Or (d.Description)).AppendLine ()
           .AppendLine ("## Expected Behavior").AppendLine (Or (d.Expected)).AppendLine ()
           .AppendLine ("## Actual Behavior").AppendLine (Or (d.Actual)).AppendLine ()
           .AppendLine ("## Steps to Reproduce").AppendLine (FormatSteps (d.Steps)).AppendLine ();

      // Always include the complete conversation so nothing outside the structured fields is lost.
      sb.AppendLine ("## Full Case Conversation")
        .AppendLine (string.IsNullOrWhiteSpace (d.FullConversation) ? "(No content)" : d.FullConversation);

      sb.AppendLine ()
        .AppendLine ("## Affected Components")
        .AppendLine ($"- Project: {d.Project}")
        .AppendLine ($"- Area: {d.Area}").AppendLine ()
        .AppendLine ("## Environment");
      if (string.IsNullOrWhiteSpace (d.Environ))
         sb.AppendLine ($"- OS: Windows").AppendLine ($"- Version: {Or (d.Version)}");
      else
         sb.AppendLine ($"- OS: {d.Environ}").AppendLine ($"- Version: {Or (d.Version)}");

      sb.AppendLine ()
        .AppendLine ("## Attachments")
        .AppendLine (FormatAttachments (d.Attachments, d.CaseId)).AppendLine ()
        .AppendLine ("## Additional Notes")
        .AppendLine ($"- Assignee: {d.Assignee}")
        .AppendLine ($"- Milestone: {d.Milestone}")
        .AppendLine ($"- FogBugz Category: {d.CategoryRaw}");

      return sb.ToString ();
   }
   #endregion

   #region Implementation -------------------------------------------
   // Provides a default message for empty or whitespace-only fields.
   static string Or (string? v) => string.IsNullOrWhiteSpace (v) ? "(Not provided)" : v;

   // Formats the steps to reproduce, numbering lines that are not already numbered.
   static string FormatSteps (string raw) {
      if (string.IsNullOrWhiteSpace (raw)) return "1. (Not provided)";
      int n = 1;
      return string.Join ("\n", raw.Split ('\n')
         .Select (l => l.Trim ()).Where (l => l.Length > 0)
         .Select (l => Regex.IsMatch (l, @"^\d+[\.\)]\s+") ? l : $"{n++}. {l}"));
   }

   // Lists the attachments, linking each to its local copy next to case_details.md.
   static string FormatAttachments (List<(string Name, string Url)> attachments, string id) =>
      attachments.Count > 0
         ? string.Join ("\n", attachments.Select (a => $"- [{a.Name}]({Uri.EscapeDataString (a.Name)})"))
         : $"- (Check FogBugz case {id} for attachments)";
   #endregion
}
#endregion
