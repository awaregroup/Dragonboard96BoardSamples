// BME280 sensor - Temperature, Humidity and Air pressure
//  https://github.com/gloveboxes/Windows-IoT-Core-Driver-Library
//
// Need to add a NuGet reference to Units.net V3.34 @ April 2019
//
// Grove BME280 Sensor in I2C1 (3V3)
//		https://www.seeedstudio.com/Grove-Temp-Humi-Barometer-Sensor-BME280.html
//
namespace BME280SensorEnd
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using Windows.ApplicationModel.Background;
	using Glovebox.IoT.Devices.Sensors;

	public sealed class StartupTask : IBackgroundTask
	{
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private BME280 bme280Sensor;
		private Timer bme280InputPollingTimer;
		private readonly TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private readonly TimeSpan timerPeriod = new TimeSpan(0, 0, 30);

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				bme280Sensor = new BME280(0x76);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}

			bme280InputPollingTimer = new Timer(SensorUpdateTimerCallback, null, timerDue, timerPeriod);

			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private void SensorUpdateTimerCallback(object state)
		{
			try
			{
				Debug.WriteLine($"{DateTime.UtcNow.ToShortTimeString()} Timer triggered " +
							$"Temperature: {bme280Sensor.Temperature.DegreesCelsius}°C " +
							$"Humidity: {bme280Sensor.Humidity:0.00}% " +
							$"Air pressure: {bme280Sensor.Pressure.Kilopascals}KPa ");
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}
	}
}
