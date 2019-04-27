// BME280 sensor - Temperature, Humidity and Air pressure
//  https://github.com/gloveboxes/Windows-IoT-Core-Driver-Library
//
// Need to add a NuGet reference to Units.net V3.34 @ April 2019
namespace AzureIoTHubClientDeviceTwinEnd
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading;

	using Microsoft.Azure.Devices.Client;
	using Microsoft.Azure.Devices.Shared;
	using Windows.ApplicationModel.Background;

	using Newtonsoft.Json;
	using Glovebox.IoT.Devices.Sensors;
	using Newtonsoft.Json.Linq;

	public sealed class StartupTask : IBackgroundTask
	{
		private const string AzureIoTHubConnectionString = "HostName=Build2019Test.azure-devices.net;DeviceId=DragonBoard410C;SharedAccessKey=SuEwxR79vrt/GE32ZjKW3SeqeGMFt+5qX4tK0WXBDIg=";
		private TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private TimeSpan timerPeriod = new TimeSpan(0, 0, 30);
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private BME280 bme280Sensor;
		private Timer bme280InputPollingTimer;
		private DeviceClient azureIoTHubClient = null;
		private Twin deviceTwin = null;

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

			try
			{
				deviceTwin = azureIoTHubClient.GetTwinAsync().Result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Azure IoT Hub device twin configuration retrieval failed:{ex.Message}");
				return;
			}

			try
			{
				if (deviceTwin.Properties.Desired.Contains("TimerDue"))
				{
					timerDue = TimeSpan.Parse(deviceTwin.Properties.Desired["TimerDue"].ToString());
				}

				if (deviceTwin.Properties.Desired.Contains("TimerPeriod"))
				{
					timerPeriod = TimeSpan.Parse(deviceTwin.Properties.Desired["TimerPeriod"].ToString());
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Sensor due or period configuration retrieval failed, using default:{ex.Message}");
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
				Debug.WriteLine($"{DateTime.UtcNow.ToString("hh:mm:ss")} Timer triggered " +
							$"Temperature: {bme280Sensor.Temperature.DegreesCelsius}°C " +
							$"Humidity: {bme280Sensor.Humidity:0.00}% " +
							$"AirPressure: {bme280Sensor.Pressure.Kilopascals}KPa ");

				SensorPayload sensorPayload = new SensorPayload()
				{
					Temperature = bme280Sensor.Temperature.DegreesCelsius,
					Humidity = bme280Sensor.Humidity,
					AirPressure = bme280Sensor.Pressure.Kilopascals
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
