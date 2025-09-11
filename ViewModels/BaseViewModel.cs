using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaInstituto.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? property = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(property);
            return true;
        }
    }
}
