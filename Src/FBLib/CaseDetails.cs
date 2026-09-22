// -------------------------------------------------------------------------------------
// CaseDetails.cs
// The parsed details of a FogBugz case, assembled by CaseFetcher and rendered to
// case_details.md by CaseMarkdown.
// -------------------------------------------------------------------------------------
namespace FBLib;

#region class CaseDetails -------------------------------------------------------------------------
// Holds the fields extracted from a case, plus its attachment list (name + download URL).
sealed class CaseDetails {
   public string CaseId = "", Title = "", Reporter = "", DateReported = "", Priority = "", Status = "",
                 CaseType = "", Assignee = "", Project = "", Area = "", Milestone = "", CategoryRaw = "",
                 Description = "", Steps = "", Expected = "", Actual = "", Environ = "", Version = "",
                 FullConversation = "";
   public bool IsUnstructured;
   public List<(string Name, string Url)> Attachments { get; set; } = [];
}
#endregion
