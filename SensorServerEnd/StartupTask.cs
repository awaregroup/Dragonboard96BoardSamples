// TODO : Customer friendly (C) notice required
// BME280 sensor - Temperature, Humidity and Air pressure
// https://github.com/gloveboxes/Windows-IoT-Core-Driver-Library
//
// Need to add a NuGet reference to Units.net V3.34 @ April 2019
//
// Grove BME280 Sensor in I2C1 (3V3)
// https://www.seeedstudio.com/Grove-Temp-Humi-Barometer-Sensor-BME280.html
//
namespace SensorServerEnd
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using Windows.ApplicationModel.AppService;
	using Windows.ApplicationModel.Background;
	using Windows.Foundation.Collections;

	using Glovebox.IoT.Devices.Sensors;

	public sealed class StartupTask : IBackgroundTask
	{
		private static BME280 bme280Sensor;
		private static Timer bme280InputPollingTimer;
		private static DateTime? lastUpdatedAtUTC = null;
		private static double temperature;
		private static double humidity;
		private static double airPressure;
		private readonly TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private readonly TimeSpan timerPeriod = new TimeSpan(0, 0, 30);
		private AppServiceConnection connection = null;
		private BackgroundTaskDeferral backgroundTaskDeferral = null;

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			backgroundTaskDeferral = taskInstance.GetDeferral();
			taskInstance.Canceled += TaskInstance_Canceled;

			Debug.WriteLine(Windows.ApplicationModel.Package.Current.Id.FamilyName);

			// Setup the BME280 sensor and timer
			if (bme280Sensor == null)
			{
				try
				{
					bme280Sensor = new BME280(0x76);

					bme280InputPollingTimer = new Timer(SensorUpdateTimerCallback, null, timerDue, timerPeriod);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}
			}

			var appServiceTrigger = taskInstance.TriggerDetails as AppServiceTriggerDetails;
			if (appServiceTrigger != null)
			{
				Debug.WriteLine("appServiceTrigger != null");

				// Verify that the app service connection is requesting the "" that this class provides
				if (appServiceTrigger.Name.Equals("SensorServerEnd"))
				{
					// Store the connection and subscribe to the "RequestRecieved" event to be notified when clients send messages
					connection = appServiceTrigger.AppServiceConnection;
					connection.RequestReceived += Connection_RequestReceived;
				}
				else
				{
					backgroundTaskDeferral.Complete();
					Debug.WriteLine("5");
				}
			}
			else
			{
				Debug.WriteLine("appServiceTrigger == null");
			}
		}

		private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
		{
			Debug.WriteLine("TaskInstance_Canceled");

			if (backgroundTaskDeferral != null)
			{
				backgroundTaskDeferral.Complete();
				backgroundTaskDeferral = null;
			}
		}

		private void SensorUpdateTimerCallback(object state)
		{
			try
			{
				temperature = bme280Sensor.Temperature.DegreesCelsius;
				humidity = bme280Sensor.Humidity;
				airPressure = bme280Sensor.Pressure.Kilopascals;
				lastUpdatedAtUTC = DateTime.UtcNow;

				Debug.WriteLine($"{lastUpdatedAtUTC} Timer triggered " +
					$"Temperature: {temperature}°C " +
					$"Humidity: {humidity:0.00}% " +
					$"Air pressure: {airPressure}KPa ");
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
		{
			ValueSet message = new ValueSet();

			Debug.WriteLine("Connection_RequestReceived");

			if (lastUpdatedAtUTC.HasValue)
			{
				message.Add("lastUpdatedAtUTC", lastUpdatedAtUTC.Value.ToString());
				message.Add("temperature", temperature);
				message.Add("humidity", humidity);
				message.Add("airPressure", airPressure);

				await args.Request.SendResponseAsync(message);
			}
		}
	}
}