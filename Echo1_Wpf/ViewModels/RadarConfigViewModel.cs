using System.ComponentModel;
using System.Runtime.CompilerServices;
using Echo1.Core.Radar;

namespace Echo1_Wpf.ViewModels;

public class RadarConfigViewModel : INotifyPropertyChanged
{
	private readonly RadarConfig _config;

	public RadarConfigViewModel(RadarConfig config) => _config = config;

	public double FrequencyGhz
	{
		get => _config.FrequencyHz / 1e9;
		set
		{
			_config.FrequencyHz = value * 1e9;
			OnPropertyChanged();
		}
	}

	public float Azimuth
	{
		get => _config.AzimuthDeg;
		set { _config.AzimuthDeg = value; OnPropertyChanged(); }
	}

	public float Elevation
	{
		get => _config.ElevationDeg;
		set { _config.ElevationDeg = value; OnPropertyChanged(); }
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string name = null!)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}