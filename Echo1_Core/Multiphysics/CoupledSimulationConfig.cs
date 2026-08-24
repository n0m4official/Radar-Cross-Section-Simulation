namespace Echo1.Core.Multiphysics;

public sealed class CoupledSimulationConfig
{
	public double TimeStepSeconds { get; init; } = 0.05;
	public int CouplingSubsteps { get; init; } = 1;
}