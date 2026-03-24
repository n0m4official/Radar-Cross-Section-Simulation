using Echo1_Core.Geometry;
using System.Numerics;

namespace Echo1.Core.Engine;

/// <summary>
/// Physical Optics (PO) monostatic RCS kernel.
/// Each facet contributes a complex scattered field; total RCS = |Σ Ei|².
/// </summary>
public static class PhysicalOpticsKernel
{
	/// <summary>
	/// Compute the PO RCS contribution of a single facet.
	/// Returns complex amplitude — caller sums coherently across facets.
	/// </summary>
	public static Complex FacetContribution(
		Facet facet,
		Vector3 kHat,      // unit incident direction (toward target)
		double k)         // wave number 2π/λ
	{
		float cosTheta = Vector3.Dot(facet.Normal, -kHat);
		if (cosTheta <= 0f) return Complex.Zero;   // back-face cull

		// PO surface current integral for a flat triangular facet
		// Simplified monostatic: E_s ∝ j·k·cos(θ)·A·exp(j·2k·r̂·centroid)
		double phase = 2.0 * k * (
			kHat.X * facet.Centroid.X +
			kHat.Y * facet.Centroid.Y +
			kHat.Z * facet.Centroid.Z);

		double amplitude = 4.0 * Math.PI * facet.Area * cosTheta;
		return new Complex(
			amplitude * Math.Cos(phase),
			amplitude * Math.Sin(phase));
	}

	public static double TotalRcsM2(Complex coherentSum)
	{
		double mag2 = coherentSum.Real * coherentSum.Real
					+ coherentSum.Imaginary * coherentSum.Imaginary;
		// σ = |E_s|² / (4π) — simplified monostatic normalisation
		return mag2 / (4.0 * Math.PI);
	}

	public static float ToDbsm(double rcsM2)
		=> rcsM2 > 1e-30 ? (float)(10.0 * Math.Log10(rcsM2)) : -100f;
}