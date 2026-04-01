using Echo1.Core.Geometry;
using Echo1.Core.Radar;
using Echo1.Core.Engine;
using System.Collections.Concurrent;
using System.Numerics;
using Complex = System.Numerics.Complex;

namespace Echo1.Core.Engine;

/// <summary>
/// RCS computation engine — Physical Optics + UTD edge diffraction.
///
/// Architecture:
///   1. Facets are parallelised across CPU cores (PLINQ partition).
///      Each thread accumulates a local complex sum.
///   2. Edges are computed once (not per-partition!) and added to the final sum.
///   3. Per-facet heatmap values (stored for the renderer) use incoherent magnitude
///      to avoid sign-reversal artefacts in the colour map.
///   4. The coherent total sum is used for the final RCS number.
///
/// Bug fixed from original:
///   The original engine looped all edges inside the parallel facet lambda,
///   causing each edge to be accumulated O(ProcessorCount) times — incorrect.
///   Edges are now computed after the parallel facet loop and added once.
/// </summary>
public sealed partial class RcsEngine
{
	private readonly RcsCache _cache;
	private readonly ParallelOptions _parallelOpts;

	public RcsEngine(int maxDegreeOfParallelism = -1)
	{
		_cache = new RcsCache();
		_parallelOpts = new ParallelOptions
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism < 0
				? Environment.ProcessorCount
				: maxDegreeOfParallelism
		};
	}

	/// <summary>
	/// Compute monostatic RCS for the given mesh and radar configuration.
	/// Caches results by (azimuth, elevation, frequency) within 0.5° / 100 MHz resolution.
	/// </summary>
	public RcsResult Compute(RcsMesh mesh, RadarConfig radar)
	{
		var cacheKey = CacheKey.From(radar);
		if (_cache.TryGet(cacheKey, out var cached)) return cached;

		var kHat = radar.IncidentDirection;
		double k = radar.WaveNumber;
		var pol = radar.TxPol;

		// ── 1. Physical Optics: parallel over facets ──────────────────────────────
		// Each partition accumulates a thread-local complex sum.
		// After the loop, all thread sums are reduced to one coherent total.

		var facets = mesh.Facets;
		var partitionSums = new Complex[Environment.ProcessorCount + 1];

		Parallel.ForEach(
			Partitioner.Create(0, facets.Length),
			_parallelOpts,
			() => Complex.Zero,                  // thread-local seed
			(range, _, localSum) =>
			{
				for (int i = range.Item1; i < range.Item2; i++)
				{
					var facet = facets[i];

					// Per-facet material lookup (falls back to mesh default)
					var material = mesh.GetMaterial(i);

					var contribution = PhysicalOpticsKernel.FacetContribution(
						facet, kHat, k, material, pol);

					if (double.IsNaN(contribution.Real) || double.IsNaN(contribution.Imaginary))
					{
						// Log the facet/edge index here to find the "bad" part of the E-2D model!
						contribution = Complex.Zero;
					}

					// Store incoherent magnitude for heatmap colouring
					// Using contribution magnitude squared (proportional to per-facet σ)
					double facetRcs = contribution.Magnitude;
					facet.RcsContribution = (float)facetRcs;
					facet.RcsDb = PhysicalOpticsKernel.ToDbsm(facetRcs);

					localSum += contribution;
				}
				return localSum;
			},
			// Reduction: add each thread's sum to the shared array under lock
			localSum =>
			{
				int slot = Environment.CurrentManagedThreadId % partitionSums.Length;
				lock (partitionSums) partitionSums[slot] += localSum;
			}
		);

		var poSum = partitionSums.Aggregate(Complex.Zero, (a, b) => a + b);

		// ── 2. Edge Diffraction: single-threaded, computed ONCE ────────────────────
		// Not parallelised here because edge count is typically 1-2 orders of magnitude
		// smaller than facet count. Parallelise if edge count > ~100k.
		double edgeRcsM2 = 0.0;

		foreach (var edge in mesh.Edges)
		{
			var e = EdgeDiffractionKernel.DiffractEdge(
				edge.A, edge.B,
				kHat,
				edge.WedgeAngle,
				k,
				edge.Normal1,
				edge.Normal2);

			// incoherent power sum
			double mag2 = e.Real * e.Real + e.Imaginary * e.Imaginary;
			edgeRcsM2 += 4.0 * Math.PI * mag2;
		}

		// ── 3. Coherent total ─────────────────────────────────────────────────────

		double poRcsM2 = PhysicalOpticsKernel.TotalRcsM2(poSum);
		double totalM2 = poRcsM2 + edgeRcsM2;
		float totalDb = PhysicalOpticsKernel.ToDbsm(totalM2);

		var result = new RcsResult(
			TotalM2: totalM2,
			TotalDbsm: totalDb,
			PoM2: poRcsM2,
			EdgeM2: edgeRcsM2,
			Radar: radar);

		_cache.Store(cacheKey, result);
		return result;
	}

	/// <summary>
	/// Compute a full azimuth sweep and return a polar dataset.
	/// Used for the polar plot panel.
	/// </summary>
	public (double[] AzDeg, double[] RcsDbsm) SweepAzimuth(
		RcsMesh mesh, RadarConfig radarTemplate,
		double startDeg = 0, double stopDeg = 360, double stepDeg = 1.0)
	{
		int n = (int)Math.Round((stopDeg - startDeg) / stepDeg) + 1;
		var azDeg = new double[n];
		var rcsDb = new double[n];

		Parallel.For(0, n, _parallelOpts, i =>
		{
			var r = new RadarConfig
			{
				FrequencyHz = radarTemplate.FrequencyHz,
				AzimuthDeg = (float)(startDeg + i * stepDeg),
				ElevationDeg = radarTemplate.ElevationDeg,
				TxPol = radarTemplate.TxPol
			};
			var result = Compute(mesh, r);
			azDeg[i] = startDeg + i * stepDeg;
			rcsDb[i] = result.TotalDbsm;
		});

		return (azDeg, rcsDb);
	}
}

/// <summary>
/// Full RCS result, including breakdown of PO vs edge-diffraction contributions.
/// </summary>
public sealed record RcsResult(
	double TotalM2,
	float TotalDbsm,
	double PoM2,
	double EdgeM2,
	RadarConfig Radar);
