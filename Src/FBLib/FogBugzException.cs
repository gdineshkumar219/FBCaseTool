// -------------------------------------------------------------------------------------
// FogBugzException.cs
// Exception raised when a FogBugz API request fails or the configuration is invalid.
// -------------------------------------------------------------------------------------
namespace FBLib;

#region class FogBugzException --------------------------------------------------------------------
/// <summary>Raised when a FogBugz API request fails or the configuration is invalid.</summary>
public sealed class FogBugzException : Exception {
   /// <summary>Creates the exception with the given message.</summary>
   public FogBugzException (string message) : base (message) { }
}
#endregion
