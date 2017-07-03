using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using WpfAnimatedGif;

using TapTrack.Ndef;
using TapTrack.Tcmp;
using TapTrack.Tcmp.Communication;
using TapTrack.Tcmp.Communication.Exceptions;
using TapTrack.Tcmp.CommandFamilies.BasicNfc;
using TapTrack.Tcmp.CommandFamilies.System;
using System.Threading;
using System.Windows.Controls.Primitives;

using Bluegiga.BLE.Events.GAP;
using Bluegiga.BLE.Events.ATTClient;
using Bluegiga.BLE.Responses.GAP;
using Bluegiga.BLE.Events.Connection;
using Bluegiga.BLE.Responses.ATTClient;

namespace TappyKeyboardWedge
{

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		List<TappyReader> activeTappies = new List<TappyReader>();
		TappyReader selectedTappy;
		TappyReader searchingForTappyies;
		GridLength zeroHeight = new GridLength(0);
		TapTrack.Tcmp.CommandFamilies.Command heartbeat;
		int heartbeatTransmitPeriod = 5000;
		System.Timers.Timer heartbeatAlarm;
		System.Timers.Timer heartbeatCheck;
		DateTime lastHearbeatTime;
		int requiredHeartbeatPeriodFromReader = 10;
		bool heartbeatOk = false;
		string[] tappiesFound;
		bool KeyboardModeLineBreak = false;

		public MainWindow()
		{

			Bluegiga.BLE.Events.Connection.DisconnectedEventHandler TappyDisconnectHandler = delegate (object sender, Bluegiga.BLE.Events.Connection.DisconnectedEventArgs e)
			{
				Console.WriteLine("***BLE Disconnected***");
				heartbeatOk = false;
				heartbeatCheck.Enabled = false;

				activeTappies.Remove(selectedTappy);
	            Dispatcher.Invoke(new Action(() => { lvActiveTappies.ItemsSource = null; lvActiveTappies.ItemsSource = activeTappies; }));

				ShowFailStatus("Tappy was disconnected");
			

			};

			InitializeComponent();
			heartbeat = new Ping();
			heartbeatAlarm = new System.Timers.Timer(heartbeatTransmitPeriod);
			heartbeatAlarm.Elapsed += sendHeartbeat;
			heartbeatAlarm.AutoReset = true;
			heartbeatAlarm.Enabled = true;
			heartbeatCheck = new System.Timers.Timer(requiredHeartbeatPeriodFromReader);
			heartbeatCheck.Elapsed += checkHeartbeat;
			heartbeatCheck.AutoReset = true;
			heartbeatCheck.Enabled = false;
			selectedTappy = new TappyReader(CommunicationProtocol.Bluetooth, TappyDisconnectHandler);
			searchingForTappyies = new TappyReader(CommunicationProtocol.Bluetooth, TappyDisconnectHandler);
			ScanForTappies(1500);
		}

		private void sendHeartbeat(Object source, ElapsedEventArgs e)
		{
			if (selectedTappy.isConnected())
			{
				selectedTappy.SendCommand(heartbeat, InvokeKeyboardFeature);
				Console.WriteLine("-----Heartbeat Sent-----");
			}
				
		}

		private async void checkHeartbeat(Object source, ElapsedEventArgs e)
		{
			if ((DateTime.Now - lastHearbeatTime).TotalSeconds > requiredHeartbeatPeriodFromReader)				
			{
				Console.WriteLine("***Hearbeat Fail***");
				heartbeatCheck.Enabled = false;
				heartbeatOk = false;				
				activeTappies.Remove(selectedTappy);
	            ShowFailStatus("Heartbeat lost with TappyBLE");
				 Action  updateUI = () =>
				{
					lvActiveTappies.ItemsSource = null;
					lvActiveTappies.ItemsSource = activeTappies;
				};
				await Dispatcher.BeginInvoke(updateUI);
				selectedTappy.Disconnect();

			}

		}

		private void ScanForTappies(int scanTime)
		{
			List<string> foundTappies = new List<string>();			

			 tappiesFound = searchingForTappyies.FindNearbyTappyBLEs(scanTime);
			
			foreach (string tappyName in tappiesFound)
			{
				foundTappies.Add(tappyName);
			}
			lvNearbyTappies.ItemsSource = null;
			lvNearbyTappies.ItemsSource = foundTappies;
		}

