using Echo1.Core.Engine;
using Echo1.Core.Geometry;
using Echo1.Core.Import;
using Echo1.Core.Radar;
using Echo1.Wpf.Rendering;
using HelixToolkit.Wpf;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Echo1.Wpf;

public partial class MainWindow : Window
{
	private RcsMesh? _mesh;
	private readonly RcsEngine _engine = new();
	private RadarConfig _radar = new();
	private readonly SceneBuilder _builder = new();
	private FreeFlyCamera? _flyCamera;

	// Sweep data (cached for export)
	private double[]? _lastSweepAz;
	private double[]? _lastSweepRcs;
	private double[]? _lastFreqSweepHz;
	private double[]? _lastFreqSweepRcs;

	// Heatmap range
	private float _heatMin = -40f;
	private float _heatMax = 10f;

	// Viewport (constructed in code to avoid XAML parser dependency on HelixToolkit)
	private readonly HelixViewport3D _viewport;
	private readonly ModelVisual3D _meshVisual = new();

	private readonly DispatcherTimer _sweepTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
	private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };

	public MainWindow()
	{
		InitializeComponent();

		_viewport = new HelixViewport3D
		{
			Background = Brushes.Transparent,
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

		// Draw heatmap legend strip
		DrawHeatLegend();
		UpdateSliderLabels();
	}

	// ──────────────────────────────────────────────────────────────────────────
	//  Model loading
	// ──────────────────────────────────────────────────────────────────────────

	private async void LoadModel_Click(object sender, RoutedEventArgs e)
	{
		var dlg = new Microsoft.Win32.OpenFileDialog
		{
			Filter = "3D Models|*.obj;*.stl",
			Title = "Load target geometry"
		};
		if (dlg.ShowDialog() != true) return;

		BtnLoad.IsEnabled = false;
		BtnLoad.Content = "Loading…";

		string path = dlg.FileName;
		try
		{
			_mesh = await Task.Run(() =>
				path.EndsWith(".stl", StringComparison.OrdinalIgnoreCase)
					? StlImporter.Load(path)
					: ObjImporter.Load(path));

			_mesh.BuildLods(new[] { _mesh.Facets.Length / 4, _mesh.Facets.Length / 16 });
			_mesh.BuildEdges();

			MeshInfoLabel.Content = $"{_mesh.Facets.Length:N0} facets · {_mesh.Edges.Length:N0} edges · "
								  + $"{_mesh.Bounds.DiagonalMetres:F1} m diagonal";

			UpdateScene();
			RecomputeRcs();
			_renderTimer.Start();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Failed to load model:\n{ex.Message}", "Load error",
				MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			BtnLoad.IsEnabled = true;
			BtnLoad.Content = "Load OBJ / STL…";
		}
	}

	// ──────────────────────────────────────────────────────────────────────────
	//  RCS computation
	// ──────────────────────────────────────────────────────────────────────────

	private void RecomputeRcs()
	{
		if (_mesh is null) return;
		var meshSnap = _mesh;
		var radarSnap = new RadarConfig
		{
			FrequencyHz = _radar.FrequencyHz,
			AzimuthDeg = _radar.AzimuthDeg,
			ElevationDeg = _radar.ElevationDeg,
			SweepRateDegPerSec = _radar.SweepRateDegPerSec,
			TxPol = _radar.TxPol
		};  // snapshot

		Task.Run(() =>
		{
			var result = _engine.Compute(meshSnap, radarSnap);
			Dispatcher.InvokeAsync(() =>
			{
				RcsDbLabel.Content = $"{result.TotalDbsm:F2} dBsm";
				RcsM2Label.Content = $"{result.TotalM2:F4} m²  ({FormatRcsNick(result.TotalM2)})";
				PoDbLabel.Content = $"{PhysicalOpticsKernel.ToDbsm(result.PoM2):F1} dBsm";
				EdgeDbLabel.Content = $"{PhysicalOpticsKernel.ToDbsm(result.EdgeM2):F1} dBsm";
			});
		});
	}

	private static string FormatRcsNick(double rcsM2) => rcsM2 switch
	{
		> 1000 => $"~{rcsM2 / 1000:F0} km² class",
		> 100 => "ship-class",
		> 10 => "heavy aircraft",
		> 1 => "fighter-class",
		> 0.01 => "stealth/small UAV",
		_ => "insect-scale"
	};

	private void UpdateScene()
	{
		if (_mesh is null) return;
		Dispatcher.InvokeAsync(() =>
		{
			var scene = _builder.BuildHeatmapScene(_mesh, _heatMin, _heatMax);
			_meshVisual.Content = scene;
		});
	}

	// ──────────────────────────────────────────────────────────────────────────
	//  Timers
	// ──────────────────────────────────────────────────────────────────────────

	private void SweepTick(object? s, EventArgs e)
	{
		_radar.AzimuthDeg = (_radar.AzimuthDeg + _radar.SweepRateDegPerSec * 0.033f) % 360f;
		AzSlider.Value = _radar.AzimuthDeg;
		RecomputeRcs();
	}

	private void RenderTick(object? s, EventArgs e) => UpdateScene();

	// ──────────────────────────────────────────────────────────────────────────
	//  Slider / control handlers
	// ──────────────────────────────────────────────────────────────────────────

	private void FreqSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
	{
		_radar.FrequencyHz = e.NewValue * 1e9;
		FreqValueLabel.Content = $"{e.NewValue:F1} GHz";
		RecomputeRcs();
	}

	private void AzSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
	{
		_radar.AzimuthDeg = (float)e.NewValue;
		AzValueLabel.Content = $"{e.NewValue:F1}°";
		RecomputeRcs();
	}

	private void ElSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
	{
		_radar.ElevationDeg = (float)e.NewValue;
		ElValueLabel.Content = $"{e.NewValue:F1}°";
		RecomputeRcs();
	}

	private void SweepRate_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
	{
		_radar.SweepRateDegPerSec = (float)e.NewValue;
		SweepRateLabel.Content = $"{(int)e.NewValue}";
	}

	private void Sweep_Checked(object s, RoutedEventArgs e)
	{
		if (SweepToggle.IsChecked == true) _sweepTimer.Start();
		else _sweepTimer.Stop();
	}

	private void BandPreset_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is string tagStr
			&& double.TryParse(tagStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double ghz))
		{
			FreqSlider.Value = ghz;
		}
	}

	private void Pol_Changed(object sender, RoutedEventArgs e)
	{
		_radar.TxPol = PolVV.IsChecked == true ? Polarisation.VV :
					   PolHH.IsChecked == true ? Polarisation.HH :
												 Polarisation.HV;
		_engine.InvalidateCache();
		RecomputeRcs();
	}

	private void HeatRange_Changed(object sender, RoutedEventArgs e)
	{
		if (float.TryParse(HeatMinBox.Text, out float mn) &&
			float.TryParse(HeatMaxBox.Text, out float mx) && mn < mx)
		{
			_heatMin = mn;
			_heatMax = mx;
			UpdateScene();
		}
	}

	// ──────────────────────────────────────────────────────────────────────────
	//  Full azimuth sweep + polar plot
	// ──────────────────────────────────────────────────────────────────────────

	private async void ComputeFullSweep_Click(object sender, RoutedEventArgs e)
	{
		if (_mesh is null) return;
		var btn = (Button)sender;
		btn.IsEnabled = false;
		btn.Content = "Computing…";

		var mesh = _mesh;
		var radar = _radar;

		try
		{
			var (azDeg, rcsDb) = await Task.Run(() =>
				_engine.SweepAzimuth(mesh, radar, 0, 359, 1.0));

			_lastSweepAz = azDeg;
			_lastSweepRcs = rcsDb;
			DrawPolarPlot(azDeg, rcsDb);
			BtnExport.IsEnabled = true;
		}
		finally
		{
			btn.IsEnabled = true;
			btn.Content = "Compute full sweep";
		}
	}

	private void DrawPolarPlot(double[] azDeg, double[] rcsDb)
	{
		PolarCanvas.Children.Clear();
		if (azDeg.Length == 0) return;

		double w = PolarCanvas.ActualWidth > 0 ? PolarCanvas.ActualWidth : 300;
		double h = PolarCanvas.ActualHeight > 0 ? PolarCanvas.ActualHeight : 200;
		double cx = w / 2, cy = h / 2;
		double r = Math.Min(cx, cy) - 12;

		// Grid circles
		for (int ring = 1; ring <= 4; ring++)
		{
			double rr = r * ring / 4.0;
			var ellipse = new Ellipse
			{
				Width = rr * 2,
				Height = rr * 2,
				Stroke = new SolidColorBrush(Color.FromRgb(0x1A, 0x27, 0x44)),
				StrokeThickness = 0.5
			};
			Canvas.SetLeft(ellipse, cx - rr);
			Canvas.SetTop(ellipse, cy - rr);
			PolarCanvas.Children.Add(ellipse);
		}

		// Normalise RCS values to [0, r]
		double minDb = rcsDb.Min();
		double maxDb = rcsDb.Max();
		double span = maxDb - minDb;
		if (span < 1.0) span = 1.0;

		// Draw polar trace
		var points = new System.Windows.Media.PointCollection();
		for (int i = 0; i < azDeg.Length; i++)
		{
			double az = azDeg[i] * Math.PI / 180.0;
			double norm = (rcsDb[i] - minDb) / span;
			double rr = norm * r;
			points.Add(new System.Windows.Point(cx + rr * Math.Sin(az), cy - rr * Math.Cos(az)));
		}

		var poly = new Polyline
		{
			Points = points,
			Stroke = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
			StrokeThickness = 1.2
		};
		PolarCanvas.Children.Add(poly);

		// Range labels
		AddCanvasText(PolarCanvas, cx + 4, cy - r - 2,
			$"{maxDb:F0} dBsm", 9, "#4FC3F7");
		AddCanvasText(PolarCanvas, cx + 4, cy - 2,
			$"{minDb:F0} dBsm", 9, "#4FC3F7");
	}

	// ──────────────────────────────────────────────────────────────────────────
	//  Frequency sweep
	// ──────────────────────────────────────────────────────────────────────────

	private async void RunFreqSweep_Click(object sender, RoutedEventArgs e)
	{
		if (_mesh is null) return;
		if (!double.TryParse(FreqSweepStart.Text, out double fStartGhz)) return;
		if (!double.TryParse(FreqSweepStop.Text, out double fStopGhz)) return;
		if (fStartGhz >= fStopGhz) return;

		var btn = (Button)sender;
		btn.IsEnabled = false;
		btn.Content = "Running…";

		var mesh = _mesh;
		var radar = _radar;

		try
		{
			var result = await Task.Run(() =>
			{
				int n = 100;
				var freqs = new double[n];
				var rcs = new double[n];
				Parallel.For(0, n, i =>
				{
					double f = (fStartGhz + (fStopGhz - fStartGhz) * i / (n - 1)) * 1e9;
					var r = new RadarConfig
					{
						FrequencyHz = f,
						AzimuthDeg = radar.AzimuthDeg,
						ElevationDeg = radar.ElevationDeg,
						TxPol = radar.TxPol
					};
					freqs[i] = f;
					rcs[i] = _engine.Compute(mesh, r).TotalDbsm;
				});
				return (freqs, rcs);
			});

			_lastFreqSweepHz = result.freqs;
			_lastFreqSweepRcs = result.rcs;
			DrawFreqPlot(result.freqs, result.rcs, fStartGhz, fStopGhz);
		}
		finally
		{
			btn.IsEnabled = true;
			btn.Content = "Run frequency sweep";
		}
	}

	private void DrawFreqPlot(double[] freqHz, double[] rcsDb,
		double fStartGhz, double fStopGhz)
	{
		FreqCanvas.Children.Clear();
		if (freqHz.Length == 0) return;

		double w = FreqCanvas.ActualWidth > 0 ? FreqCanvas.ActualWidth : 300;
		double h = FreqCanvas.ActualHeight > 0 ? FreqCanvas.ActualHeight : 120;
		double pl = 8, pr = 8, pt = 8, pb = 18;

		double minDb = rcsDb.Min();
		double maxDb = rcsDb.Max();
		double span = maxDb - minDb;
		if (span < 1) span = 1;

		var points = new System.Windows.Media.PointCollection();
		for (int i = 0; i < freqHz.Length; i++)
		{
			double x = pl + (freqHz[i] / 1e9 - fStartGhz) / (fStopGhz - fStartGhz) * (w - pl - pr);
			double y = (h - pb) - (rcsDb[i] - minDb) / span * (h - pt - pb);
			points.Add(new System.Windows.Point(x, y));
		}

		FreqCanvas.Children.Add(new Polyline
		{
			Points = points,
			Stroke = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
			StrokeThickness = 1.0
		});

		AddCanvasText(FreqCanvas, pl, h - 14, $"{fStartGhz:F0} GHz", 9, "#8BA5C9");
		AddCanvasText(FreqCanvas, w - pr - 30, h - 14, $"{fStopGhz:F0} GHz", 9, "#8BA5C9");
		AddCanvasText(FreqCanvas, pl, pt, $"{maxDb:F0}", 9, "#4FC3F7");
	}

	// ──────────────────────────────────────────────────────────────────────────
	//  Material assignment
	// ──────────────────────────────────────────────────────────────────────────


	// Actual material application happens in ApplyMaterial_Click; this handler is just a placeholder
	private void MaterialCombo_Changed(object sender, SelectionChangedEventArgs e) { }

	// Actual materials are classified, so we map the combo box selection to known material properties here
	// Will not implement custom material editing in this project due to:
	//		- legality concerns around user-provided material data
	//		- Complexity of accurate EM material characterisation
	//		- Actual material properties, compositions, ingredients, and properties are classified information in many jurisdictions
	private void ApplyMaterial_Click(object sender, RoutedEventArgs e)
	{
		if (_mesh is null) return;

		var mat = (MaterialCombo.SelectedItem as ComboBoxItem)?.Tag as string switch
		{
			"CarbonFoam" => KnownMaterials.CarbonFoam10mm,
			"Ferrite" => KnownMaterials.FerriteTile3mm,
			"Dielectric" => KnownMaterials.DielectricCoating5mm,
			_ => MaterialProperties.PEC
		};

		_mesh.MeshDefaultMaterial = mat;
		_engine.InvalidateCache();
		MaterialStatusLabel.Content = $"Applied: {mat.Name}";
		RecomputeRcs();
	}

	// ──────────────────────────────────────────────────────────────────────────
	//  CSV export
	// ──────────────────────────────────────────────────────────────────────────

	private void ExportCsv_Click(object sender, RoutedEventArgs e)
	{
		if (_lastSweepAz is null || _lastSweepRcs is null) return;

		var dlg = new Microsoft.Win32.SaveFileDialog
		{
			Filter = "CSV files|*.csv",
			FileName = $"RCS_sweep_{_radar.FrequencyHz / 1e9:F1}GHz_{DateTime.Now:yyyyMMdd_HHmm}.csv"
		};
		if (dlg.ShowDialog() != true) return;

		var sb = new StringBuilder();
		sb.AppendLine("# SpecterCS RCS azimuth sweep");
		sb.AppendLine($"# Frequency: {_radar.FrequencyHz / 1e9:F2} GHz");
		sb.AppendLine($"# Elevation: {_radar.ElevationDeg:F1} deg");
		sb.AppendLine($"# Polarisation: {_radar.TxPol}");
		sb.AppendLine($"# Model: {_mesh?.Name ?? "unknown"}");
		sb.AppendLine($"# Facets: {_mesh?.Facets.Length ?? 0}");
		sb.AppendLine("azimuth_deg,rcs_dbsm");

		for (int i = 0; i < _lastSweepAz.Length; i++)
			sb.AppendLine($"{_lastSweepAz[i]:F1},{_lastSweepRcs[i]:F4}");

		File.WriteAllText(dlg.FileName, sb.ToString());
		MaterialStatusLabel.Content = $"Exported {_lastSweepAz.Length} points.";
	}

	// ──────────────────────────────────────────────────────────────────────────
	//  Helpers
	// ──────────────────────────────────────────────────────────────────────────

	private void UpdateSliderLabels()
	{
		FreqValueLabel.Content = $"{FreqSlider.Value:F1} GHz";
		AzValueLabel.Content = $"{AzSlider.Value:F1}°";
		ElValueLabel.Content = $"{ElSlider.Value:F1}°";
		SweepRateLabel.Content = $"{(int)SweepRateSlider.Value}";
	}

	private void DrawHeatLegend()
	{
		// Render heatmap legend as a LinearGradientBrush
		var stops = new GradientStopCollection();
		for (int i = 0; i <= 8; i++)
		{
			float t = i / 8f;
			var c = HeatmapColorMap.Sample(t);
			stops.Add(new GradientStop(c, t));
		}
		HeatLegend.Background = new LinearGradientBrush(stops,
			new System.Windows.Point(0, 0), new System.Windows.Point(1, 0));
	}

	private static void AddCanvasText(Canvas canvas, double x, double y,
		string text, int fontSize, string hexColor)
	{
		var tb = new TextBlock
		{
			Text = text,
			FontSize = fontSize,
			Foreground = new SolidColorBrush(
				(Color)ColorConverter.ConvertFromString(hexColor))
		};
		Canvas.SetLeft(tb, x);
		Canvas.SetTop(tb, y);
		canvas.Children.Add(tb);
	}

	private void Window_KeyDown(object sender, KeyEventArgs e) { }
}
