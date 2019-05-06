// BME280 sensor - Temperature, Humidity and Air pressure
//  https://github.com/gloveboxes/Windows-IoT-Core-Driver-Library
//
// Need to add a NuGet reference to Units.net V3.34 @ April 2019
//
// Connection string set in Device TPM using code or IoT Dashboard -> Device Portal
//
// Grove BME280 Sensor in I2C1 (3V3)
//		https://www.seeedstudio.com/Grove-Temp-Humi-Barometer-Sensor-BME280.html
//
// Set TimerDue & TimerPeriod using sample JSON on readme.txt file
//
namespace AzureIoTHubClientTPMEnd
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Azure.Devices.Client;
	using Microsoft.Azure.Devices.Shared;
	using Microsoft.Devices.Tpm;
	using Windows.ApplicationModel;
	using Windows.ApplicationModel.Background;
	using Windows.Storage.Streams;
	using Windows.System;
	using Windows.System.Profile;

	using Glovebox.IoT.Devices.Sensors;
	using Newtonsoft.Json;

	public sealed class StartupTask : IBackgroundTask
	{
		private readonly TimeSpan deviceRestartPeriod = new TimeSpan(0, 0, 25);
		private readonly TimeSpan sasTokenValidityPeriod = new TimeSpan(0, 5, 0);
		private string azureIoTHubUri;
		private string deviceId;
		private string sasToken;
		private DateTime sasTokenIssuedAtUtc;
		private TimeSpan timerDue = new TimeSpan(0, 0, 10);
		private TimeSpan timerPeriod = new TimeSpan(0, 0, 30);
		private BackgroundTaskDeferral backgroundTaskDeferral = null;
		private BME280 bme280Sensor;
		private Timer bme280InputPollingTimer;
		private DeviceClient azureIoTHubClient = null;
		private Twin deviceTwin = null;

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
				TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM

				azureIoTHubUri = myDevice.GetHostName();
				deviceId = myDevice.GetDeviceId();
				sasToken = myDevice.GetSASToken((uint)sasTokenValidityPeriod.TotalSeconds);
				sasTokenIssuedAtUtc = DateTime.UtcNow;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"TpmDevice.GetSASToken failed:{ex.Message}");
				return;
			}

			try
			{
				azureIoTHubClient = DeviceClient.Create(azureIoTHubUri, AuthenticationMethodFactory.CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Mqtt);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"DeviceClient.Create with TPM info failed:{ex.Message}");
				return;
			}

			try
			{
				azureIoTHubClient.SetMethodHandlerAsync("Restart", RestartAsync, null);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Azure IoT Hub device method handler configuration failed:{ex.Message}");
				return;
			}

			try
			{
				TwinCollection reportedProperties;
				reportedProperties = new TwinCollection();

				// This is from the OS 
				reportedProperties["Timezone"] = TimeZoneSettings.CurrentTimeZoneDisplayName;
				reportedProperties["OSVersion"] = Environment.OSVersion.VersionString;
				reportedProperties["MachineName"] = Environment.MachineName;

				// This is from the application manifest 
				Package package = Package.Current;
				PackageId packageId = package.Id;
				PackageVersion version = packageId.Version;
				reportedProperties["ApplicationDisplayName"] = package.DisplayName;
				reportedProperties["ApplicationName"] = packageId.Name;
				reportedProperties["ApplicationVersion"] = string.Format($"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");

				// Unique identifier from the hardware
				SystemIdentificationInfo systemIdentificationInfo = SystemIdentification.GetSystemIdForPublisher();
				using (DataReader reader = DataReader.FromBuffer(systemIdentificationInfo.Id))
				{
					byte[] bytes = new byte[systemIdentificationInfo.Id.Length];
					reader.ReadBytes(bytes);
					reportedProperties["SystemId"] = BitConverter.ToString(bytes);
				}

				azureIoTHubClient.UpdateReportedPropertiesAsync(reportedProperties).Wait();
			}
			catch (Exception ex)
			{
				Debug.Print($"Azure IoT Hub device twin configuration retrieval failed:{ex.Message}");
				return;
			}

			try
			{
				deviceTwin = azureIoTHubClient.GetTwinAsync().Result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Azure IoT Hub device twin configuration retrieval failed:{ex.Message}");
				return;
			}

			try
			{
				if (deviceTwin.Properties.Desired.Contains("TimerDue"))
				{
					timerDue = TimeSpan.Parse(deviceTwin.Properties.Desired["TimerDue"].ToString());
				}

				if (deviceTwin.Properties.Desired.Contains("TimerPeriod"))
				{
					timerPeriod = TimeSpan.Parse(deviceTwin.Properties.Desired["TimerPeriod"].ToString());
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Sensor due or period configuration retrieval failed using default:{ex.Message}");
				return;
			}

			bme280InputPollingTimer = new Timer(SensorUpdateTimerCallback, null, timerDue, timerPeriod);

			//enable task to continue running in background
			backgroundTaskDeferral = taskInstance.GetDeferral();
		}

		private async void SensorUpdateTimerCallback(object state)
		{
			try
			{
				// Checking that SaS token isn't about to expire
				if ((DateTime.UtcNow - sasTokenIssuedAtUtc) >= sasTokenValidityPeriod)
				{
					Debug.WriteLine($"{DateTime.UtcNow.ToString("hh:mm:ss")} SAS token needs renewing");

					try
					{
						TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM

						azureIoTHubUri = myDevice.GetHostName();
						deviceId = myDevice.GetDeviceId();
						sasToken = myDevice.GetSASToken((uint)sasTokenValidityPeriod.TotalSeconds);
						sasTokenIssuedAtUtc = DateTime.UtcNow;
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"TpmDevice.GetSASToken refresh failed:{ex.Message}");
						return;
					}

					try
					{
						azureIoTHubClient = DeviceClient.Create(azureIoTHubUri, AuthenticationMethodFactory.CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Mqtt);
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"DeviceClient.Create with TPM info failed:{ex.Message}");
						return;
					}

					Debug.WriteLine($"{DateTime.UtcNow.ToString("hh:mm:ss")} SAS token renewed ");
				}

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
					AirPressure = airPressure.Kilopascals
				};

				string payloadText = JsonConvert.SerializeObject(sensorPayload);

				using (var message = new Message(Encoding.ASCII.GetBytes(payloadText)))
				{
					Debug.WriteLine($" {DateTime.UtcNow.ToString("hh:mm:ss.fff")} AzureIoTHubClient SendEventAsync starting");
					await azureIoTHubClient.SendEventAsync(message);
					Debug.WriteLine($" {DateTime.UtcNow.ToString("hh:mm:ss.fff")} AzureIoTHubClient SendEventAsync finished");
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
			if (bme280InputPollingTimer != null)
			{
				bme280InputPollingTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}

			ShutdownManager.BeginShutdown(ShutdownKind.Restart, deviceRestartPeriod);

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
