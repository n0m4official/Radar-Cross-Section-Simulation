using Echo1.Core.Geometry;
using Echo1.Core.Radar;
using Echo1.Core.Engine;
using System.Collections.Concurrent;
using System.Numerics;

namespace Echo1.Core.Engine;

public sealed class RcsEngine
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

	public RcsResult Compute(RcsMesh mesh, RadarConfig radar)
	{
		var cacheKey = CacheKey.From(radar);
		if (_cache.TryGet(cacheKey, out var cached)) return cached;

		var kHat = radar.IncidentDirection;
		double k = radar.WaveNumber;

		var localSums = new Complex[Environment.ProcessorCount];

		Parallel.ForEach(
			Partitioner.Create(0, mesh.Facets.Length),
			_parallelOpts,
			() => Complex.Zero,
			(range, _, localSum) =>
			{
				for (int i = range.Item1; i < range.Item2; i++)
				{
					var facet = mesh.Facets[i];
					var po = PhysicalOpticsKernel.FacetContribution(facet, kHat, k);
					facet.RcsContribution = (float)po.Magnitude;
					facet.RcsDb = PhysicalOpticsKernel.ToDbsm(po.Magnitude);
					localSum += po;
				}
				// Edge diffraction contributions
				foreach (var edge in mesh.Edges)
				{
					localSum += EdgeDiffractionKernel.DiffractEdge(
						edge.A, edge.B, kHat, -kHat,
						edge.WedgeAngle, k);
				}
				return localSum;
			},
			localSum => { lock (localSums) localSums[0] += localSum; }
		);

		var total = localSums.Aggregate(Complex.Zero, (a, b) => a + b);
		double totalM2 = PhysicalOpticsKernel.TotalRcsM2(total);
		var result = new RcsResult(totalM2, PhysicalOpticsKernel.ToDbsm(totalM2), radar);
		_cache.Store(cacheKey, result);
		return result;
	}
}

public sealed record RcsResult(double TotalM2, float TotalDbsm, RadarConfig Radar);