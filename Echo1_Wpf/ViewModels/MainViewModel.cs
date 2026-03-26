// Echo1_RcsSimulator\Echo1_Wpf\ViewModels\MainViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Echo1.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string? name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}