using System.Runtime.InteropServices;

namespace Echo1.Core.Gpu;

[StructLayout(LayoutKind.Sequential)]
public struct FacetGpuData
{
	public float Nx, Ny, Nz;   // normal
	public float Cx, Cy, Cz;   // centroid
	public float Area;
	public float _pad;         // ensures 16‑byte alignment
}