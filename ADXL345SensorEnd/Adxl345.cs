
namespace Sensors
{
	using System;
	using Windows.Devices.Enumeration;
	using Windows.Devices.I2c;

	enum Register :byte
	{
		PowerControl = 0x2D,
		DataFormat = 0x31,
		XAxisData = 0x32,
		YAxisData = 0x34,
		ZAxisData = 0x36,
	}

	class Adxl345 : IDisposable
	{
		private const byte I2CAddress = 0x53;	// 7-bit I2C address of the ADXL345 with SDO pulled low
		private const int AccelerometerResolution = 1024; // 10 bits of resolution
		private const int AccelerometerDynamicRange = 8;    /* The ADXL345 had a total dynamic range of 8G, since we're configuring it to +-4G */
		private const double UnitsPerG = AccelerometerResolution / AccelerometerDynamicRange;  /* Ratio of raw int values to G units                          */
		private I2cDevice I2CAccelerometer;

		public async void Initialise(int I2CPort = 0, int I2CAddress = I2CAddress)
		{
			var settings = new I2cConnectionSettings(I2CAddress);
			settings.BusSpeed = I2cBusSpeed.FastMode;                       /* 400KHz bus speed */

			string aqs = I2cDevice.GetDeviceSelector();                     /* Get a selector string that will return all I2C controllers on the system */
			var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller devices with our selector string             */
			I2CAccelerometer = await I2cDevice.FromIdAsync(dis[I2CPort].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings    */
			if (I2CAccelerometer == null)
			{
				throw new ApplicationException("I2C device not found");
			}

			byte[] WriteBuf_DataFormat = new byte[] {(byte)Register.DataFormat, 0x01 };     // 0x01 sets range to +- 4Gs 
			byte[] WriteBuf_PowerControl = new byte[] {(byte)Register.PowerControl, 0x08 }; // 0x08 puts the accelerometer into measurement mode

			/* Write the register settings */
			I2CAccelerometer.Write(WriteBuf_DataFormat);
			I2CAccelerometer.Write(WriteBuf_PowerControl);
		}

		public Acceleration Read()
		{
			byte[] ReadBuf = new byte[6];  
			byte[] RegAddrBuf = new byte[] { (byte)Register.XAxisData }; 

			I2CAccelerometer.WriteRead(RegAddrBuf, ReadBuf);

			/* In order to get the raw 16-bit data values, we need to concatenate two 8-bit bytes for each axis */
			short AccelerationRawX = BitConverter.ToInt16(ReadBuf, 0);
			short AccelerationRawY = BitConverter.ToInt16(ReadBuf, 2);
			short AccelerationRawZ = BitConverter.ToInt16(ReadBuf, 4);

			/* Convert raw values to G's */
			Acceleration accel = new Acceleration()
			{
				X = (double)AccelerationRawX / UnitsPerG,
				Y = (double)AccelerationRawY / UnitsPerG,
				Z = (double)AccelerationRawZ / UnitsPerG,
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
		};
	}
}


