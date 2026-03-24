using System.Numerics;
using System.Windows.Media.Media3D;

namespace Echo1.Wpf.Rendering;

public static class VectorExtensions
{
	public static Point3D ToPoint3D(this Vector3 v)
		=> new(v.X, v.Y, v.Z);
}