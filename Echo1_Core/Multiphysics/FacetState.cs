using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo1.Core.Multiphysics
{
	public sealed class FacetState
	{
		public double[] TemperatureK { get; }
		public double[] AbsorbedPowerW { get; }
		public double[] ConvectionCoefficientWm2K { get; }

		// Increment after each completed coupled timestep
		public long Revision { get; private set; }

		public FacetState(int facetCount, double initialTemperatureK = 293.15)
		{
			TemperatureK = Enumerable.Repeat(initialTemperatureK, facetCount).ToArray();
			AbsorbedPowerW = new double[facetCount];
			ConvectionCoefficientWm2K = new double[facetCount];
		}

		public void AdvanceRevision()
		{
			Revision++;
		}
	}
}
