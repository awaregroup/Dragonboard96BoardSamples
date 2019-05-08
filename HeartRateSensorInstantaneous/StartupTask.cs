// TODO : Customer friendly (C) notice required
// 96 board schematic 
// https://github.com/96boards/96boards-sensors/raw/master/Sensors.pdf
// DragonBoard Windows 10 pin mappings 
// https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb
// Seeedstudio Ear-clip Heart Rate Sensor in G3
// https://www.seeedstudio.com/Grove-Ear-clip-Heart-Rate-Sensor-p-1116.html
// Seeedstudio LED one of in G4
// https://www.seeedstudio.com/Grove-Red-LED-p-1142.html
// https://www.seeedstudio.com/Grove-White-LED-p-1140.html
// https://www.seeedstudio.com/Grove-Blue-LED.html<summary>
// https://www.seeedstudio.com/Grove-White-LED-p-1140.html
//
// Make the LED Flash on for a set period each heart beat. Heart beat pulse turns on LED and starts timer to turn it off
// The use the time between heartbeats to work out the pulse rate.
//
namespace HeartRateSensorInstantaneous
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	public sealed class StartupTask : IBackgroundTask
	{
		private const int HeartBeatSensorPinNumber = 24;
		private const int HeartBeatDisplayGpioPinNumber = 35;
		private readonly TimeSpan timerPeriodLedIlluminated = new TimeSpan(0, 0, 0, 0, 10);
		private readonly TimeSpan timerPeriodInfinite = new TimeSpan(0, 0, 0);
		private GpioPin heartBeatSensorGpioPin = null;
		private GpioPin heartBeatDisplayGpioPin = null;
		private Timer heartBeatDisplayOffTimer;
		private DateTime? heartBeatLastUtc = null;
		private BackgroundTaskDeferral backgroundTaskDeferral = null;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			heartBeatDisplayOffTimer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);

			try
			{
				GpioController gpioController = GpioController.GetDefault();

				heartBeatDisplayGpioPin = gpioController.OpenPin(HeartBeatDisplayGpioPinNumber);
				heartBeatDisplayGpioPin.SetDriveMode(GpioPinDriveMode.Output);
				heartBeatDisplayGpioPin.Write(GpioPinValue.Low);

				heartBeatSensorGpioPin = gpioController.OpenPin(HeartBeatSensorPinNumber);
				heartBeatSensorGpioPin.SetDriveMode(GpioPinDriveMode.InputPullDown);
				heartBeatSensorGpioPin.ValueChanged += InterruptGpioPin_ValueChanged; 
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}

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

			heartBeatDisplayGpioPin.Write(GpioPinValue.High);

			// Start the timer to turn the LED off
			heartBeatDisplayOffTimer.Change(timerPeriodLedIlluminated, timerPeriodInfinite);

			if (heartBeatLastUtc.HasValue)
			{
				TimeSpan gapBetweenHeatBeats = currentTime - heartBeatLastUtc.Value;

				double pulesRate = new TimeSpan(0, 1, 0).TotalMilliseconds / gapBetweenHeatBeats.TotalMilliseconds;

				Debug.WriteLine($" {pulesRate:0}BPM");
			}

			heartBeatLastUtc = currentTime;
		}

		private void TimerCallback(object state)
		{
			heartBeatDisplayGpioPin.Write(GpioPinValue.Low);
		}
	}
}
