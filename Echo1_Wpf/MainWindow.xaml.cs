using Echo1.Wpf.Rendering;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Echo1.Wpf;

public partial class MainWindow : Window
{
	private RcsMesh? _mesh;
	private RcsEngine _engine = new();
	private RadarConfig _radar = new();
	private SceneBuilder _builder = new();
	private FreeFlyCamera? _flyCamera;

	private DispatcherTimer _sweepTimer = new() { Interval = TimeSpan.FromMilliseconds(33) }; // ~30 fps
	private DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(16) }; // ~60 fps

	public MainWindow()
	{
		InitializeComponent();
		_sweepTimer.Tick += SweepTick;
		_renderTimer.Tick += RenderTick;
		_flyCamera = new FreeFlyCamera(Viewport);
		_flyCamera.Start();
	}

	private async void LoadModel_Click(object sender, RoutedEventArgs e)
	{
		var dlg = new Microsoft.Win32.OpenFileDialog
		{
			Filter = "3D Models|*.obj;*.stl"
		};
		if (dlg.ShowDialog() != true) return;

		// Load off the UI thread
		_mesh = await Task.Run(() =>
			dlg.FileName.EndsWith(".stl", StringComparison.OrdinalIgnoreCase)
				? StlImporter.Load(dlg.FileName)
				: ObjImporter.Load(dlg.FileName));

		_mesh.BuildLods(new[] { _mesh.Facets.Length / 4, _mesh.Facets.Length / 16 });

		// Initial render
		UpdateScene();
		_renderTimer.Start();
	}

	// Radar sweep tick — update angle, recompute RCS
	private void SweepTick(object? s, EventArgs e)
	{
		_radar.AzimuthDeg = (_radar.AzimuthDeg + _radar.SweepRateDegPerSec * 0.033f) % 360f;
		AzSlider.Value = _radar.AzimuthDeg;
		RecomputeRcs();
	}

	// Render tick — update heatmap geometry
	private void RenderTick(object? s, EventArgs e) => UpdateScene();

	private void RecomputeRcs()
	{
		if (_mesh is null) return;
		Task.Run(() =>
		{
			var result = _engine.Compute(_mesh, _radar);
			Dispatcher.InvokeAsync(() =>
			{
				RcsDbLabel.Content = $"{result.TotalDbsm:F2} dBsm";
				RcsM2Label.Content = $"{result.TotalM2:F4} m²";
			});
		});
	}

	private void UpdateScene()
	{
		if (_mesh is null) return;
		var scene = _builder.BuildHeatmapScene(_mesh, -40f, 10f);
		MeshVisual.Content = scene;
	}

	private void FreqSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
	{
		_radar.FrequencyHz = e.NewValue * 1e9;
		RecomputeRcs();
	}

	private void AzSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
	{
		_radar.AzimuthDeg = (float)e.NewValue;
		RecomputeRcs();
	}

	private void ElSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
	{
		_radar.ElevationDeg = (float)e.NewValue;
		RecomputeRcs();
	}

	private void Sweep_Checked(object s, RoutedEventArgs e)
	{
		if (SweepToggle.IsChecked == true) _sweepTimer.Start();
		else _sweepTimer.Stop();
	}
}