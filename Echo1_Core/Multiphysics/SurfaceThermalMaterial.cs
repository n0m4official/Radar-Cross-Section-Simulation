using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo1.Core.Multiphysics
{
	public sealed record SurfaceThermalMaterial(
		double DensityKgM3,
		double SpecificHeatJkgK,
		double ConductivityWmK,
		double ThicknessM,
		double Emissivity)
	{
		public static readonly SurfaceThermalMaterial GenericMetal = new(
			 DensityKgM3: 2_700,
			 SpecificHeatJkgK: 900,
			 ConductivityWmK: 160,
			 ThicknessM: 0.002,
			 Emissivity: 0.25);

		public static readonly SurfaceThermalMaterial GenericCoating = new(
			DensityKgM3: 1_400,
			SpecificHeatJkgK: 1_000,
			ConductivityWmK: 0.3,
			ThicknessM: 0.005,
			Emissivity: 0.85);
	}
}