		private void btnConnect_Click(object sender, RoutedEventArgs e)
		{
			//ToggleButton button = sender as ToggleButton;
			Button button = sender as Button;
			string tappyName = button?.Tag.ToString();
			if (!String.IsNullOrEmpty(tappyName))
			{
				ShowPendingStatus($"Connecting to {tappyName}");

				Task.Run(() =>
				{
					if (searchingForTappyies.ConnectKioskKeyboardWedgeByName(tappyName))

					{
						List<string> nearbyTappies = lvNearbyTappies.ItemsSource as List<string>;
						selectedTappy = searchingForTappyies;
						activeTappies.Add(selectedTappy);
						nearbyTappies.Remove(selectedTappy.DeviceName);
						heartbeatCheck.Enabled = true;
						lastHearbeatTime = DateTime.Now;
						heartbeatOk = true;
						ShowSuccessStatus($"Connected to {tappyName}");
						Dispatcher.Invoke(new Action(() =>
						{
							lvActiveTappies.ItemsSource = null;
							lvActiveTappies.ItemsSource = activeTappies;
							lvNearbyTappies.ItemsSource = null;
							lvNearbyTappies.ItemsSource = nearbyTappies;

						}));


					}
					else
					{
						ShowFailStatus($"Problem connecting to {tappyName}");
						return;
					}
				});
			}
			else
			{
				MessageBox.Show("Problem with Tappy Device Name");
				return;
			}
		}

		private async void btnDisconnect_Click(object sender, RoutedEventArgs e)
		{
			string activetappyname = selectedTappy.DeviceName;
			selectedTappy.Disconnect();
			activeTappies.Remove(selectedTappy);
			heartbeatCheck.Enabled = false;
			heartbeatOk = false;
			Action updateUI = () =>
			{
				lvActiveTappies.ItemsSource = null;
				lvActiveTappies.ItemsSource = activeTappies;
				List<string> nearbyTappies = lvNearbyTappies.ItemsSource as List<string>;
				nearbyTappies.Add(activetappyname);
				lvNearbyTappies.ItemsSource = null;
				lvNearbyTappies.ItemsSource = nearbyTappies;
			};
			await Dispatcher.BeginInvoke(updateUI);
			ShowSuccessStatus("Disconnected from TappyBLE");
	}


		private void ShowSuccessStatus(string message = "")
		{
			Action show = () =>
			{
				statusPopup.IsOpen = true;
				statusText.Content = "Success";
				statusMessage.Content = message;
				ImageBehavior.SetAnimatedSource(statusImage, (BitmapImage)FindResource("Success"));

				Task.Run(() =>
				{
					Thread.Sleep(1000);
					HideStatus();
				});
			};

			Dispatcher.BeginInvoke(show);
		}

		private void ShowPendingStatus(string message)
		{
			Action show = () =>
			{
				statusPopup.IsOpen = true;
				statusText.Content = "Pending";
				statusMessage.Content = message;
				ImageBehavior.SetAnimatedSource(statusImage, (BitmapImage)FindResource("Pending"));
			};

			Dispatcher.BeginInvoke(show);
		}

		private void ShowFailStatus(string message)
		{
			Action show = () =>
			{
				dismissButtonContainer.Height = new GridLength(50);
				dismissButton.Visibility = Visibility.Visible;
				statusPopup.IsOpen = true;
				statusText.Content = "Fail";
				statusMessage.Content = message;
			};

			Dispatcher.BeginInvoke(show);
		}

		private void HideStatus()
		{
			Action hide = () =>
			{
				statusPopup.IsOpen = false;
			};

			Dispatcher.Invoke(hide);
		}

		private void DismissButton_Click(object sender, RoutedEventArgs e)
		{
			HideStatus();
			dismissButton.Visibility = Visibility.Hidden;
			dismissButtonContainer.Height = zeroHeight;
		
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e)
		{
			Cursor = Cursors.Wait;
			ScanForTappies(2000);
			Cursor = Cursors.Arrow;
		}

		private void tgbtnLaunchKeyboardFeature_Checked(object sender, RoutedEventArgs e)
		{
			
		}

