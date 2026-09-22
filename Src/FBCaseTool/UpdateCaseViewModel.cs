// -------------------------------------------------------------------------------------
// UpdateCaseViewModel.cs
// View-model for the Update Case dialog: holds the editable fields and the dropdown lists,
// loads them from FogBugz and performs the update on OK, via the shared FogBugz client.
// -------------------------------------------------------------------------------------
using FBLib;

namespace FBCaseTool;

#region class UpdateCaseViewModel -----------------------------------------------------------------
// Backs the Update Case dialog and mediates between the UI and the FogBugz client.
sealed class UpdateCaseViewModel : ObservableObject {
   #region Constructor ----------------------------------------------
   public UpdateCaseViewModel (string? configPath, string? caseId, string? draft) {
      mConfigPath = configPath;
      mCaseId = caseId ?? "";
      mComment = draft ?? "";
      OkCommand = new (OnOk, CanOk);
      LoadCommand = new (OnLoad, () => !IsBusy && !string.IsNullOrWhiteSpace (CaseId));
      CancelCommand = new (OnCancel);
      // Auto-load when the host supplied a case id; otherwise wait for the user to enter one.
      if (mCaseId.Length > 0) _ = LoadAsync ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public string CaseId {
      get => mCaseId;
      set { if (SetProperty (ref mCaseId, value)) { LoadCommand.RaiseCanExecuteChanged (); OkCommand.RaiseCanExecuteChanged (); } }
   }
   public string CaseTitle { get => mCaseTitle; private set => SetProperty (ref mCaseTitle, value); }
   public string Comment { get => mComment; set => SetProperty (ref mComment, value); }
   public string Assignee { get => mAssignee; set => SetProperty (ref mAssignee, value); }
   public string? SelectedStatus { get => mStatus; set => SetProperty (ref mStatus, value); }
   public IReadOnlyList<string> People { get => mPeople; private set => SetProperty (ref mPeople, value); }
   public IReadOnlyList<string> Statuses { get => mStatuses; private set => SetProperty (ref mStatuses, value); }

   public string ErrorMessage {
      get => mError;
      private set { if (SetProperty (ref mError, value)) Raise (nameof (HasError)); }
   }
   public bool HasError => mError.Length > 0;

   public bool IsBusy {
      get => mBusy;
      private set {
         if (!SetProperty (ref mBusy, value)) return;
         OkCommand.RaiseCanExecuteChanged ();
         LoadCommand.RaiseCanExecuteChanged ();
      }
   }
   public bool CanEdit {
      get => mCanEdit;
      private set { if (SetProperty (ref mCanEdit, value)) OkCommand.RaiseCanExecuteChanged (); }
   }

   public RelayCommand OkCommand { get; }
   public RelayCommand LoadCommand { get; }
   public RelayCommand CancelCommand { get; }

   // How the dialog ended, read by the host to set the process exit code.
   public EExit Result { get; private set; } = EExit.Cancelled;

   // Raised when the dialog should close (after a successful update or on cancel).
   public event Action? RequestClose;
   #endregion

   #region Implementation -------------------------------------------
   // Loads the entered case's values and dropdown lists directly from FogBugz.
   async Task LoadAsync () {
      string id = CaseId.Trim ();
      if (!IsPositiveNumber (id)) { ErrorMessage = "Enter a valid case number."; CanEdit = false; return; }
      IsBusy = true;
      ErrorMessage = "";
      try {
         var info = await Task.Run (() => Client ().QueryCase (id));
         CaseTitle = string.IsNullOrWhiteSpace (info.Title) ? $"Case {id}" : $"Case {id}: {info.Title}";
         Statuses = info.Statuses;
         People = [.. info.People.Select (p => p.Name)];
         mKnownPeople = new (People, StringComparer.OrdinalIgnoreCase);
         Assignee = info.CurrentAssignee;
         SelectedStatus = info.Statuses.FirstOrDefault (s => s.Equals (info.CurrentStatus, StringComparison.OrdinalIgnoreCase)) ?? info.CurrentStatus;
         mLoadedCaseId = id;
         CanEdit = true;
      } catch (Exception ex) {
         ErrorMessage = $"Could not load case {id}: {ex.Message}";
         mLoadedCaseId = "";
         CanEdit = false;
      } finally {
         IsBusy = false;
         OkCommand.RaiseCanExecuteChanged ();
      }
   }

   // Loads the case named in the Case box.
   void OnLoad () => _ = LoadAsync ();

   // Validates the input and updates the case, keeping the dialog open on failure.
   async void OnOk () {
      string assignee = (Assignee ?? "").Trim ();
      string? status = SelectedStatus?.Trim ();
      if (assignee.Length == 0) { ErrorMessage = "Please choose an assignee."; return; }
      // The assignee combo allows type-ahead, so confirm the typed name is a known person.
      if (mKnownPeople.Count > 0 && !mKnownPeople.Contains (assignee)) {
         ErrorMessage = $"'{assignee}' is not a known assignee. Pick a name from the list.";
         return;
      }
      if (string.IsNullOrWhiteSpace (status)) { ErrorMessage = "Please select a status."; return; }

      IsBusy = true;
      ErrorMessage = "";
      try {
         string? comment = string.IsNullOrWhiteSpace (Comment) ? null : Comment;
         await Task.Run (() => Client ().UpdateCase (mLoadedCaseId, "edit", comment, assignee, null, status!, null));
         Result = EExit.Updated;
         RequestClose?.Invoke ();
      } catch (Exception ex) {
         ErrorMessage = ex.Message;
      } finally {
         IsBusy = false;
      }
   }

   // Closes the dialog without changing the case.
   void OnCancel () {
      Result = EExit.Cancelled;
      RequestClose?.Invoke ();
   }

   // OK is allowed only once the case shown in the box has been loaded.
   bool CanOk () => CanEdit && !IsBusy && mLoadedCaseId.Length > 0 && CaseId.Trim () == mLoadedCaseId;

   static bool IsPositiveNumber (string s) => s.Length > 0 && s.All (char.IsDigit);

   // Lazily creates the shared FogBugz client (throws FogBugzException on a bad config).
   FogBugzClient Client () => mClient ??= FogBugzClient.Create (mConfigPath);
   #endregion

   #region Private data ---------------------------------------------
   readonly string? mConfigPath;
   FogBugzClient? mClient;
   string mCaseId, mLoadedCaseId = "";
   string mCaseTitle = "", mComment, mAssignee = "", mError = "";
   string? mStatus;
   IReadOnlyList<string> mPeople = [], mStatuses = [];
   HashSet<string> mKnownPeople = new (StringComparer.OrdinalIgnoreCase);
   bool mBusy, mCanEdit;
   #endregion
}
#endregion
