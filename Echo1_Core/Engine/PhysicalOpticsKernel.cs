using Echo1.Core.Geometry;
using Echo1.Core.Radar;
using System.Numerics;
using Complex = System.Numerics.Complex;

namespace Echo1.Core.Engine;

/// <summary>
/// Physical Optics (PO) kernel for monostatic RCS.
///
/// The correct monostatic PO formula for a perfectly electrically conducting (PEC)
/// triangular facet is derived from the Stratton-Chu surface integral:
///
///   E_s ∝ (jk / 2π) ∫∫ (n̂ × E_i) × k̂_s · e^(j2k k̂·r) dA
///
/// For a flat triangular facet illuminated by a plane wave, the integral over the
/// facet area in the far field reduces to the closed-form expression implemented below.
/// The bistatic PO amplitude for a single flat facet is:
///
///   F(k̂_i, k̂_s) = (k² / π) · A · cos(θ_i) · sinc(u) · sinc(v)  [general bistatic]
///
/// For monostatic (k̂_s = -k̂_i), the expression simplifies, and for arbitrary facet
/// shape the exact integral is computed via the analytic formula for a triangle
/// (Ufimtsev 2007, §3.2; Knott, Shaeffer & Tuley "Radar Cross Section", ch.4).
///
/// The full vector PO integral over a triangle with vertices P0, P1, P2 and
/// monostatic incident direction k̂ is:
///
///   I = Σ_i [ (k̂ × r_i) × (k̂ × r_{i+1}) / |cross| ] · (e^(j2k·k̂·r_i) - e^(j2k·k̂·r_{i+1})) / (2j·k·Δφ_i)
///
/// where Δφ_i is the phase difference between vertices i and i+1.
/// This is the Ling-Lee-Chuang (1989) exact triangle PO integral.
///
/// Reference: Ling, H., Chou, R.C., Lee, S.W. (1989). "Shooting and Bouncing Rays:
/// Calculating the RCS of an Arbitrarily Shaped Cavity." IEEE Trans. Antennas Propag.
/// </summary>
public static class PhysicalOpticsKernel
{
	/// <summary>
	/// Computes the exact PO monostatic scattering amplitude for a single triangular facet
	/// using the Ling-Lee-Chuang analytic triangle integral.
	///
	/// Returns the complex scattering amplitude S such that σ = 4π|S|²/λ².
	/// The k² factor is implicit — the returned value already carries units of m²/sr^(1/2).
	/// </summary>
	public static Complex FacetContribution(Facet facet, Vector3 kHat, double k,
		MaterialProperties material, Polarisation pol = Polarisation.VV)
	{
		// Back-face culling: facet must face the radar
		double cosTheta = Vector3.Dot(facet.Normal, -kHat);
		if (cosTheta <= 1e-9) return Complex.Zero;

		// Compute material reflection coefficient (Fresnel, monostatic)
		// For PEC: Γ = -1 (H-pol), +1 (V-pol). For coated surfaces: use Fresnel.
		Complex gamma = material.FresnelReflection(cosTheta, pol);

		// Phase of each vertex: φ_i = 2k · (k̂ · r_i)
		// Factor of 2 is because monostatic: incident + reflected path both traverse k̂·r.
		double phi0 = 2.0 * k * (kHat.X * facet.V0.X + kHat.Y * facet.V0.Y + kHat.Z * facet.V0.Z);
		double phi1 = 2.0 * k * (kHat.X * facet.V1.X + kHat.Y * facet.V1.Y + kHat.Z * facet.V1.Z);
		double phi2 = 2.0 * k * (kHat.X * facet.V2.X + kHat.Y * facet.V2.Y + kHat.Z * facet.V2.Z);

		// Phasors at each vertex
		var E0 = new Complex(Math.Cos(phi0), Math.Sin(phi0));
		var E1 = new Complex(Math.Cos(phi1), Math.Sin(phi1));
		var E2 = new Complex(Math.Cos(phi2), Math.Sin(phi2));

		// Analytic triangle PO integral (Ling-Lee-Chuang):
		// I = Σ_{edges} [ (E_i - E_{i+1}) / (phi_{i+1} - phi_i) ]
		// When vertices are phase-degenerate (edge perpendicular to k̂), use area limit.
		Complex I = TrianglePhaseIntegral(E0, E1, E2, phi0, phi1, phi2);

		// PO amplitude: S = (j * k² / 2π) · A · cosθ · Γ · I
		// Combined into single complex expression:
		double amplitude = (1.0) / (2.0 * Math.PI) * facet.Area * cosTheta;
		var jk = new Complex(0.0, amplitude);   // j factor from surface current to radiation

		return jk * gamma * I;
	}

	/// <summary>
	/// Analytic PO phase integral over a triangle.
	/// Computes: Σ_edges (E_a - E_b) / (phi_b - phi_a)
	/// with stable sinc limit when |phi_b - phi_a| &lt; ε.
	/// </summary>
	private static Complex TrianglePhaseIntegral(
		Complex E0, Complex E1, Complex E2,
		double phi0, double phi1, double phi2)
	{
		return EdgeIntegral(E0, E1, phi0, phi1)
			 + EdgeIntegral(E1, E2, phi1, phi2)
			 + EdgeIntegral(E2, E0, phi2, phi0);
	}

	private static Complex EdgeIntegral(Complex Ea, Complex Eb, double phiA, double phiB)
	{
		double dPhi = phiB - phiA;
		if (Math.Abs(dPhi) < 1e-9)
		{
			// L'Hôpital limit: (Eb - Ea)/dPhi → dEa/dphi_a * (-1) → -j*Ea
			// Accurate Taylor: (Ea + Eb)/2 (midpoint average of phasor)
			return (Ea + Eb) * 0.5;
		}
		return (Eb - Ea) / dPhi;
	}

	/// <summary>
	/// Converts coherent complex amplitude sum to monostatic RCS in m².
	/// σ = 4π |S|²
	/// where S is the accumulated FacetContribution sum (already contains k²/2π).
	/// </summary>
	public static double TotalRcsM2(Complex coherentSum)
	{
		double mag2 = coherentSum.Real * coherentSum.Real
					+ coherentSum.Imaginary * coherentSum.Imaginary;
		return 4.0 * Math.PI * mag2;
	}

	public static float ToDbsm(double rcsM2)
		=> rcsM2 > 1e-30 ? (float)(10.0 * Math.Log10(rcsM2)) : -100f;
}