		private void InvokeKeyboardFeature(ResponseFrame frame, Exception e)
		{
			if (!selectedTappy.isConnected() || CheckForErrorsOrTimeout(frame,e))
			{
				return;
			}
			if (frame.CommandFamily0 == 0x00 && frame.CommandFamily1 == 0x00 && frame.ResponseCode == TapTrack.Tcmp.CommandFamilies.System.Ping.commandCode)
			{
				Console.WriteLine("-------Hearbeat received-------");
				lastHearbeatTime = DateTime.Now;
				heartbeatOk = true;

			}
			else if (frame?.CommandFamily0 == 0x01 && frame?.CommandFamily1 == 0x00 && frame?.ResponseCode == 0x01) //found UID only
			{
				Console.WriteLine("----------Tag UID found----------");
				byte[] data = frame?.Data;

				if (data == null)
				{
					return;
				}
				else
				{
					byte[] uid = new byte[data.Length - 1];
					if (uid.Length > 0)
					{
						Array.Copy(data, 1, uid, 0, data.Length - 1);
						System.Windows.Forms.SendKeys.SendWait(BitConverter.ToString(uid));
						if (KeyboardModeLineBreak)
							System.Windows.Forms.SendKeys.SendWait("{ENTER}");
					}
				}

			}
			else if (frame?.CommandFamily0 == 0x01 && frame?.CommandFamily1 == 0x00 && frame?.ResponseCode == 0x02) //found NDEF

			{
				Console.WriteLine("----------Tag NDEF found----------");

				byte[] data = frame?.Data;

				if (data == null)
				{
					return;
				}
				else
				{
					byte[] temp = new byte[data.Length - data[1] - 2];

					if (temp.Length > 0)
					{
						Array.Copy(data, 2 + data[1], temp, 0, temp.Length);

						NdefLibrary.Ndef.NdefMessage message = NdefLibrary.Ndef.NdefMessage.FromByteArray(temp);

						Action EnterKeystrokes = () =>
						{
							foreach (NdefLibrary.Ndef.NdefRecord record in message)
							{
								if (record.Type == null)
									return;
								string type = Encoding.UTF8.GetString(record.Type);
								if (type.Equals("T"))
								{
									NdefLibrary.Ndef.NdefTextRecord textRecord = new NdefLibrary.Ndef.NdefTextRecord(record);
									System.Windows.Forms.SendKeys.SendWait(textRecord.Text);
									if (KeyboardModeLineBreak)
										System.Windows.Forms.SendKeys.SendWait("{ENTER}");
								}

							}
						};

						Dispatcher.BeginInvoke(EnterKeystrokes);
					}
				}
			} else if (frame.CommandFamily0 == 0x00 && frame.CommandFamily1 == 0x00 && frame.ResponseCode == 0x0C)
			{
				Console.WriteLine("----------Tappy Keyboard Wedge Mode Settings confimred----------");
				if (frame.Data.Length > 3)
				{
					if (frame.Data[0] == 0x00)
						Console.WriteLine(">> Dual tag detection disabled <<");
					else
						Console.WriteLine(">> Dual tag detection enabled <<");
					if (frame.Data[1] == 0x00)
						Console.WriteLine(">> NDEF detection disabled (reading UIDs instead) <<");
					else
						Console.WriteLine(">> NDEF detection enabled <<");

					int heartbeatPeriod = (int)frame.Data[2];
					Console.WriteLine($">> Heartbeat period set to {heartbeatPeriod} <<");
					if (frame.Data[3] == 0x00)
						Console.WriteLine(">> Scan error messages disabled <<");
					else
						Console.WriteLine(">> Scan error messages enabled <<");

					ShowSuccessStatus("Keyboard wedge configuration set");

				}
				else
				{
					Console.WriteLine("*****Error: Tappy Keyboard Wedge Mode Settings truncated resonse*****");
					
				}


			}
		}

