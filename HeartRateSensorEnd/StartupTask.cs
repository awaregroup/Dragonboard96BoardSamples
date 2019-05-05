// 96 board schematic 
//		https://github.com/96boards/96boards-sensors/raw/master/Sensors.pdf
// DragonBoard Windows 10 pin mappings 
//		https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb
// Seeedstudio Ear-clip Heart Rate Sensor
//    https://www.seeedstudio.com/Grove-Ear-clip-Heart-Rate-Sensor-p-1116.html
// Seeedstudio LED one of
//    https://www.seeedstudio.com/Grove-Red-LED-p-1142.html
//    https://www.seeedstudio.com/Grove-White-LED-p-1140.html
//    https://www.seeedstudio.com/Grove-Blue-LED.html<summary>
//    https://www.seeedstudio.com/Grove-White-LED-p-1140.html
//
// Make the LED Flash on for a set period each heart beat. Heart beat pulse turns on LED and starts timer to turn it off, 
//
// Then count the heart heats in a period and upload to Azure IoT Hub
//
namespace HeartRateSensorEnd
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	using Microsoft.Azure.Devices.Client;
	using Newtonsoft.Json;

	public sealed class StartupTask : IBackgroundTask
	{
		private const string AzureIoTHubConnectionString = "HostName=Build2019Test.azure-devices.net;DeviceId=DragonBoard410C;SharedAccessKey=SuEwxR79vrt/GE32ZjKW3SeqeGMFt+5qX4tK0WXBDIg=";
		private DeviceClient azureIoTHubClient = null;
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private readonly TimeSpan timerPeriodLedIlluminated = new TimeSpan(0, 0, 0, 0, 10);
		private readonly TimeSpan timerPeriodInfinite = new TimeSpan(0, 0, 0);
		private GpioPin heartBeatSensorGpioPin = null;
		private const int heartBeatSensorPinNumber = 24;
		private GpioPin heartBeatDisplayGpioPin = null;
		private const int heartBeatDisplayGpioPinNumber = 35;
		private Timer heartBeatDisplayOffTimer;
		private Timer heartBeatMeasurmentTimer;
		private readonly TimeSpan heartBeatMeasurementPeriod = new TimeSpan(0, 0, 15);
		private int heartBeatCountInMeasurementPeriod = 0;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			heartBeatDisplayOffTimer = new Timer(LedOffCallback, null, Timeout.Infinite, Timeout.Infinite);

			try
			{
				GpioController gpioController = GpioController.GetDefault();

				heartBeatDisplayGpioPin = gpioController.OpenPin(heartBeatDisplayGpioPinNumber);
				heartBeatDisplayGpioPin.SetDriveMode(GpioPinDriveMode.Output);
				heartBeatDisplayGpioPin.Write(GpioPinValue.Low);

				heartBeatSensorGpioPin = gpioController.OpenPin(heartBeatSensorPinNumber);
				heartBeatSensorGpioPin.SetDriveMode(GpioPinDriveMode.InputPullDown);
				heartBeatSensorGpioPin.ValueChanged += InterruptGpioPin_ValueChanged; ;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
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

			heartBeatMeasurmentTimer = new Timer(MeasurementCallbackAsync, null, heartBeatMeasurementPeriod, heartBeatMeasurementPeriod);

			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private void InterruptGpioPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
		{
			DateTime currentTime = DateTime.UtcNow;

			// ignore the falling edge of heart beat sensor pulse
			if (args.Edge == GpioPinEdge.FallingEdge)
			{
				return;
			}

			Interlocked.Increment(ref heartBeatCountInMeasurementPeriod);

			heartBeatDisplayGpioPin.Write(GpioPinValue.High);

			// Start the timer to turn the LED off
			heartBeatDisplayOffTimer.Change(timerPeriodLedIlluminated, timerPeriodInfinite);
		}

		private void LedOffCallback(object state)
		{
			heartBeatDisplayGpioPin.Write(GpioPinValue.Low);
		}

		private async void MeasurementCallbackAsync(object state)
		{
			DateTime currentTime = DateTime.UtcNow;

			Debug.WriteLine($"{currentTime:hh:mm:ss} MeasurementCallback");

			int heartBeatCount = Interlocked.Exchange(ref heartBeatCountInMeasurementPeriod, 0);

			double bpm = heartBeatCount * (new TimeSpan(0, 1, 0) / heartBeatMeasurementPeriod);
			Debug.WriteLine($" {bpm:0}BPM");

			// Prepare the payload for sending to Azure IoT Hub
			SensorPayload sensorPayload = new SensorPayload()
			{
				UpdatedAtUtC = currentTime,
				Bpm = bpm
			};

			string payloadText = JsonConvert.SerializeObject(sensorPayload);

			try
			{
				using (var message = new Message(Encoding.ASCII.GetBytes(payloadText)))
				{
					Debug.WriteLine("AzureIoTHubClient SendEventAsync starting");
					await azureIoTHubClient.SendEventAsync(message);
					Debug.WriteLine("AzureIoTHubClient SendEventAsync finished");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Sending pulse rate values failed:{ex.Message}");
			}
		}

		private class SensorPayload
		{
			[JsonProperty(propertyName: "UpdatedAtUTC")]
			public DateTime UpdatedAtUtC { get; set; }

			[JsonProperty(propertyName: "BPM")]
			public double Bpm { get; set; }
		}
	}
}
