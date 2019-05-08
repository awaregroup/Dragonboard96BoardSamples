// TODO : Customer friendly (C) notice required
// BME280 sensor - Temperature, Humidity and Air pressure
//  https://github.com/gloveboxes/Windows-IoT-Core-Driver-Library
//
// Need to add a NuGet reference to Units.net V3.34 @ April 2019
//
// Grove BME280 Sensor in I2C1 (3V3)
// https://www.seeedstudio.com/Grove-Temp-Humi-Barometer-Sensor-BME280.html
//
// Grove LED in socket G4
// https://www.seeedstudio.com/Grove-Red-LED-p-1142.html
// https://www.seeedstudio.com/Grove-Green-LED.html
// https://www.seeedstudio.com/Grove-Blue-LED.html
// https://www.seeedstudio.com/Grove-White-LED-p-1140.html
//
// Toggle the status of the LED with an "ActuatorToggle" method calls and remotely reboot the device with a "RestartDevice" method call
//
namespace AzureIoTHubClientMethodHandlerEnd
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Azure.Devices.Client;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;
	using Windows.System;

	using Glovebox.IoT.Devices.Sensors;
	using Newtonsoft.Json;

	public sealed class StartupTask : IBackgroundTask
	{
		private const string AzureIoTHubConnectionString = "HostName=Build2019Test.azure-devices.net;DeviceId=DragonBoard410C;SharedAccessKey=ewbUCMtd6Blau9vaQBqO/J6GlSxgbxPM5aWRgZz6N7c=";
		private const int OutputGpioPinNumber = 35;
		private readonly TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private readonly TimeSpan timerPeriod = new TimeSpan(0, 0, 30);
		private readonly TimeSpan deviceRestartPeriod = new TimeSpan(0, 0, 45);
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private BME280 bme280Sensor;
		private Timer bme280InputPollingTimer;
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
				outputGpioPin = gpioController.OpenPin(OutputGpioPinNumber);
				outputGpioPin.SetDriveMode(GpioPinDriveMode.Output);
				outputGpioPin.Write(GpioPinValue.Low);
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
				azureIoTHubClient.SetMethodHandlerAsync("RestartDevice", RestartAsync, null);
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

				SensorPayloadDto sensorPayload = new SensorPayloadDto()
				{
					Temperature = temperature.DegreesCelsius,
					Humidity = humidity,
					AirPressure = airPressure.Kilopascals,
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

			ShutdownManager.BeginShutdown(ShutdownKind.Restart, deviceRestartPeriod);

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
