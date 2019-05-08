// TODO : Customer friendly (C) notice required
// 96 board schematic 
// https://github.com/96boards/96boards-sensors/raw/master/Sensors.pdf
// DragonBoard Windows 10 pin mappings 
// https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb
//
// Grove Mini PIR Motion Sensor in G3 
// https://www.seeedstudio.com/Grove-mini-PIR-motion-sensor-p-2930.html
// Occupancy detection for Smart Builidng scenario
//
namespace PIRSensorEnd
{
	using System;
	using System.Diagnostics;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	public sealed class StartupTask : IBackgroundTask
	{
		private const int InterruptPinNumber = 24;
		private GpioPin interruptGpioPin = null;
		private BackgroundTaskDeferral backgroundTaskDeferral = null;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				GpioController gpioController = GpioController.GetDefault();

				interruptGpioPin = gpioController.OpenPin(InterruptPinNumber);
				interruptGpioPin.SetDriveMode(GpioPinDriveMode.InputPullDown);

				interruptGpioPin.ValueChanged += InterruptGpioPin_ValueChanged; 
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

			Debug.WriteLine($"{DateTime.UtcNow.ToLongTimeString()} PIR Interrupt {sender.PinNumber} triggered {args.Edge}");
		}
	}
}
