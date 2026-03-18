using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Echo1.Wpf.Rendering;

/// <summary>
/// WASD + mouse-look free-fly camera controller for HelixViewport3D.
/// Attach via FreeFlyCamera.Attach(viewport).
/// </summary>
public sealed class FreeFlyCamera
{
	private readonly HelixViewport3D _viewport;
	private Point _lastMouse;
	private bool _rightDown;
	private readonly DispatcherTimer _timer;

	private const float Speed = 0.5f;  // units/frame
	private const float MouseSens = 0.3f;

	public FreeFlyCamera(HelixViewport3D viewport)
	{
		_viewport = viewport;
		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16),
						DispatcherPriority.Render,
						Tick, viewport.Dispatcher);

		viewport.MouseRightButtonDown += (s, e) =>
		{ _rightDown = true; _lastMouse = e.GetPosition(viewport); };
		viewport.MouseRightButtonUp += (_, _) => _rightDown = false;
		viewport.MouseMove += OnMouseMove;
		viewport.PreviewKeyDown += OnKeyDown;
	}

	public void Start() => _timer.Start();
	public void Stop() => _timer.Stop();

	private void Tick(object s, EventArgs e) { /* movement applied in OnKeyDown */ }

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		var cam = (PerspectiveCamera)_viewport.Camera;
		var look = Vector3D.Normalize(cam.LookDirection);
		var right = Vector3D.CrossProduct(look, cam.UpDirection);
		right = Vector3D.Normalize(right);

		float spd = Keyboard.IsKeyDown(Key.LeftShift) ? Speed * 4f : Speed;

		cam.Position += e.Key switch
		{
			Key.W => look * spd,
			Key.S => -look * spd,
			Key.A => -right * spd,
			Key.D => right * spd,
			Key.Q => cam.UpDirection * spd,
			Key.E => -cam.UpDirection * spd,
			_ => default
		};
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (!_rightDown) return;
		var pos = e.GetPosition(_viewport);
		var delta = pos - _lastMouse;
		_lastMouse = pos;

		var cam = (PerspectiveCamera)_viewport.Camera;
		var look = cam.LookDirection;

		// Yaw
		var yaw = new AxisAngleRotation3D(cam.UpDirection, -delta.X * MouseSens);
		look = new RotateTransform3D(yaw).Transform(look);

		// Pitch
		var right = Vector3D.CrossProduct(look, cam.UpDirection);
		var pitch = new AxisAngleRotation3D(right, -delta.Y * MouseSens);
		look = new RotateTransform3D(pitch).Transform(look);

		cam.LookDirection = look;
	}
}