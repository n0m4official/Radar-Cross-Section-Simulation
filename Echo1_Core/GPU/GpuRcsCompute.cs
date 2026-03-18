// NuGet: ComputeSharp
using ComputeSharp;
using Echo1.Core.Radar;
using System.Runtime.InteropServices;

namespace Echo1.Core.Gpu;

/// <summary>GPU shader for parallel facet RCS contributions.</summary>
[ThreadGroupSize(DefaultThreadGroupSizes.X)]
[GeneratedComputeShaderDescriptor]
public partial struct RcsShader : IComputeShader
{
	public ReadOnlyBuffer<FacetGpuData> Facets;
	public ReadWriteBuffer<float> Results;
	public float4 KHat;   // incident direction xyz, w=wavenumber

	public void Execute()
	{
		int i = ThreadIds.X;
		var f = Facets[i];
		float dot = f.Nx * KHat.X + f.Ny * KHat.Y + f.Nz * KHat.Z;

		if (dot <= 0f)
		{
			Results[i] = 0f;
			return;
		}

		float phase = 2f * KHat.W * (f.Cx * KHat.X + f.Cy * KHat.Y + f.Cz * KHat.Z);
		float amp = 4f * 3.14159265f * f.Area * dot;
		// Return amplitude squared for incoherent sum (fast) or phase for coherent
		Results[i] = amp; // Extend for full complex sum
	}
}

[StructLayout(LayoutKind.Sequential)]
public struct FacetGpuData
{
	public float Nx, Ny, Nz;       // normal
	public float Cx, Cy, Cz;       // centroid
	public float Area;
	public float _pad;
}

/// <summary>GPU compute entry point.</summary>
public sealed class GpuRcsCompute : IDisposable
{
	private readonly GraphicsDevice _device;

	public GpuRcsCompute()
		=> _device = GraphicsDevice.GetDefault();

	public float[] ComputeFacetContributions(FacetGpuData[] facets, RadarConfig radar)
	{
		using var facetBuf = _device.AllocateReadOnlyBuffer(facets);
		using var resultBuf = _device.AllocateReadWriteBuffer<float>(facets.Length);

		var dir = radar.IncidentDirection;
		var kHat = new float4(dir.X, dir.Y, dir.Z, (float)radar.WaveNumber);
		var shader = new RcsShader { Facets = facetBuf, Results = resultBuf, KHat = kHat };

		_device.For(facets.Length, in shader);
		return resultBuf.ToArray();
	}

	public void Dispose() => _device.Dispose();
}