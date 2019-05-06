// 96 board schematic 
//		https://github.com/96boards/96boards-sensors/raw/master/Sensors.pdf
// DragonBoard Windows 10 pin mappings 
//		https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb
//
//	Grove button in G3
//		https://www.seeedstudio.com/Grove-Button-P.html
//		https://www.seeedstudio.com/Grove-Red-LED-Button.html
//		https://www.seeedstudio.com/Grove-Blue-LED-Button-p-3104.html
//		https://www.seeedstudio.com/Grove-Yellow-LED-Button-p-3101.html
//		https://www.seeedstudio.com/Grove-Mech-Keycap.html
//		https://www.seeedstudio.com/Grove-Touch-Sensor.html
//
namespace DigitalInterruptEnd
{
	using System;
	using System.Diagnostics;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	public sealed class StartupTask : IBackgroundTask
	{
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private GpioPin interruptGpioPin = null;
		private const int interruptPinNumber = 24;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				GpioController gpioController = GpioController.GetDefault();

				interruptGpioPin = gpioController.OpenPin(interruptPinNumber);
				interruptGpioPin.SetDriveMode(GpioPinDriveMode.InputPullDown);

				interruptGpioPin.ValueChanged += InterruptGpioPin_ValueChanged; ;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}

			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private void InterruptGpioPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
		{
			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Digital Input Interrupt {sender.PinNumber} triggered {args.Edge}");
		}
	}
}
