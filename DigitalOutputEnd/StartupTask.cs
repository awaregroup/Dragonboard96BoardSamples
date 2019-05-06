// 96 board schematic 
//		https://github.com/96boards/96boards-sensors/raw/master/Sensors.pdf
// DragonBoard Windows 10 pin mappings 
//		https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb
//
// Grove LED in socket G4
//		https://www.seeedstudio.com/Grove-Red-LED-p-1142.html
//		https://www.seeedstudio.com/Grove-Green-LED.html
//		https://www.seeedstudio.com/Grove-Blue-LED.html
//		https://www.seeedstudio.com/Grove-White-LED-p-1140.html
//
namespace DigitalOutputEnd
{
	using System;
	using System.Threading;
	using System.Diagnostics;
	using Windows.ApplicationModel.Background;
	using Windows.Devices.Gpio;

	public sealed class StartupTask : IBackgroundTask
	{
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private readonly TimeSpan timerDue = new TimeSpan(0, 0, 5);
		private readonly TimeSpan timerPeriod = new TimeSpan(0, 0, 1);
		private readonly int outputGpioPinNumber = 35;
		private Timer digitalOutpuUpdatetimer;
		private GpioPin outputGpioPin = null;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				GpioController gpioController = GpioController.GetDefault();
				outputGpioPin = gpioController.OpenPin(outputGpioPinNumber);
				outputGpioPin.SetDriveMode(GpioPinDriveMode.Output);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);

				return;
			}
			digitalOutpuUpdatetimer = new Timer(TimerCallback, null, timerDue, timerPeriod);

			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private void TimerCallback(object state)
		{
			DateTime currentTime = DateTime.UtcNow;
			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Timer triggered");

			if (outputGpioPin.Read() == GpioPinValue.High)
			{
				outputGpioPin.Write(GpioPinValue.Low);
			}
			else
			{
				outputGpioPin.Write(GpioPinValue.High);
			}
		}
	}
}
