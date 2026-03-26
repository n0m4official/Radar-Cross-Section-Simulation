// Echo1_RcsSimulator\Echo1_Core\Gpu\FacetGpuData.cs
using System.Runtime.InteropServices;

namespace Echo1.Core.Gpu;

[StructLayout(LayoutKind.Sequential)]
public struct FacetGpuData
{
	public float Nx, Ny, Nz;
	public float Cx, Cy, Cz;
	public float Area;
	public float _pad;
}