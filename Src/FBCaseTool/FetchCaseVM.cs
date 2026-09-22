// -------------------------------------------------------------------------------------
// FetchCaseVM.cs
// View-model for the Fetch Case dialog: holds the case number, the chosen output folder
// (remembered across runs) and the attachment filter, and runs the fetch via FBLib's
// CaseFetcher on the shared FogBugz client.
// -------------------------------------------------------------------------------------
using System.Diagnostics;
using System.IO;
using FBLib;
using Microsoft.Win32;

namespace FBCaseTool;

#region class FetchCaseVM ------------------------------------------------------------------
// Backs the Fetch Case dialog and mediates between the UI and FBLib's CaseFetcher.
sealed class FetchCaseVM : ObservableObject {
   #region Constructor ----------------------------------------------
   public FetchCaseVM (string? configPath, string? caseId = null) {
      mConfigPath = configPath;
      mCaseId = caseId ?? "";
      mSettings = AppSettings.Load ();
      mOutputFolder = string.IsNullOrWhiteSpace (mSettings.LastFetchFolder) ? DefaultFolder : mSettings.LastFetchFolder;
      FetchCommand = new (OnFetch, CanFetch);
      BrowseCommand = new (OnBrowse, () => !IsBusy);
      CancelCommand = new (OnCancel);
      OpenFolderCommand = new (OnOpenFolder, () => mResult != null && !IsBusy);
   }
   #endregion

   #region Properties -----------------------------------------------
   public string CaseId {
      get => mCaseId;
      set { if (SetProperty (ref mCaseId, value)) FetchCommand.RaiseCanExecuteChanged (); }
   }
   public string OutputFolder {
      get => mOutputFolder;
      set { if (SetProperty (ref mOutputFolder, value)) FetchCommand.RaiseCanExecuteChanged (); }
   }
   public IReadOnlyList<EAttachmentMode> AttachmentModes { get; } = [EAttachmentMode.None, EAttachmentMode.All];
   public EAttachmentMode SelectedAttachmentMode { get => mMode; set => SetProperty (ref mMode, value); }

   public string StatusMessage {
      get => mStatus;
      private set { if (SetProperty (ref mStatus, value)) Raise (nameof (HasStatus)); }
   }
   public bool HasStatus => mStatus.Length > 0;

   public string ErrorMessage {
      get => mError;
      private set { if (SetProperty (ref mError, value)) Raise (nameof (HasError)); }
   }
   public bool HasError => mError.Length > 0;

   public bool IsBusy {
      get => mBusy;
      private set {
         if (!SetProperty (ref mBusy, value)) return;
         Raise (nameof (IsIdle));
         FetchCommand.RaiseCanExecuteChanged ();
         BrowseCommand.RaiseCanExecuteChanged ();
         OpenFolderCommand.RaiseCanExecuteChanged ();
      }
   }
   // Inverse of IsBusy, for disabling inputs while a fetch runs.
   public bool IsIdle => !mBusy;

   public RelayCommand FetchCommand { get; }
   public RelayCommand BrowseCommand { get; }
   public RelayCommand CancelCommand { get; }
   public RelayCommand OpenFolderCommand { get; }

   // How the dialog ended, read by the host to set the process exit code (0 fetched, 2 cancelled).
   public EExit Result { get; private set; } = EExit.Cancelled;

   // Raised when the dialog should close.
   public event Action? RequestClose;
   #endregion

   #region Implementation -------------------------------------------
   // Validates the input and fetches the case, keeping the dialog open so the user can retry.
   async void OnFetch () {
      string id = CaseId.Trim ();
      if (!IsPositiveNumber (id)) { ErrorMessage = "Enter a valid case number."; return; }
      string folder = (OutputFolder ?? "").Trim ();
      if (folder.Length == 0) { ErrorMessage = "Choose an output folder."; return; }

      IsBusy = true;
      ErrorMessage = "";
      StatusMessage = "";
      mResult = null;
      try {
         var mode = SelectedAttachmentMode;
         IProgress<string> progress = new Progress<string> (m => StatusMessage = m);
         var result = await Task.Run (() => new CaseFetcher (Client ()).Fetch (id, folder, mode, progress.Report));
         mResult = result;
         Persist (folder);
         Result = EExit.Updated;   // reuse the shared success code (0)
         StatusMessage = $"Fetched case {result.CaseId} to {result.OutputFolder}.{AttachmentSummary (mode, result)}";
      } catch (Exception ex) {
         ErrorMessage = ex.Message;
         StatusMessage = "";
      } finally {
         IsBusy = false;
      }
   }

   // Lets the user pick the output folder, seeding the picker with the current one.
   void OnBrowse () {
      var dlg = new OpenFolderDialog { Title = "Select output folder", Multiselect = false };
      string start = (OutputFolder ?? "").Trim ();
      if (start.Length > 0 && Directory.Exists (start)) dlg.InitialDirectory = start;
      if (dlg.ShowDialog () == true) {
         OutputFolder = dlg.FolderName;
         Persist (dlg.FolderName);
      }
   }

   // Opens the fetched case folder in File Explorer.
   void OnOpenFolder () {
      if (mResult == null) return;
      try {
         Process.Start (new ProcessStartInfo (mResult.OutputFolder) { UseShellExecute = true });
      } catch (Exception ex) {
         ErrorMessage = $"Could not open folder: {ex.Message}";
      }
   }

   // Closes the dialog, keeping a successful fetch's exit code when one has happened.
   void OnCancel () {
      Result = mResult != null ? EExit.Updated : EExit.Cancelled;
      RequestClose?.Invoke ();
   }

   // Fetch is allowed once a valid case number and an output folder are present.
   bool CanFetch () => !IsBusy && IsPositiveNumber (CaseId.Trim ()) && !string.IsNullOrWhiteSpace (OutputFolder);

   // Remembers the folder so it becomes the default the next time the dialog opens.
   void Persist (string folder) {
      if (string.IsNullOrWhiteSpace (folder)) return;
      mSettings.LastFetchFolder = folder;
      mSettings.Save ();
   }

   static bool IsPositiveNumber (string s) => s.Length > 0 && s.All (char.IsDigit);

   // Describes what happened to the attachments, tailored to the chosen mode.
   static string AttachmentSummary (EAttachmentMode mode, CaseFetchResult r) {
      if (r.AttachmentsTotal == 0) return "";
      return mode == EAttachmentMode.None
         ? $" {r.AttachmentsTotal} attachment(s) skipped."
         : $" {r.AttachmentsSaved}/{r.AttachmentsTotal} attachment(s) saved.";
   }

   // Lazily creates the shared FogBugz client (throws FogBugzException on a bad config).
   FogBugzClient Client () => mClient ??= FogBugzClient.Create (mConfigPath);

   // The initial output folder used when nothing has been remembered yet.
   static string DefaultFolder =>
      Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "FBCaseInfo");
   #endregion

   #region Private data ---------------------------------------------
   readonly string? mConfigPath;
   readonly AppSettings mSettings;
   FogBugzClient? mClient;
   CaseFetchResult? mResult;
   string mCaseId, mOutputFolder, mStatus = "", mError = "";
   EAttachmentMode mMode = EAttachmentMode.All;
   bool mBusy;
   #endregion
}
#endregion
