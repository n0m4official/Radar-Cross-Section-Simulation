// Echo1_RcsSimulator\Echo1_Core\Geometry\Vector3d.cs
namespace Echo1.Core.Geometry
{
	public record struct Vector3d(double X, double Y, double Z)
	{
		public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		public static Vector3d operator -(Vector3d a, Vector3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
	}
}