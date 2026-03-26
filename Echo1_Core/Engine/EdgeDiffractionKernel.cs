// Echo1_RcsSimulator\Echo1_Core\Engine\EdgeDiffractionKernel.cs
using System.Numerics;

namespace Echo1.Core.Engine;

public static class EdgeDiffractionKernel
{
	/// <summary>
	/// Ufimtsev PTD wedge diffraction (simplified form)
	/// </summary>
	public static Complex DiffractEdge(
		Vector3 a,
		Vector3 b,
		Vector3 incidentDir,
		Vector3 scatterDir,
		float wedgeAngle,
		double k)
	{
		// Edge direction
		Vector3 edgeDir = Vector3.Normalize(b - a);

		// Projection of incident + scatter onto plane perpendicular to edge
		Vector3 projInc = incidentDir - Vector3.Dot(incidentDir, edgeDir) * edgeDir;
		Vector3 projSca = scatterDir - Vector3.Dot(scatterDir, edgeDir) * edgeDir;

		if (projInc.LengthSquared() < 1e-10f ||
			projSca.LengthSquared() < 1e-10f)
			return Complex.Zero;

		projInc = Vector3.Normalize(projInc);
		projSca = Vector3.Normalize(projSca);

		// Angular terms
		float phi_i = MathF.Acos(Vector3.Dot(projInc, Vector3.UnitX));
		float phi_s = MathF.Acos(Vector3.Dot(projSca, Vector3.UnitX));

		// Diffracted coefficient (simplified PTD expression)
		double D = Math.Sqrt(1.0 / (2.0 * Math.PI * k * edgeDir.Length()))
				   * Math.Cos((phi_s - phi_i) / 2.0);

		// Phase term
		double phase = k * Vector3.Distance(a, b);
		return new Complex(D * Math.Cos(phase), D * Math.Sin(phase));
	}
}