using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;

namespace Echo1.Wpf.Rendering;

public sealed class FreeFlyCamera
{
	private readonly HelixViewport3D _viewport;
	private Point _lastMouse;
	private bool _rightDown;

	private const double Speed = 0.5;
	private const double MouseSens = 0.3;

	public FreeFlyCamera(HelixViewport3D viewport)
	{
		_viewport = viewport;

		viewport.MouseRightButtonDown += (_, e) =>
		{
			_rightDown = true;
			_lastMouse = e.GetPosition(viewport);
			viewport.CaptureMouse();
		};
		viewport.MouseRightButtonUp += (_, _) =>
		{
			_rightDown = false;
			viewport.ReleaseMouseCapture();
		};
		viewport.MouseMove += OnMouseMove;
		viewport.PreviewKeyDown += OnKeyDown;
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		var cam = (PerspectiveCamera)_viewport.Camera;
		var look = cam.LookDirection;
		look = ScaleVector(look, 1.0 / look.Length); // manual normalize
		var right = Vector3D.CrossProduct(look, cam.UpDirection);
		right = ScaleVector(right, 1.0 / right.Length);

		double spd = Keyboard.IsKeyDown(Key.LeftShift) ? Speed * 4 : Speed;

		Vector3D delta = e.Key switch
		{
			Key.W => ScaleVector(look, spd),
			Key.S => ScaleVector(look, -spd),
			Key.A => ScaleVector(right, -spd),
			Key.D => ScaleVector(right, spd),
			Key.Q => ScaleVector(cam.UpDirection, spd),
			Key.E => ScaleVector(cam.UpDirection, -spd),
			_ => default
		};

		cam.Position = new Point3D(
			cam.Position.X + delta.X,
			cam.Position.Y + delta.Y,
			cam.Position.Z + delta.Z);
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (!_rightDown) return;
		var pos = e.GetPosition(_viewport);
		var delta = pos - _lastMouse;
		_lastMouse = pos;

		var cam = (PerspectiveCamera)_viewport.Camera;
		var look = cam.LookDirection;

		// Yaw around world up
		var yaw = new AxisAngleRotation3D(cam.UpDirection, -delta.X * MouseSens);
		look = new RotateTransform3D(yaw).Transform(look);

		// Pitch around camera right
		var right = Vector3D.CrossProduct(look, cam.UpDirection);
		var pitch = new AxisAngleRotation3D(right, -delta.Y * MouseSens);
		look = new RotateTransform3D(pitch).Transform(look);

		cam.LookDirection = look;
	}

	private static Vector3D ScaleVector(Vector3D v, double scale)
		=> new(v.X * scale, v.Y * scale, v.Z * scale);
}