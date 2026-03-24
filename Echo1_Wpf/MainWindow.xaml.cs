using Echo1.Core.Engine;
using Echo1.Core.Geometry;
using Echo1.Core.Import;
using Echo1.Core.Radar;
using Echo1.Wpf.Rendering;
using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Echo1.Wpf;

public partial class MainWindow : Window
{
	private RcsMesh? _mesh;
	private RcsEngine _engine = new();
	private RadarConfig _radar = new();
	private SceneBuilder _builder = new();
	private FreeFlyCamera? _flyCamera;

	// Created in code — no XAML parser dependency on HelixToolkit
	private readonly HelixViewport3D _viewport;
	private readonly ModelVisual3D _meshVisual = new();

	private readonly DispatcherTimer _sweepTimer =
		new() { Interval = TimeSpan.FromMilliseconds(33) };
	private readonly DispatcherTimer _renderTimer =
		new() { Interval = TimeSpan.FromMilliseconds(16) };

	public MainWindow()
	{
		InitializeComponent();

		// Build viewport in code and inject into the Border placeholder
		_viewport = new HelixViewport3D
		{
			Background = System.Windows.Media.Brushes.Transparent,
			ZoomExtentsWhenLoaded = true,
			Camera = new PerspectiveCamera
			{
				Position = new Point3D(0, 0, 50),
				LookDirection = new Vector3D(0, 0, -1),
				UpDirection = new Vector3D(0, 1, 0),
				FieldOfView = 60
			}
		};

		_viewport.Children.Add(new SunLight());
		_viewport.Children.Add(_meshVisual);
		ViewportHost.Child = _viewport;

		_sweepTimer.Tick += SweepTick;
		_renderTimer.Tick += RenderTick;
		_flyCamera = new FreeFlyCamera(_viewport);
	}

	private async void LoadModel_Click(object sender, RoutedEventArgs e)
	{
		var dlg = new Microsoft.Win32.OpenFileDialog
		{
			Filter = "3D Models|*.obj;*.stl"
		};
		if (dlg.ShowDialog() != true) return;

		string path = dlg.FileName;
		_mesh = await Task.Run(() =>
			path.EndsWith(".stl", StringComparison.OrdinalIgnoreCase)
				? StlImporter.Load(path)
				: ObjImporter.Load(path));

		_mesh.BuildLods(new[] { _mesh.Facets.Length / 4, _mesh.Facets.Length / 16 });
		_mesh.BuildEdges();
		UpdateScene();
		_renderTimer.Start();
	}

	private void SweepTick(object? s, EventArgs e)
	{
		_radar.AzimuthDeg = (_radar.AzimuthDeg + _radar.SweepRateDegPerSec * 0.033f) % 360f;
		AzSlider.Value = _radar.AzimuthDeg;
		RecomputeRcs();
	}

	private void RenderTick(object? s, EventArgs e) => UpdateScene();

	private void RecomputeRcs()
	{
		if (_mesh is null) return;
		var meshSnap = _mesh;
		var radarSnap = _radar;
		Task.Run(() =>
		{
			var result = _engine.Compute(meshSnap, radarSnap);
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
		_meshVisual.Content = scene;
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

	private void Window_KeyDown(object sender, KeyEventArgs e) { }
}