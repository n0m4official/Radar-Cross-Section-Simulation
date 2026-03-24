using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Echo1_Wpf.Controls
{
	public partial class RcsPolarPlot : UserControl
	{
		public void UpdatePlot(IEnumerable<(float Angle, float Rcs)> data)
		{
			PlotCanvas.Children.Clear();
			double centerX = PlotCanvas.ActualWidth / 2;
			double centerY = PlotCanvas.ActualHeight / 2;
			double radius = Math.Min(centerX, centerY) * 0.8;

			Polyline line = new Polyline { Stroke = Brushes.Cyan, StrokeThickness = 1 };

			foreach (var point in data)
			{
				double rad = (point.Angle - 90) * (Math.PI / 180.0);
				// Normalize RCS for display (e.g., -40dB to 40dB range)
				double r = ((point.Rcs + 40) / 80.0) * radius;
				r = Math.Max(0, r);

				line.Points.Add(new System.Windows.Point(
					centerX + r * Math.Cos(rad),
					centerY + r * Math.Sin(rad)));
			}
			PlotCanvas.Children.Add(line);
		}

		private void PlotCanvas_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e) => PlotCanvas.Children.Clear();
	}
}