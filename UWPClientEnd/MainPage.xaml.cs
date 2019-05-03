using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPClientEnd
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		AppServiceConnection connection;

		public MainPage()
		{
			this.InitializeComponent();
		}

		private async void Page_Loaded(object sender, RoutedEventArgs e)
		{
			Refresh.IsEnabled = false;

			connection = new AppServiceConnection();
			connection.AppServiceName = "SensorServerEnd";
			connection.PackageFamilyName = "SensorServerEnd-uwp_nmn3tag1rpsaw";
			AppServiceConnectionStatus status = await connection.OpenAsync();

			if (status == AppServiceConnectionStatus.Success)
			{
				Refresh.IsEnabled = true; 
			}
		}

		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			Measurements.Items.Clear();

			var message = new ValueSet();
			var response = await connection.SendMessageAsync(message);

			if (response.Message.ContainsKey("lastUpdatedAtUTC"))
			{
				foreach (var value in response.Message)
				{
					Measurements.Items.Add($"{value.Key}:{value.Value} ");
				}
			}
		}
	}
}
