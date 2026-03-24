using ComputeSharp;
using Echo1_Core.Engine;
using Echo1_Core.Geometry;
using Echo1_Core.Radar;

namespace Echo1.Core.Gpu;

/// <summary>
/// GPU compute shader for coherent Physical Optics facet contributions.
/// Outputs REAL and IMAG arrays separately.
/// </summary>
[ThreadGroupSize((DefaultThreadGroupSizes)256)]
public readonly partial struct RcsShader : IComputeShader
{
	// Input facet data
	public readonly ReadOnlyBuffer<FacetGpuData> Facets;

	// Output buffers
	public readonly ReadWriteBuffer<float> Real;
	public readonly ReadWriteBuffer<float> Imag;

	// Incident dir (xyz) + wavenumber (w)
	public readonly float4 KHat;

	public void Execute()
	{
		int i = ThreadIds.X;
		var f = Facets[i];

		// cos(theta)
		float dot = f.Nx * KHat.X + f.Ny * KHat.Y + f.Nz * KHat.Z;

		if (dot <= 0f)
		{
			Real[i] = 0;
			Imag[i] = 0;
			return;
		}

		float phase =
			2f * KHat.W * (
				f.Cx * KHat.X +
				f.Cy * KHat.Y +
				f.Cz * KHat.Z
			);

		float amp = 4f * 3.14159265f * f.Area * dot;

		Real[i] = amp * Hlsl.Cos(phase);
		Imag[i] = amp * Hlsl.Sin(phase);
	}
}