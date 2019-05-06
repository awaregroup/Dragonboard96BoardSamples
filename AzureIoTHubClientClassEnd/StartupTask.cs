// BME280 sensor - Temperature, Humidity and Air pressure
//  https://github.com/gloveboxes/Windows-IoT-Core-Driver-Library
//
// Need to add a NuGet reference to Units.net V3.34 @ April 2019
//
// Grove BME280 Sensor in I2C1 (3V3)
//		https://www.seeedstudio.com/Grove-Temp-Humi-Barometer-Sensor-BME280.html
//
// Low overhead with fine grained control over formatting of payload but harder to construct complex/flexible payloads on the fly.
//
namespace AzureIoTHubClientClassEnd
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading;

	using Microsoft.Azure.Devices.Client;
	using Windows.ApplicationModel.Background;

	using Newtonsoft.Json;
	using Glovebox.IoT.Devices.Sensors;

	public sealed class StartupTask : IBackgroundTask
	{
		private const string AzureIoTHubConnectionString = "HostName=Build2019Test.azure-devices.net;DeviceId=DragonBoard410C;SharedAccessKey=SuEwxR79vrt/GE32ZjKW3SeqeGMFt+5qX4tK0WXBDIg=";
		private readonly TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private readonly TimeSpan timerPeriod = new TimeSpan(0, 0, 30);
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private BME280 bme280Sensor;
		private Timer bme280InputPollingTimer;
		private DeviceClient azureIoTHubClient = null;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				bme280Sensor = new BME280(0x76);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"BME280 Initialisation failed:{ex.Message}");

				return;
			}

			try
			{
				azureIoTHubClient = DeviceClient.CreateFromConnectionString(AzureIoTHubConnectionString, TransportType.Mqtt);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"CreateFromConnectionString failed:{ex.Message}");
				return;
			}

			bme280InputPollingTimer = new Timer(SensorUpdateTimerCallback, null, timerDue, timerPeriod);

			//enable task to continue running in background
			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private async void SensorUpdateTimerCallback(object state)
		{
			try
			{
				UnitsNet.Temperature temperature = bme280Sensor.Temperature;
				double humidity = bme280Sensor.Humidity;
				UnitsNet.Pressure airPressure = bme280Sensor.Pressure;

				Debug.WriteLine($"{DateTime.UtcNow.ToLongTimeString()} Timer triggered " +
							$"Temperature: {temperature.DegreesCelsius:0.0}°C {temperature.DegreesFahrenheit:0.0}°F " +
							$"Humidity: {humidity:0.0}% " +
							$"AirPressure: {airPressure.Kilopascals:0.000}KPa ");

				SensorPayload sensorPayload = new SensorPayload()
				{
					Temperature = temperature.DegreesCelsius,
					Humidity = humidity,
					AirPressure = airPressure.Kilopascals
				};

				string payloadText = JsonConvert.SerializeObject(sensorPayload);

				using (var message = new Message(Encoding.ASCII.GetBytes(payloadText)))
				{
					Debug.WriteLine("AzureIoTHubClient SendEventAsync starting");
					await azureIoTHubClient.SendEventAsync(message);
					Debug.WriteLine("AzureIoTHubClient SendEventAsync finished");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Retrieving or sending sensor values failed:{ex.Message}");
			}
		}
	}

	public sealed class SensorPayload
	{
		public double Temperature { get; set; }
		public double Humidity { get; set; }
		public double AirPressure { get; set; }
	}
}
