// Echo1_RcsSimulator\Echo1_Core\Engine\EdgeDiffractionKernel.cs
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
/// reflection boundaries (Kouyoumjian & Pathak, 1974, Proc. IEEE).
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
///   Kouyoumjian & Pathak (1974) "A uniform geometrical theory of diffraction
///     for an edge in a perfectly conducting surface." Proc. IEEE, 62(11).
///   Ufimtsev (2007) "Fundamentals of the Physical Theory of Diffraction." Wiley.
///   Knott, Shaeffer & Tuley (2004) "Radar Cross Section." SciTech, ch. 6.
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

		if (Vector3.Dot(faceNormal1, incidentDir) <= 0f &&
			Vector3.Dot(faceNormal2, incidentDir) <= 0f)
		{
			return Complex.Zero;
		}

		Vec3 edgeVec = b - a;
		float edgeLen = edgeVec.Length();

		// 1. Sanitize Normals (The most likely Hawkeye failure point)
		if (float.IsNaN(faceNormal1.X) || float.IsNaN(faceNormal2.X) ||
			faceNormal1.LengthSquared() < 1e-6f || faceNormal2.LengthSquared() < 1e-6f)
		{
			return Complex.Zero;
		}

		// 2. Sanitize the wedge angle and edge
		if (float.IsNaN(wedgeAngle) || edgeLen < 1e-9f) return Complex.Zero;

		// Normalize incident direction safely
		if (incidentDir.LengthSquared() < 1e-12f) return Complex.Zero;
		incidentDir = Vec3.Normalize(incidentDir);

		// Edge tangent
		Vec3 t = edgeVec.LengthSquared() < 1e-12f ? Vec3.UnitX : Vec3.Normalize(edgeVec);

		// β₀: angle between incident direction and edge tangent
		float dot = Math.Clamp(Vec3.Dot(incidentDir, t), -1.0f, 1.0f);
		float sinBeta0Sq = 1.0f - dot * dot;
		if (sinBeta0Sq < 1e-12f) return Complex.Zero;
		double sinBeta0 = Math.Sqrt(sinBeta0Sq);

		// Edge coordinate frame
		Vec3 bisector = faceNormal1 + faceNormal2;
		if (bisector.LengthSquared() < 1e-12f) return Complex.Zero;
		Vec3 wedgeBisector = Vec3.Normalize(bisector);

		Vec3 bHat = Vec3.Cross(t, wedgeBisector);
		if (bHat.LengthSquared() < 1e-12f) return Complex.Zero;
		bHat = Vec3.Normalize(bHat);

		// Project incident direction into edge-normal plane
		Vec3 incProj = incidentDir - Vec3.Dot(incidentDir, t) * t;
		if (incProj.LengthSquared() < 1e-12f) return Complex.Zero;
		incProj = Vec3.Normalize(incProj);

		// Azimuth angles
		double phiInc = Math.Atan2(Vec3.Dot(incProj, bHat), Vec3.Dot(incProj, wedgeBisector));
		if (phiInc < 0) phiInc += 2.0 * Math.PI;
		double phiSca = 2.0 * Math.PI - phiInc;

		// Wedge parameter n
		double alpha = wedgeAngle;
		double n = Math.Clamp((2.0 * Math.PI - alpha) / Math.PI, 1.0, 2.0);

		// Fresnel path parameter
		double L = sinBeta0Sq;

		if (sinBeta0Sq < 1e-2)   // ~β₀ < 6 degrees
		{
			return Complex.Zero;
		}

		// UTD diffraction coefficients
		double phi_minus = phiSca - phiInc;
		double phi_plus = phiSca + phiInc;

		Complex Ds = UtdDiffractionCoeff(phi_minus, phi_plus, n, k, L, sinBeta0, true);
		Complex Dh = UtdDiffractionCoeff(phi_minus, phi_plus, n, k, L, sinBeta0, false);

		// Monostatic VV: average
		Complex D = (Ds + Dh) * 0.5;

		// Radiation integral along edge (mid-edge approximation)
		Vec3 edgeMid = (a + b) * 0.5f;
		double midPhase = 2.0 * k * (incidentDir.X * edgeMid.X
								   + incidentDir.Y * edgeMid.Y
								   + incidentDir.Z * edgeMid.Z);
		var phasePhasor = new Complex(Math.Cos(midPhase), Math.Sin(midPhase));

		return D * phasePhasor / Math.Sqrt(4.0 * Math.PI);
	}

	private static Complex UtdDiffractionCoeff(
		double phi_minus, double phi_plus, double n,
		double k, double L, double sinBeta0, bool soft)
	{
		double safeSinBeta = Math.Max(sinBeta0, 1e-12);

		double prefactorMag = 1.0 / (2.0 * n * Math.Sqrt(2.0 * Math.PI * k) * safeSinBeta);

		Complex phase = Complex.Exp(new Complex(0, -Math.PI / 4.0));
		Complex prefactor = prefactorMag * phase;

		// Ufimtsev uses ONLY phi = phi_s - phi_i
		Complex F1 = CotangentTerm(phi_minus, n, +1.0, k, L);
		Complex F2 = CotangentTerm(phi_minus, n, -1.0, k, L);

		Complex result;

		if (soft)
		{
			result = -(prefactor) * (F1 + F2);
		}
		else
		{
			// Hard polarization flips sign between terms
			result = -(prefactor) * (F1 - F2);
		}

		return result;
	}

	private static Complex CotangentTerm(double phi, double n, double sign, double k, double L)
	{
		// Proper Ufimtsev angular term
		double angle = (Math.PI + sign * phi) / (2.0 * n);

		double cotVal = CosSafe(angle) / SinSafe(angle);

		// Proper Keller cone distance function
		double m = NearestInt((phi + sign * Math.PI) / (2.0 * Math.PI * n));

		double delta = phi + sign * Math.PI - 2.0 * Math.PI * n * m;

		double a = 2.0 * Math.Pow(Math.Sin(delta / 2.0), 2);

		double x = k * L * Math.Max(a, 1e-12);

		Complex F = FresnelTransition(x);

		return new Complex(cotVal, 0.0) * F;
	}

	private static Complex FresnelTransition(double x)
	{
		x = Math.Max(x, 0.0);

		if (x < 0.3)
		{
			double sqrtX = Math.Sqrt(x);
			double mag = Math.Sqrt(Math.PI) * sqrtX;
			double phase = Math.PI / 4.0 + x;
			var series = new Complex(mag * Math.Cos(phase), mag * Math.Sin(phase));
			var corr = new Complex(1.0 - x / 3.0, -x * x / 12.0);
			return series * corr;
		}
		else if (x > 30.0)
		{
			return new Complex(1.0 - 3.0 / (4.0 * x * x),
							   -1.0 / (2.0 * x) + 15.0 / (8.0 * x * x * x));
		}
		else
		{
			double sqrtX = Math.Sqrt(x);
			var integral = GaussLegendreFresnelIntegral(sqrtX);
			var inf = new Complex(Math.Sqrt(Math.PI) / 2.0 * Math.Cos(-Math.PI / 4.0),
								  Math.Sqrt(Math.PI) / 2.0 * Math.Sin(-Math.PI / 4.0));
			var tailIntegral = inf - integral;
			var ejx = new Complex(Math.Cos(x), Math.Sin(x));
			return new Complex(0, 2.0 * sqrtX) * ejx * tailIntegral;
		}
	}

	private static Complex GaussLegendreFresnelIntegral(double a)
	{
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

	private static double NearestInt(double x) => Math.Round(x);
	private static double CosSafe(double x) => Math.Cos(x);
	private static double SinSafe(double x)
	{
		double s = Math.Sin(x);
		return Math.Abs(s) < 1e-12 ? (s >= 0 ? 1e-12 : -1e-12) : s;
	}
}
