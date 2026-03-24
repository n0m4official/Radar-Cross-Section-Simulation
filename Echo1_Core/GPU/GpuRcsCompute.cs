using ComputeSharp;
using System;
using System.Numerics;
using Echo1.Core.Radar;
using Echo1.Core.Gpu;


namespace Echo1_Core.Gpu;

public sealed class GpuRcsCompute : IDisposable
{
	private readonly GraphicsDevice _device;

	public GpuRcsCompute()
		=> _device = GraphicsDevice.GetDefault();

	public Complex ComputeFacetContributions(FacetGpuData[] facets, RadarConfig radar)
	{
		using var facetBuf = _device.AllocateReadOnlyBuffer(facets);
		using var realBuf = _device.AllocateReadWriteBuffer<float>(facets.Length);
		using var imagBuf = _device.AllocateReadWriteBuffer<float>(facets.Length);

		var d = radar.IncidentDirection;
		var kHat = new float4(d.X, d.Y, d.Z, (float)radar.WaveNumber);

		var shader = new RcsShader
		{
			Facets = facetBuf,
			Real = realBuf,
			Imag = imagBuf,
			KHat = kHat
		};

		_device.For(facets.Length, shader);

		float[] re = realBuf.ToArray();
		float[] im = imagBuf.ToArray();

		double sumR = 0, sumI = 0;
		for (int i = 0; i < re.Length; i++)
		{
			sumR += re[i];
			sumI += im[i];
		}

		return new Complex(sumR, sumI);
	}

	public void Dispose() => _device?.Dispose();
}