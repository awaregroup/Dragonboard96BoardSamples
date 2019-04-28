// 96 board schematic 
//		https://github.com/96boards/96boards-sensors/raw/master/Sensors.pdf
// DragonBoard Windows 10 pin mappings 
//		https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb
//
namespace HCSR04SensorEnd
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using Windows.ApplicationModel.Background;

	using Glovebox.IoT.Devices.Sensors.Distance;

	public sealed class StartupTask : IBackgroundTask
	{
		private readonly TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private readonly TimeSpan timerPeriod = new TimeSpan(0, 0, 15);
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private  HCSR04 hcsr04;
		private Timer hcsr04InputPollingTimer;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				hcsr04 = new HCSR04(12,36);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}

			hcsr04InputPollingTimer = new Timer(SensorUpdateTimerCallback, null, timerDue, timerPeriod);

			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private void SensorUpdateTimerCallback(object state)
		{
			try
			{
				UnitsNet.Length distance = hcsr04.GetDistance();

				Debug.WriteLine($"{DateTime.UtcNow.ToShortTimeString()} Timer triggered " +
							$"Distance: {distance.Meters:0.00}M " +
							$" {distance.Centimeters:0.0}cm " +
							$" {distance.Millimeters}mm");
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}
	}
}
