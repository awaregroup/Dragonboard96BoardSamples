// TODO : Customer friendly (C) notice required
// Simple minimal functionality low dependency lirbary from Analog devices ADXL345 3 Axis Accelerometer
namespace Sensors
{
	using System;
	using Windows.Devices.Enumeration;
	using Windows.Devices.I2c;

	enum Register : byte
	{
		PowerControl = 0x2D,
		DataFormat = 0x31,
		XAxisData = 0x32,
		YAxisData = 0x34,
		ZAxisData = 0x36,
	}

	class Adxl345 : IDisposable
	{
		private const byte I2CAddress = 0x53; // Default 7-bit I2C address of the ADXL345 
		private const int AccelerometerResolution = 1024; // 10 bits of resolution
		private const int AccelerometerDynamicRange = 8; // Total dynamic range of 8G, since we're configuring it to +-4G */
		private const double UnitsPerG = AccelerometerResolution / AccelerometerDynamicRange; 
		private I2cDevice I2CAccelerometer;

		public async void Initialise(int I2CPort = 0, int I2CAddress = I2CAddress)
		{
			var settings = new I2cConnectionSettings(I2CAddress)
			{
				BusSpeed = I2cBusSpeed.FastMode,
			};

			string aqs = I2cDevice.GetDeviceSelector(); 
			var dis = await DeviceInformation.FindAllAsync(aqs);
			I2CAccelerometer = await I2cDevice.FromIdAsync(dis[I2CPort].Id, settings);
			if (I2CAccelerometer == null)
			{
				throw new ApplicationException("I2C device not found");
			}

			// 0x01 sets range to +- 4Gs 
			byte[] writeBufferDataFormat = new byte[] { (byte)Register.DataFormat, 0x01 }; 
			I2CAccelerometer.Write(writeBufferDataFormat);

			// 0x08 puts the accelerometer into measurement mode
			byte[] writeBufferPowerControl = new byte[] { (byte)Register.PowerControl, 0x08 }; 
			I2CAccelerometer.Write(writeBufferPowerControl);
		}

		public Acceleration Read()
		{
			byte[] readBuffer = new byte[6];  
			byte[] regAddressBuffer = new byte[] { (byte)Register.XAxisData }; 

			I2CAccelerometer.WriteRead(regAddressBuffer, readBuffer);

			/* In order to get the raw 16-bit data values, we need to concatenate two 8-bit bytes for each axis */
			short accelerationRawX = BitConverter.ToInt16(readBuffer, 0);
			short accelerationRawY = BitConverter.ToInt16(readBuffer, 2);
			short accelerationRawZ = BitConverter.ToInt16(readBuffer, 4);

			/* Convert raw values to G's */
			Acceleration accel = new Acceleration()
			{
				X = (double)accelerationRawX / UnitsPerG,
				Y = (double)accelerationRawY / UnitsPerG,
				Z = (double)accelerationRawZ / UnitsPerG,
			};

			return accel;
		}

		public void Dispose()
		{
			if (I2CAccelerometer != null)
			{
				I2CAccelerometer.Dispose();
			}
		}

		public struct Acceleration
		{
			public double X;
			public double Y;
			public double Z;
		}
	}
}