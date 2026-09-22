// -------------------------------------------------------------------------------------
// ObservableObject.cs
// Minimal INotifyPropertyChanged base class for the FBCaseTool view-models.
// -------------------------------------------------------------------------------------
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FBCaseTool;

#region class ObservableObject --------------------------------------------------------------------
// Base class that raises PropertyChanged for bound view-model properties.
abstract class ObservableObject : INotifyPropertyChanged {
   public event PropertyChangedEventHandler? PropertyChanged;

   // Sets the field and raises PropertyChanged only when the value actually changes.
   protected bool SetProperty<T> (ref T field, T value, [CallerMemberName] string? name = null) {
      if (EqualityComparer<T>.Default.Equals (field, value)) return false;
      field = value;
      PropertyChanged?.Invoke (this, new (name));
      return true;
   }

   // Raises PropertyChanged for the given (or calling) property name.
   protected void Raise ([CallerMemberName] string? name = null) => PropertyChanged?.Invoke (this, new (name));
}
#endregion
