// ADXL345 sensor - X,Y & Z acceleration at upto +-16G
// https://www.seeedstudio.com/Grove-3-Axis-Digital-Accelerometer-16g-p-1156.html
// Minimal implementation insipred by I2C/SPI version in
// https://github.com/Microsoft/Windows-iotcore-samples
namespace ADXL345SensorEnd
{
	using System;
	using System.Threading;
	using System.Diagnostics;
	using Windows.ApplicationModel.Background;

	using Sensors;

	public sealed class StartupTask : IBackgroundTask
	{
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private Adxl345 adxl345 = new Adxl345();
		private Timer adxl345InputPollingTimer;
		private readonly TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private readonly TimeSpan timerPeriod = new TimeSpan(0, 0, 1);

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				adxl345.Initialise(1);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);

				return;
			}

			adxl345InputPollingTimer = new Timer(SensorUpdateTimerCallback, null, timerDue, timerPeriod);

			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private void SensorUpdateTimerCallback(object state)
		{
			try
			{
				Adxl345.Acceleration acceleration = adxl345.Read();

				Debug.WriteLine($"{DateTime.UtcNow.ToString("HH:mm:ss")} " +
							$" X:{acceleration.X:0.00}G" +
							$" Y:{acceleration.Y:0.00}G" +
							$" Z:{acceleration.Z:0.00}G");
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}
	}
}