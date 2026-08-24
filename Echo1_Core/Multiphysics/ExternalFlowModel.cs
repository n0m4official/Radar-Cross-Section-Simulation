namespace Echo1.Core.Multiphysics;

public sealed class ExternalFlowModel
{
	private const double AirGasConstant = 287.05;
	private const double AirSpecificHeat = 1006.0;

	public double GetConvectionCoefficient(FlowConditions flow)
	{
		double filmTemperature = Math.Max(150.0, flow.AirTemperatureK);

		double density = flow.PressurePa / (AirGasConstant * filmTemperature);
		double viscosity = 1.716e-5
			* Math.Pow(filmTemperature / 273.15, 1.5)
			* (273.15 + 111.0) / (filmTemperature + 111.0);

		double conductivity = 0.024 + 7.0e-5 * (filmTemperature - 273.15);
		double prandtl = 0.71;
		double length = Math.Max(flow.CharacteristicLengthM, 1e-6);

		double reynolds = density * flow.VelocityMps * length / viscosity;

		double nusselt = reynolds < 5e5
			? 0.664 * Math.Sqrt(Math.Max(reynolds, 0.0))
				* Math.Pow(prandtl, 1.0 / 3.0)
			: (0.037 * Math.Pow(reynolds, 0.8) - 871.0)
				* Math.Pow(prandtl, 1.0 / 3.0);

		return Math.Max(0.1, nusselt * conductivity / length);
	}

	public void StepAirTemperature(
		FlowConditions flow,
		double heatIntoAirW,
		double dtSeconds)
	{
		double thermalMass =
			Math.Max(flow.ControlVolumeAirMassKg, 1e-6) * AirSpecificHeat;

		double exhaustCoolingW =
			flow.MassFlowRateKgPerS
			* AirSpecificHeat
			* (flow.AirTemperatureK - flow.AmbientTemperatureK);

		flow.AirTemperatureK +=
			(heatIntoAirW - exhaustCoolingW) * dtSeconds / thermalMass;
	}
}