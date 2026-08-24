using Echo1.Core.Engine;
using Echo1.Core.Geometry;
using Echo1.Core.Radar;

namespace Echo1.Core.Multiphysics;

public sealed class CoupledSimulation
{
	private readonly RcsMesh _mesh;
	private readonly RcsEngine _rcsEngine;
	private readonly ThermalMeshTopology _topology;
	private readonly FacetThermalSolver _thermalSolver = new();
	private readonly ExternalFlowModel _flowModel = new();

	public FacetState State { get; }
	public FlowConditions Flow { get; }
	public CoupledSimulationConfig Config { get; }

	public CoupledSimulation(
		RcsMesh mesh,
		RcsEngine rcsEngine,
		FlowConditions flow,
		CoupledSimulationConfig? config = null)
	{
		_mesh = mesh;
		_rcsEngine = rcsEngine;
		_topology = ThermalMeshTopology.From(mesh);

		State = new FacetState(mesh.Facets.Length, flow.AmbientTemperatureK);
		Flow = flow;
		Config = config ?? new CoupledSimulationConfig();
	}

	public CoupledStepResult Advance(RadarConfig radar)
	{
		int substeps = Math.Max(1, Config.CouplingSubsteps);
		double substepSeconds = Config.TimeStepSeconds / substeps;

		double maxDeltaK = 0.0;

		for (int step = 0; step < substeps; step++)
		{
			double[] absorbed = ElectromagneticAbsorptionKernel.ComputeAbsorbedPower(
				_mesh, radar, State);

			Array.Copy(absorbed, State.AbsorbedPowerW, absorbed.Length);

			ThermalStepResult thermal = _thermalSolver.Step(
				_mesh,
				_topology,
				State,
				Flow,
				_flowModel,
				substepSeconds);

			_flowModel.StepAirTemperature(
				Flow,
				thermal.HeatIntoAirW,
				substepSeconds);

			maxDeltaK = Math.Max(maxDeltaK, thermal.MaxTemperatureChangeK);
		}

		State.AdvanceRevision();

		// Temperature now feeds back into temperature-sensitive EM material properties.
		RcsResult rcs = _rcsEngine.Compute(_mesh, radar, State);

		return new CoupledStepResult(
			rcs,
			maxDeltaK,
			State.TemperatureK.Min(),
			State.TemperatureK.Max(),
			Flow.AirTemperatureK);
	}
}

public readonly record struct CoupledStepResult(
	RcsResult Rcs,
	double MaxFacetTemperatureChangeK,
	double MinimumFacetTemperatureK,
	double MaximumFacetTemperatureK,
	double AirTemperatureK);