using System.Numerics;
using Complex = System.Numerics.Complex;
using Vec3 = System.Numerics.Vector3;

namespace Echo1.Core.Engine;

/// <summary>
/// Uniform Theory of Diffraction (UTD) / Physical Theory of Diffraction (PTD)
/// wedge diffraction kernel for RCS edge contributions.
///
/// Implements the Keller-Mitzner-Ufimtsev formulation with Kouyoumjian-Pathak
/// uniform transition function F(x) to remove the singularities at shadow and
/// reflection boundaries (Kouyoumjian &amp; Pathak, 1974, Proc. IEEE).
///
/// For a straight wedge edge with:
///   - Interior wedge angle α (the solid angle of the wedge material)
///   - Exterior wedge angle W = π - α  (the diffracting angle seen by the field)
///   - n = (2π - α)/π  (wedge parameter, n=2 for half-plane, n=1 for flat surface)
///
/// The UTD diffraction coefficient for soft (Ds) and hard (Dh) polarizations:
///
///   D_s,h = -e^(-jπ/4) / (2n·√(2πk)·sin(β₀)) · [F₁ ± F₂]
///
/// where:
///   F₁ = cot((π + (φ - φ')) / 2n) · F(kL·a⁺(φ - φ'))
///   F₂ = cot((π - (φ - φ')) / 2n) · F(kL·a⁻(φ - φ'))
///   F(x) = 2j√x · e^(jx) · ∫_x^∞ e^(-jt²) dt   [Fresnel integral]
///
/// References:
///   Kouyoumjian &amp; Pathak (1974) "A uniform geometrical theory of diffraction
///     for an edge in a perfectly conducting surface." Proc. IEEE, 62(11).
///   Ufimtsev (2007) "Fundamentals of the Physical Theory of Diffraction." Wiley.
///   Knott, Shaeffer &amp; Tuley (2004) "Radar Cross Section." SciTech, ch. 6.
/// </summary>
public static class EdgeDiffractionKernel
{
	/// <summary>
	/// Computes the monostatic UTD diffraction contribution of a single edge segment.
	///
	/// Parameters:
	///   a, b         — edge endpoint positions (world space, metres)
	///   incidentDir  — unit vector FROM radar TO target (k̂_i)
	///   wedgeAngle   — dihedral interior angle of the wedge in radians
	///   k            — wave number (2π/λ)
	///   n1, n2       — outward normals of the two facets sharing this edge
	///
	/// Returns complex scattering amplitude contribution (SI units consistent with PO kernel).
	/// </summary>
	public static Complex DiffractEdge(
		Vec3 a,
		Vec3 b,
		Vec3 incidentDir,
		float wedgeAngle,
		double k,
		Vec3 faceNormal1,
		Vec3 faceNormal2)
	{
		Vec3 edgeVec = b - a;
		float edgeLen = edgeVec.Length();
		if (edgeLen < 1e-9f) return Complex.Zero;

		Vec3 t = Vec3.Normalize(edgeVec);  // edge tangent (unit)

		// β₀: angle between incident direction and edge tangent
		// For monostatic, scattering direction = -incidentDir
		float sinBeta0Sq = 1.0f - MathF.Pow(Vec3.Dot(incidentDir, t), 2);
		if (sinBeta0Sq < 1e-8f) return Complex.Zero;  // edge-on incidence — skip
		double sinBeta0 = Math.Sqrt(sinBeta0Sq);

		// Build local edge coordinate frame:
		// - t̂: edge tangent
		// - n̂: bisector of the wedge faces (pointing into the exterior region)
		// - b̂ = t̂ × n̂
		Vec3 wedgeBisector = Vec3.Normalize(faceNormal1 + faceNormal2);
		Vec3 bHat = Vec3.Normalize(Vec3.Cross(t, wedgeBisector));

		// Project incident direction into edge-normal plane (perpendicular to t̂)
		Vec3 incProj = incidentDir - Vec3.Dot(incidentDir, t) * t;
		float incProjLen = incProj.Length();
		if (incProjLen < 1e-9f) return Complex.Zero;
		incProj = Vec3.Normalize(incProj);

		// φ': azimuth angle of incidence (from face 1 normal, measured in edge normal plane)
		// φ : azimuth angle of scattering = 2π - φ' for monostatic (backscatter)
		double phiInc = Math.Atan2(Vec3.Dot(incProj, bHat), Vec3.Dot(incProj, wedgeBisector));
		// Ensure [0, 2π]
		if (phiInc < 0) phiInc += 2.0 * Math.PI;

		// Monostatic: scatter back toward radar, so φ_s = 2π - φ_inc in the wedge plane
		double phiSca = 2.0 * Math.PI - phiInc;

		// Wedge parameter n = (2π - α) / π where α = interior angle
		// n = 2: half-plane diffractor
		// n = 1: flat surface (no diffraction at smooth surface)
		double alpha = wedgeAngle;  // interior solid angle of the wedge material
		double n = (2.0 * Math.PI - alpha) / Math.PI;
		n = Math.Clamp(n, 1.0, 2.0);

		// Effective path length L for Fresnel parameter
		// For monostatic far-field: L = distance from edge to radar is effectively ∞,
		// but we use the edge length as the integration variable.
		// L = sin²β₀ (normalised to edge length, factor applied below)
		double L = sinBeta0Sq;

		// Compute UTD diffraction coefficients (soft and hard)
		double phi_minus = phiSca - phiInc;  // (φ - φ')
		double phi_plus = phiSca + phiInc;  // (φ + φ')

		Complex Ds = UtdDiffractionCoeff(phi_minus, phi_plus, n, k, L, sinBeta0, soft: true);
		Complex Dh = UtdDiffractionCoeff(phi_minus, phi_plus, n, k, L, sinBeta0, soft: false);

		// For monostatic VV: use average of soft and hard (E-plane average)
		// A full polarimetric implementation would separate these by pol vector projection.
		Complex D = (Ds + Dh) * 0.5;

		// Radiation integral along edge (coherent sum):
		// I_edge = ∫_0^L e^(j2k k̂·r) ds evaluated at centroid (mid-edge approximation)
		// For a short edge relative to λ, the centroid-phase approximation is valid.
		Vec3 edgeMid = (a + b) * 0.5f;
		double midPhase = 2.0 * k * (incidentDir.X * edgeMid.X
								   + incidentDir.Y * edgeMid.Y
								   + incidentDir.Z * edgeMid.Z);
		var phasePhasor = new Complex(Math.Cos(midPhase), Math.Sin(midPhase));

		// Total edge contribution: D · L_edge · e^(jφ_mid)
		return D * edgeLen * phasePhasor;
	}

