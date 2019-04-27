// BME280 sensor - Temperature, Humidity and Air pressure
//  https://github.com/gloveboxes/Windows-IoT-Core-Driver-Library
//
// Need to add a NuGet reference to Units.net V3.34 @ April 2019
namespace AzureIoTHubClientMethodHandlerEnd
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading;

	using Microsoft.Azure.Devices.Client;
	using Windows.ApplicationModel.Background;

	using Newtonsoft.Json;
	using Glovebox.IoT.Devices.Sensors;
	using Windows.System;
	using System.Threading.Tasks;
	using Windows.Devices.Gpio;

	public sealed class StartupTask : IBackgroundTask
	{
		private const string AzureIoTHubConnectionString = "HostName=Build2019Test.azure-devices.net;DeviceId=DragonBoard410C;SharedAccessKey=SuEwxR79vrt/GE32ZjKW3SeqeGMFt+5qX4tK0WXBDIg=";
		private readonly TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private readonly TimeSpan timerPeriod = new TimeSpan(0, 0, 30);
		private readonly TimeSpan DeviceRestartPeriod = new TimeSpan(0, 0, 25);
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private BME280 bme280Sensor;
		private Timer bme280InputPollingTimer;
		private readonly int outputGpioPinNumber = 35;
		private GpioPin outputGpioPin = null;
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
				GpioController gpioController = GpioController.GetDefault();
				outputGpioPin = gpioController.OpenPin(outputGpioPinNumber);
				outputGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Actuator GPIO Initialisation failed:{ex.Message}");

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
				azureIoTHubClient.SetMethodHandlerAsync("Restart", RestartAsync, null);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Azure IoT Hub device method handler configuration failed:{ex.Message}");
				return;
			}

			try
			{
				azureIoTHubClient.SetMethodHandlerAsync("ActuatorToggle", ActuatorAsync, null);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Azure IoT Hub device Actuator method handler configuration failed:{ex.Message}");
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

				SensorPayloadDto sensorPayload = new SensorPayloadDto()
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

#pragma warning disable 1998
		private async Task<MethodResponse> RestartAsync(MethodRequest methodRequest, object userContext)
		{
			Debug.WriteLine("RestartAsync initiated");

			if (bme280InputPollingTimer != null)
			{
				bme280InputPollingTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}

			ShutdownManager.BeginShutdown(ShutdownKind.Restart, DeviceRestartPeriod);

			return new MethodResponse(200);
		}

		private async Task<MethodResponse> ActuatorAsync(MethodRequest methodRequest, object userContext)
		{
			Debug.WriteLine("ActuatorAsync initiated");

			if (outputGpioPin.Read() == GpioPinValue.High)
			{
				outputGpioPin.Write(GpioPinValue.Low);
			}
			else
			{
				outputGpioPin.Write(GpioPinValue.High);
			}
			return new MethodResponse(200);
		}
#pragma warning restore 1998
	}

	public sealed class SensorPayloadDto
	{
		public double Temperature { get; set; }
		public double Humidity { get; set; }
		public double AirPressure { get; set; }
	}
}
