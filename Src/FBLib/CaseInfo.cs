// -------------------------------------------------------------------------------------
// CaseInfo.cs
// Models returned by FogBugzClient.QueryCase: a case's current fields plus the statuses
// and people needed to edit it.
// -------------------------------------------------------------------------------------
namespace FBLib;

#region class CaseInfo ----------------------------------------------------------------------------
/// <summary>Current field values plus the available statuses and people for a FogBugz case.</summary>
public sealed class CaseInfo {
   /// <summary>The case number.</summary>
   public string CaseId { get; init; } = "";
   /// <summary>The case title.</summary>
   public string Title { get; init; } = "";
   /// <summary>The case's current status name.</summary>
   public string CurrentStatus { get; init; } = "";
   /// <summary>The full name of the case's current assignee.</summary>
   public string CurrentAssignee { get; init; } = "";
   /// <summary>The case category id (statuses are per category).</summary>
   public string Category { get; init; } = "";
   /// <summary>The status names available for the case's category.</summary>
   public IReadOnlyList<string> Statuses { get; init; } = [];
   /// <summary>The people who can be assigned the case.</summary>
   public IReadOnlyList<Person> People { get; init; } = [];
}
#endregion

#region class Person ------------------------------------------------------------------------------
/// <summary>A FogBugz person offered as an assignee candidate.</summary>
public sealed class Person {
   /// <summary>The person's full name.</summary>
   public string Name { get; init; } = "";
   /// <summary>The person's email address.</summary>
   public string Email { get; init; } = "";
}
#endregion
