// Echo1_RcsSimulator\Echo1_Core\Gpu\RcsShader.cs
using ComputeSharp;
using Echo1.Core.Gpu;

[ThreadGroupSize(DefaultThreadGroupSizes.X)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct RcsShader : IComputeShader
{
	public readonly ReadOnlyBuffer<FacetGpuData> Facets;
	public readonly ReadWriteBuffer<float> Real;
	public readonly ReadWriteBuffer<float> Imag;
	public readonly float4 KHat;

	public RcsShader(
		ReadOnlyBuffer<FacetGpuData> facets,
		ReadWriteBuffer<float> real,
		ReadWriteBuffer<float> imag,
		float4 kHat)
	{
		Facets = facets;
		Real = real;
		Imag = imag;
		KHat = kHat;
	}

	public void Execute()
	{
		int i = ThreadIds.X;
		var f = Facets[i];
		float dot = f.Nx * KHat.X + f.Ny * KHat.Y + f.Nz * KHat.Z;

		if (dot <= 0f) { Real[i] = 0; Imag[i] = 0; return; }

		float phase = 2f * KHat.W * (f.Cx * KHat.X + f.Cy * KHat.Y + f.Cz * KHat.Z);
		float amp = 4f * 3.14159265f * f.Area * dot;
		Real[i] = amp * Hlsl.Cos(phase);
		Imag[i] = amp * Hlsl.Sin(phase);
	}
}