NB- Don't upgrade to latest version of Microsoft.Azure.Devices.Client (i.e. later than 1.19 ) without careful testing.

If DragonBoard realtime clock not set to roughly correct time all uploads will failed as SaS key invalid, follow steps here remote powershell in if 
NTP doesn't work after rebboot
https://stackoverflow.com/questions/30585900/how-to-set-system-time-in-windows-10-iot

or run the NTPClient in utilities items folder

Use Azure IoT Hub Device Explorer to look at data from devices, send commands, inspect devices properties etc.
https://github.com/Azure/azure-iot-sdk-csharp/tree/master/tools/DeviceExplorer

If TPM Initialisation fails
https://discuss.96boards.org/t/windows-10-iot-tpm-configuration-fails/4250

Setting the TPM Azure connections string from IoT Dashboard
https://blogs.windows.com/buildingapps/2016/07/20/building-secure-apps-for-windows-iot-core/#zi4A4c0VTkFZEfSU.97

Desired state JSON
{
  "deviceId": "DragonBoard410C",
  "etag": "AAAAAAAAAAI=",
  "deviceEtag": "MTQ0OTY1OTk1",
  "status": "enabled",
  "statusUpdateTime": "0001-01-01T00:00:00",
  "connectionState": "Disconnected",
  "lastActivityTime": "2019-04-27T02:20:09.8098574",
  "cloudToDeviceMessageCount": 0,
  "authenticationType": "sas",
  "x509Thumbprint": {
    "primaryThumbprint": null,
    "secondaryThumbprint": null
  },
  "version": 5,
  "properties": {
    "desired": {
      "TimerDue": "00:00:15",
      "TimerPeriod": "00:00:45",
      "$metadata": {
        "$lastUpdated": "2019-04-27T02:24:01.4344953Z",
        "$lastUpdatedVersion": 2,
        "TimerDue": {
          "$lastUpdated": "2019-04-27T02:24:01.4344953Z",
          "$lastUpdatedVersion": 2
        },
        "TimerPeriod": {
          "$lastUpdated": "2019-04-27T02:24:01.4344953Z",
          "$lastUpdatedVersion": 2
        }
      },
      "$version": 2
    },
    "reported": {
      "Timezone": "(UTC+12:00) Auckland, Wellington",
      "OSVersion": "Microsoft Windows NT 10.0.17763.0",
      "MachineName": "DB410C",
      "ApplicationDisplayName": "AzureIoTHubClientPropertiesEnd",
      "ApplicationName": "AzureIoTHubClientPropertiesEnd-uwp",
      "ApplicationVersion": "1.0.0.0",
      "SystemId": "60-1B-29-3F-56-35-A7-A9-AD-D4-1E-32-7E-28-79-E2-CA-44-B9-49-CB-A8-42-25-D3-10-7A-AE-03-2B-49-92",
      "$metadata": {
        "$lastUpdated": "2019-04-27T02:19:28.7713807Z",
        "Timezone": {
          "$lastUpdated": "2019-04-27T02:19:28.7713807Z"
        },
        "OSVersion": {
          "$lastUpdated": "2019-04-27T02:19:28.7713807Z"
        },
        "MachineName": {
          "$lastUpdated": "2019-04-27T02:19:28.7713807Z"
        },
        "ApplicationDisplayName": {
          "$lastUpdated": "2019-04-27T02:19:28.7713807Z"
        },
        "ApplicationName": {
          "$lastUpdated": "2019-04-27T02:19:28.7713807Z"
        },
        "ApplicationVersion": {
          "$lastUpdated": "2019-04-27T02:19:28.7713807Z"
        },
        "SystemId": {
          "$lastUpdated": "2019-04-27T02:19:28.7713807Z"
        }
      },
      "$version": 3
    }
  },
  "capabilities": {
    "iotEdge": false
  }
}
