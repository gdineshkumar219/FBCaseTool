// -------------------------------------------------------------------------------------
// RelayCommand.cs
// A tiny ICommand implementation that forwards to delegates, used by the dialog buttons.
// -------------------------------------------------------------------------------------
using System.Windows.Input;

namespace FBCaseTool;

#region class RelayCommand ------------------------------------------------------------------------
// Forwards Execute/CanExecute to the supplied delegates.
sealed class RelayCommand : ICommand {
   #region Constructor ----------------------------------------------
   public RelayCommand (Action execute, Func<bool>? canExecute = null) {
      mExecute = execute;
      mCanExecute = canExecute;
   }
   #endregion

   #region Methods --------------------------------------------------
   public event EventHandler? CanExecuteChanged;
   public bool CanExecute (object? parameter) => mCanExecute?.Invoke () ?? true;
   public void Execute (object? parameter) => mExecute ();

   // Asks WPF to re-query CanExecute so button enablement refreshes.
   public void RaiseCanExecuteChanged () => CanExecuteChanged?.Invoke (this, EventArgs.Empty);
   #endregion

   #region Private data ---------------------------------------------
   readonly Action mExecute;
   readonly Func<bool>? mCanExecute;
   #endregion
}
#endregion
