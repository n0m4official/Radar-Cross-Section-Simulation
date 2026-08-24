namespace Echo1.Core.Multiphysics;

public sealed class FlowConditions
{
	public double AmbientTemperatureK { get; init; } = 293.15;
	public double AirTemperatureK { get; set; } = 293.15;
	public double VelocityMps { get; init; } = 20.0;
	public double PressurePa { get; init; } = 101_325.0;
	public double CharacteristicLengthM { get; init; } = 1.0;

	// Enables the fluid temperature to rise because of surface heating.
	public double ControlVolumeAirMassKg { get; init; } = 1.0;
	public double MassFlowRateKgPerS { get; init; } = 0.1;
}