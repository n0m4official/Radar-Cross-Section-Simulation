using ComputeSharp;
using Echo1.Core.Gpu;

/// <summary>
/// GPU Physical Optics kernel (ComputeSharp HLSL).
///
/// Mirrors the corrected CPU PhysicalOpticsKernel.FacetContribution logic.
/// Amplitude = (k² / 2π) · Area · cosθ
///
/// The original shader used amplitude = 4π · Area · cosθ, which was missing
/// the k² factor and had the wrong normalisation constant (4π vs k²/2π).
///
/// Note: the GPU kernel uses a simplified phase model (centroid-only, no per-vertex
/// phase integral). For the full Ling-Lee-Chuang triangle integral, use the CPU path.
/// The GPU path is used for real-time heatmap visualisation where approximate per-facet
/// values are sufficient; the final reported RCS number always comes from the CPU path.
/// </summary>
[ThreadGroupSize(DefaultThreadGroupSizes.X)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct RcsShader : IComputeShader
{
	public readonly ReadOnlyBuffer<FacetGpuData> Facets;
	public readonly ReadWriteBuffer<float> Real;
	public readonly ReadWriteBuffer<float> Imag;
	/// <summary>
	/// KHat: xyz = incident unit vector, w = wave number k (2π/λ)
	/// </summary>
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

		// cosθ = dot(normal, -k̂)  (normal points outward, k̂ points toward target)
		float cosTheta = -(f.Nx * KHat.X + f.Ny * KHat.Y + f.Nz * KHat.Z);

		if (cosTheta <= 0f) { Real[i] = 0f; Imag[i] = 0f; return; }

		float k = KHat.W;
		// Monostatic phase: 2k · (k̂ · centroid)
		float phase = 2f * k * (f.Cx * KHat.X + f.Cy * KHat.Y + f.Cz * KHat.Z);

		// Corrected amplitude: (k² / 2π) · Area · cosθ
		const float TwoPi = 6.28318530718f;
		float amp = (k * k / TwoPi) * f.Area * cosTheta;

		Real[i] = amp * Hlsl.Cos(phase);
		Imag[i] = amp * Hlsl.Sin(phase);
	}
}
