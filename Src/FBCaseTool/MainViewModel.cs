// -------------------------------------------------------------------------------------
// MainViewModel.cs
// Shell view-model for the main window: hosts the Update and Fetch view-models and the
// mode toggle that swaps between them, and reports the process exit code.
// -------------------------------------------------------------------------------------
namespace FBCaseTool;

#region class MainViewModel -----------------------------------------------------------------------
// Owns the mode view-models and exposes the currently selected one to the window.
sealed class MainViewModel : ObservableObject {
   #region Constructor ----------------------------------------------
   public MainViewModel (UpdateCaseViewModel update, FetchCaseVM fetch, EMode startMode) {
      Update = update;
      Fetch = fetch;
      mMode = startMode;
   }
   #endregion

   #region Properties -----------------------------------------------
   public UpdateCaseViewModel Update { get; }
   public FetchCaseVM Fetch { get; }

   // The toggle states are mutually exclusive; setting one to true selects that mode.
   public bool IsUpdateMode { get => mMode == EMode.Update; set { if (value) SetMode (EMode.Update); } }
   public bool IsFetchMode { get => mMode == EMode.Fetch; set { if (value) SetMode (EMode.Fetch); } }

   // The view-model shown in the window's content area, chosen by the current mode.
   public object CurrentViewModel => mMode == EMode.Fetch ? Fetch : Update;

   // The window title reflects the active mode.
   public string Title => mMode == EMode.Fetch ? "Fetch FogBugz Case" : "Update FogBugz Case";

   // The process exit code: 0 when either mode completed its action, otherwise 2 (cancelled).
   public EExit Result => Update.Result == EExit.Updated || Fetch.Result == EExit.Updated ? EExit.Updated : EExit.Cancelled;
   #endregion

   #region Implementation -------------------------------------------
   // Switches mode and refreshes the toggle states, title and hosted content.
   void SetMode (EMode mode) {
      if (!SetProperty (ref mMode, mode, nameof (IsUpdateMode))) return;
      Raise (nameof (IsFetchMode));
      Raise (nameof (CurrentViewModel));
      Raise (nameof (Title));
   }
   #endregion

   #region Private data ---------------------------------------------
   EMode mMode;
   #endregion
}
#endregion

#region enum EMode -------------------------------------------------------------------------------
// The window's two mutually-exclusive modes.
enum EMode { Update, Fetch }
#endregion