		private bool CheckForErrorsOrTimeout(ResponseFrame frame, Exception e)
		{
			if (e != null)
			{
				if (e.GetType() == typeof(HardwareException))
					ShowFailStatus("Tappy is not connected");
				else
					ShowFailStatus("An error occured");

				return true;
			}
			else if (!TcmpFrame.IsValidFrame(frame))
			{
				ShowFailStatus("An error occured");

				return true;
			}
			else if (frame.IsApplicationErrorFrame())
			{
				ApplicationErrorFrame errorFrame = (ApplicationErrorFrame)frame;
				ShowFailStatus(errorFrame.ErrorString);
				return true;
			}
			else if (frame.CommandFamily0 == 0 && frame.CommandFamily1 == 0 && frame.ResponseCode < 0x05)
			{
				ShowFailStatus(TappyError.LookUp(frame.CommandFamily, frame.ResponseCode));
				return true;
			}
			else if (frame.ResponseCode == 0x03)
			{
				ShowFailStatus("No tag detected");
				return true;
			}
			else
			{
				return false;
			}
		}



		#region BLE Scan Refresh
		private async void btnRefresh_async_Click(object sender, RoutedEventArgs e)
		{
			btnRefresh.IsEnabled = false;
			ImageBehavior.SetAnimatedSource(imgRefresh, (BitmapImage)FindResource("Pending"));

		    await GetNearbyTappies_async();

			ImageBehavior.SetAnimatedSource(imgRefresh, (BitmapImage)FindResource("BluetoothSearching"));
			btnRefresh.IsEnabled = true;
		}

		private async Task<bool> GetNearbyTappies_async()
		{
			bool result = false;
			List<string> nearbyTappies = new List<string>();	
			string[] tappiesFound  = await Task.Run(() => searchingForTappyies.FindNearbyTappyBLEs(1500));
			foreach (string tappyName in tappiesFound)
			{
				nearbyTappies.Add(tappyName);
				result = true;
			}

			Action updateUI = () =>
			{
				lvNearbyTappies.ItemsSource = null;
				lvNearbyTappies.ItemsSource = nearbyTappies;
			};
			await Dispatcher.BeginInvoke(updateUI);

			return result;
		}
		#endregion

		#region CRLFToggle
		private void chbxAddlineBreak_Checked(object sender, RoutedEventArgs e)
		{
			KeyboardModeLineBreak = true;
		}

		private void chbxAddlineBreak_Unchecked(object sender, RoutedEventArgs e)
		{
			KeyboardModeLineBreak = false;
		}
		#endregion

		#region UID_NDEFToggle
		private void chbxReadUIDOnly_Checked(object sender, RoutedEventArgs e)
		{
			//placeholder, no reason to do anything 
		}

		private void chbxReadUIDOnly_Unchecked(object sender, RoutedEventArgs e)
		{
			//placeholder, no reason to do anything 
		}
		#endregion chbxScanForType1

		#region Type1_MifareToggle
		private void chbxScanForType1_Checked(object sender, RoutedEventArgs e)
		{
			//placeholder, no reason to do anything 
		}

		private void chbxScanForType1_Unchecked(object sender, RoutedEventArgs e)
		{
			//placeholder, no reason to do anything 
		}
		#endregion

		#region TappyConfigSettings
		private void setConfigurationButton_Click(object sender, RoutedEventArgs e)
		{
			if (selectedTappy.isConnected() == false)
			{
				ShowFailStatus("Tappy not connected");
				return;
			}

			byte uidOrNdefSetting, type1Setting, scanErrMsgSetting;
			if (chbxReadUIDOnly.IsChecked == true)
			{
				uidOrNdefSetting = 0x02; //disable NDEF detection
			}
			else
			{
				uidOrNdefSetting = 0x01; //ensable NDEF detection
			}
			if (chbxScanForType1.IsChecked == true)
			{
				type1Setting = 0x01; //enable dual tag type detection
			}
			else
			{
				type1Setting = 0x02; //disable dual tag type detection
			}
			if (chbxSendScanErrMsgs.IsChecked == true)
			{
				scanErrMsgSetting = 0x01; //enable scan error messages
			}
			else
			{
				scanErrMsgSetting = 0x02; //disable scan error messages
			}
			ConfigureKioskKeyboardWedgeMode configTappy = new ConfigureKioskKeyboardWedgeMode(type1Setting, uidOrNdefSetting, 0x00, scanErrMsgSetting);
			selectedTappy?.SendCommand(configTappy, InvokeKeyboardFeature);


		}



		#endregion


	}
}