	/// <summary>
	/// UTD diffraction coefficient with Kouyoumjian-Pathak uniform Fresnel correction.
	/// </summary>
	private static Complex UtdDiffractionCoeff(
		double phi_minus, double phi_plus, double n,
		double k, double L, double sinBeta0, bool soft)
	{
		// Pre-factor: e^(-jπ/4) / (2n·√(2πk·sinBeta0))
		double prefactorMag = 1.0 / (2.0 * n * Math.Sqrt(2.0 * Math.PI * k) * sinBeta0);
		var prefactor = new Complex(
			prefactorMag * Math.Cos(-Math.PI / 4.0),
			prefactorMag * Math.Sin(-Math.PI / 4.0));

		// Four angular terms (two sign combinations × two polarisations):
		Complex f1 = CotangentTerm(phi_minus, n, +1.0, k, L);
		Complex f2 = CotangentTerm(phi_minus, n, -1.0, k, L);
		Complex f3 = CotangentTerm(phi_plus, n, +1.0, k, L);
		Complex f4 = CotangentTerm(phi_plus, n, -1.0, k, L);

		Complex D_soft = -prefactor * (f1 + f2 + f3 + f4);
		Complex D_hard = -prefactor * (f1 + f2 - f3 - f4);

		return soft ? D_soft : D_hard;
	}

	/// <summary>
	/// Cotangent factor with Kouyoumjian-Pathak uniform Fresnel transition function F(x).
	/// F(x) prevents divergence at shadow and reflection boundaries.
	/// cot( (π ± φ) / 2n ) · F(kL · a±(φ))
	/// </summary>
	private static Complex CotangentTerm(double phi, double n, double sign, double k, double L)
	{
		double arg = (Math.PI + sign * phi) / (2.0 * n);
		double cotVal = CosSafe(arg) / SinSafe(arg);  // cot(arg), protected against ÷0

		double aPhi = 2.0 * Math.Pow(Math.Cos(phi / 2.0 - Math.PI * NearestInt(phi / (2.0 * Math.PI * n))), 2);
		double x = k * L * aPhi;

		Complex F = FresnelTransition(x);

		return new Complex(cotVal, 0.0) * F;
	}

