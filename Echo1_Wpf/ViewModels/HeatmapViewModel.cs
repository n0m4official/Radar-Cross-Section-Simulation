using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Echo1_Wpf.ViewModels;

public class HeatmapViewModel : INotifyPropertyChanged
{
	private float _minDb = -40f;
	private float _maxDb = 10f;

	public float MinDb
	{
		get => _minDb;
		set { _minDb = value; OnPropertyChanged(); }
	}

	public float MaxDb
	{
		get => _maxDb;
		set { _maxDb = value; OnPropertyChanged(); }
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string name = null!)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}