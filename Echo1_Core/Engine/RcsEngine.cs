using Echo1.Core.Engine;
using Echo1.Core.Geometry;
using Echo1.Core.Gpu;
using Echo1.Core.Radar;
using Echo1_Core.GPU;
using Echo1_Core.Radar;
using System.Numerics;

namespace Echo1_Core.Engine
{
	public class RcsEngine
	{
		private readonly GpuRcsCompute _gpuCompute = new();

		public void ComputeSweep(RcsMesh mesh, RadarConfig config, bool useGpu)
		{
			var direction = CalculateDirection(config.AzimuthDeg, config.ElevationDeg);

			if (useGpu)
			{
				// Note: Ensure your GpuRcsCompute.cs is updated to handle 
				// the mesh.Facets array buffer.
				_gpuCompute.Run(mesh, config);
			}
			else
			{
				// CPU Parallel processing
				Parallel.ForEach(mesh.Facets, facet =>
				{
					if (BackFaceCuller.IsVisible(facet, direction))
					{
						facet.RcsDb = PhysicalOpticsKernel.CalculateFacetRcs(facet, config, direction);
					}
					else
					{
						facet.RcsDb = -99f; // Masked/Culled
					}
				});
			}
		}

		private Vector3 CalculateDirection(float az, float el)
		{
			float azRad = az * (MathF.PI / 180f);
			float elRad = el * (MathF.PI / 180f);
			return new Vector3(
				MathF.Cos(elRad) * MathF.Sin(azRad),
				MathF.Sin(elRad),
				MathF.Cos(elRad) * MathF.Cos(azRad)
			);
		}
	}
}