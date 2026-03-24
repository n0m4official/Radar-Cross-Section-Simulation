using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Echo1.Wpf.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		private float _currentRcs;
		public float CurrentRcs
		{
			get => _currentRcs;
			set { _currentRcs = value; OnPropertyChanged(); }
		}

		private string _status = "Ready";
		public string Status
		{
			get => _status;
			set { _status = value; OnPropertyChanged(); }
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null!)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}