	/// <summary>
	/// Kouyoumjian-Pathak uniform Fresnel transition function:
	///   F(x) = 2j·√x · e^(jx) · ∫_√x^∞ e^(-jt²) dt
	///
	/// Computed via series expansion for small x, asymptotic for large x.
	/// </summary>
	private static Complex FresnelTransition(double x)
	{
		if (x < 0) x = 0;

		if (x < 0.3)
		{
			// Small-argument series (Abramowitz &amp; Stegun approximation):
			// F(x) ≈ √(πx) · e^(j(π/4 + x)) · [1 - j·x/3 + ...]
			double sqrtX = Math.Sqrt(x);
			double mag = Math.Sqrt(Math.PI) * sqrtX;
			double phase = Math.PI / 4.0 + x;
			var series = new Complex(mag * Math.Cos(phase), mag * Math.Sin(phase));
			// Correction term:
			var corr = new Complex(1.0 - x / 3.0, -x * x / 12.0);
			return series * corr;
		}
		else if (x > 30.0)
		{
			// Asymptotic expansion: F(x) → 1 - j/(2x) - 3/(4x²) + ...
			return new Complex(1.0 - 3.0 / (4.0 * x * x),
							  -1.0 / (2.0 * x) + 15.0 / (8.0 * x * x * x));
		}
		else
		{
			// Mid-range: numerical integration of ∫_0^√x e^(-jt²) dt
			// via 8-point Gauss-Legendre quadrature on [0, √x]
			double sqrtX = Math.Sqrt(x);
			var integral = GaussLegendreFresnelIntegral(sqrtX);
			// F(x) = 2j√x · e^(jx) · (∫_0^∞ - ∫_0^√x)
			// Known: ∫_0^∞ e^(-jt²) dt = √π/2 · e^(-jπ/4)
			var inf = new Complex(Math.Sqrt(Math.PI) / 2.0 * Math.Cos(-Math.PI / 4.0),
								  Math.Sqrt(Math.PI) / 2.0 * Math.Sin(-Math.PI / 4.0));
			var tailIntegral = inf - integral;
			var ejx = new Complex(Math.Cos(x), Math.Sin(x));
			return new Complex(0, 2.0 * sqrtX) * ejx * tailIntegral;
		}
	}

	/// <summary>
	/// 16-point Gauss-Legendre quadrature for ∫_0^a e^(-jt²) dt.
	/// </summary>
	private static Complex GaussLegendreFresnelIntegral(double a)
	{
		// 16-point GL nodes and weights on [-1, 1]
		ReadOnlySpan<double> nodes = stackalloc double[]
		{
			-0.9894009349919, -0.9445750230732, -0.8656312023341, -0.7554044083550,
			-0.6178762444026, -0.4580167776572, -0.2816035507792, -0.0950125098360,
			 0.0950125098360,  0.2816035507792,  0.4580167776572,  0.6178762444026,
			 0.7554044083550,  0.8656312023341,  0.9445750230732,  0.9894009349919
		};
		ReadOnlySpan<double> weights = stackalloc double[]
		{
			0.0271524594117, 0.0622535239386, 0.0951585116824, 0.1246289712556,
			0.1495959888165, 0.1691565193950, 0.1826034150449, 0.1894506104341,
			0.1894506104341, 0.1826034150449, 0.1691565193950, 0.1495959888165,
			0.1246289712556, 0.0951585116824, 0.0622535239386, 0.0271524594117
		};

		double halfA = a * 0.5;
		var sum = Complex.Zero;
		for (int i = 0; i < 16; i++)
		{
			double t = halfA * (1.0 + nodes[i]);
			double t2 = t * t;
			sum += weights[i] * new Complex(Math.Cos(-t2), Math.Sin(-t2));
		}
		return sum * halfA;
	}

	// Helper: nearest integer (for a±(φ) computation)
	private static double NearestInt(double x) => Math.Round(x);
	private static double CosSafe(double x) => Math.Cos(x);
	private static double SinSafe(double x)
	{
		double s = Math.Sin(x);
		return Math.Abs(s) < 1e-12 ? (s >= 0 ? 1e-12 : -1e-12) : s;
	}
}
