// 96 board schematic 
//		https://github.com/96boards/96boards-sensors/raw/master/Sensors.pdf
// DragonBoard Windows 10 pin mappings 
//		https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb
// Seeedstudio Ear-clip Heart Rate Sensor in G3
//    https://www.seeedstudio.com/Grove-Ear-clip-Heart-Rate-Sensor-p-1116.html
// Seeedstudio LED one of in G4
//    https://www.seeedstudio.com/Grove-Red-LED-p-1142.html
//    https://www.seeedstudio.com/Grove-White-LED-p-1140.html
//    https://www.seeedstudio.com/Grove-Blue-LED.html<summary>
//    https://www.seeedstudio.com/Grove-White-LED-p-1140.html
//
// Proves hardware setup working by toggling the state of the LED on each heart beat.
//
namespace HeartRateSensorToggle
{
	using System;
	using System.Diagnostics;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	public sealed class StartupTask : IBackgroundTask
	{
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private GpioPin HeartBeatSensorGpioPin = null;
		private const int heartBeatSensorPinNumber = 24;
		private GpioPin heartBeatDisplayGpioPin = null;
		private const int heartBeatDisplayGpioPinNumber = 35;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				GpioController gpioController = GpioController.GetDefault();

				heartBeatDisplayGpioPin = gpioController.OpenPin(heartBeatDisplayGpioPinNumber);
				heartBeatDisplayGpioPin.SetDriveMode(GpioPinDriveMode.Output);
				heartBeatDisplayGpioPin.Write(GpioPinValue.Low);

				HeartBeatSensorGpioPin = gpioController.OpenPin(heartBeatSensorPinNumber);
				HeartBeatSensorGpioPin.SetDriveMode(GpioPinDriveMode.InputPullDown);
				HeartBeatSensorGpioPin.ValueChanged += InterruptGpioPin_ValueChanged; ;
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

			Debug.WriteLine($"{DateTime.UtcNow.ToLongTimeString()} Interrupt {sender.PinNumber} triggered {args.Edge}");

			// ignore the falling edge of heart beat sensor pulse
			if ( args.Edge == GpioPinEdge.FallingEdge)
			{
				return;
			}

			// Toggle the state of the LED on each leading edge
			if (heartBeatDisplayGpioPin.Read() == GpioPinValue.High)
			{
				heartBeatDisplayGpioPin.Write(GpioPinValue.Low);
			}
			else
			{
				heartBeatDisplayGpioPin.Write(GpioPinValue.High);
			}
		}
	}
}
