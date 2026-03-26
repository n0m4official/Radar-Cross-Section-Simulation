// Echo1_RcsSimulator\Echo1_Core\Engine\PhysicalOpticsKernel.cs
using Echo1.Core.Geometry;
using System.Numerics;
using Complex = System.Numerics.Complex;

namespace Echo1.Core.Engine;

public static class PhysicalOpticsKernel
{
	public static Complex FacetContribution(Facet facet, Vector3 kHat, double k)
	{
		float cosTheta = Vector3.Dot(facet.Normal, -kHat);
		if (cosTheta <= 0f) return Complex.Zero;

		double phase = 2.0 * k * (
			kHat.X * facet.Centroid.X +
			kHat.Y * facet.Centroid.Y +
			kHat.Z * facet.Centroid.Z);
		double amplitude = 4.0 * Math.PI * facet.Area * cosTheta;

		return new Complex(amplitude * Math.Cos(phase), amplitude * Math.Sin(phase));
	}

	public static double TotalRcsM2(Complex coherentSum)
	{
		double mag2 = coherentSum.Real * coherentSum.Real
					+ coherentSum.Imaginary * coherentSum.Imaginary;
		return mag2 / (4.0 * Math.PI);
	}

	public static float ToDbsm(double rcsM2)
		=> rcsM2 > 1e-30 ? (float)(10.0 * Math.Log10(rcsM2)) : -100f;
}