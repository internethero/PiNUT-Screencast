using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Graphics.Display;
using Windows.Foundation;
using System.Reflection;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Lib.Service;
using Lib.Core;
using Lib.Model;

namespace UWP.ViewModels
{
    public enum UiStateType
    {
        Disconnected,
        Connected,
        Connecting,
        Error,
        DiscoverDevices
    }

    public class ConnectViewModel : ObservableObject
    {
        public Collection<Device> Devices = new Collection<Device>();

        public StreamStats StreamStats = new StreamStats();

        private Device _selectedDevice = null;
        public Device SelectedDevice
        {
            get { return _selectedDevice; }
            private set { _selectedDevice = value; OnPropertyChanged(); }
        }

        private UiStateType _uiState = UiStateType.Disconnected;
        public UiStateType UiState
        {
            get { return _uiState; }
            private set { _uiState = value; OnPropertyChanged(); }
        }

        private bool _btnToggleStreamEnabled = false;
        public bool BtnToggleStreamEnabled
        {
            get { return _btnToggleStreamEnabled; }
            private set { _btnToggleStreamEnabled = value; OnPropertyChanged(); }
        }

        public ConnectViewModel()
        {
            App.AppServiceDisconnected += OnAppServiceDisconnected;
            App.StreamState += OnStreamState;
            App.StreamStats += OnStreamStats;
            this.PropertyChanged += OnSelfPropertyChanged;
            
            DiscoverDevicesAsync();
        }

        /// <summary>
        /// Returns current display size, takes screen orientation into account
        /// Taken from https://stackoverflow.com/a/41418762
        /// </summary>
        /// <returns></returns>
        public static Size GetCurrentDisplaySize()
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            TypeInfo t = typeof(DisplayInformation).GetTypeInfo();
            var props = t.DeclaredProperties.Where(x => x.Name.StartsWith("Screen") && x.Name.EndsWith("InRawPixels")).ToArray();
            var w = props.Where(x => x.Name.Contains("Width")).First().GetValue(displayInformation);
            var h = props.Where(x => x.Name.Contains("Height")).First().GetValue(displayInformation);
            var size = new Size(System.Convert.ToDouble(w), System.Convert.ToDouble(h));
            
            switch (displayInformation.CurrentOrientation)
            {
                case DisplayOrientations.Landscape:
                case DisplayOrientations.LandscapeFlipped:
                    size = new Size(Math.Max(size.Width, size.Height), Math.Min(size.Width, size.Height));
                    break;
                case DisplayOrientations.Portrait:
                case DisplayOrientations.PortraitFlipped:
                    size = new Size(Math.Min(size.Width, size.Height), Math.Max(size.Width, size.Height));
                    break;
            }
            return size;
        }

        /// <summary>
        /// Lost connection to AppService, set UiState to disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAppServiceDisconnected(object sender, EventArgs e)
        {
            #pragma warning disable CS4014
            // Because this call is not awaited, execution of the current method continues before the call is completed
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync
                (Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    UiState = UiStateType.Disconnected;
                });
            #pragma warning restore CS4014
        }

        /// <summary>
        /// Listen to our own PropertyChanged events to enable/disable toggle stream button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelfPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "SelectedDevice":
                    if (SelectedDevice != null)
                        BtnToggleStreamEnabled = true;
                    else
                        BtnToggleStreamEnabled = false;
                    break;

                case "UiState":
                    if (SelectedDevice != null && UiState == UiStateType.Disconnected ||
                        UiState == UiStateType.Connected || UiState == UiStateType.Error)
                        BtnToggleStreamEnabled = true;
                    else
                        BtnToggleStreamEnabled = false;
                    break;
            }
        }

        /// <summary>
        /// Handles incomming stream process state from launcher
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state"></param>
        private void OnStreamState(object sender, StreamStateType state)
        {
            #pragma warning disable CS4014
            // Because this call is not awaited, execution of the current method continues before the call is completed
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync
                (Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    switch (state)
                    {
                        case StreamStateType.Connected:
                            UiState = UiStateType.Connected;

                            if (App.Settings.AutoMinimize)
                                MinimizeApplicationAsync();
                            break;

                        case StreamStateType.Error:
                            UiState = UiStateType.Error;
                            break;

                        case StreamStateType.Dissconnected:
                            UiState = UiStateType.Disconnected;
                            break;
                    }
                });
            #pragma warning restore CS4014
        }

        /// <summary>
        /// Handles incomming stream stats from Launcher
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="stats"></param>
        public void OnStreamStats(object sender, string stats)
        {
            if (stats != null)
            {
                #pragma warning disable CS4014
                // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        StreamStats.Update(stats);
                    });
                #pragma warning restore CS4014
            }
        }

        /// <summary>
        /// When selection changes in listview, set SelectedDevice
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnDevicesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Device device in e.AddedItems)
            {
                if (device.ServiceTested && device.ServiceAlive)
                {
                    SelectedDevice = device;
                }
            }
        }

        /// <summary>
        /// Data context changed in the listview
        /// TODO: Disable selection on devices in listview that have serviceAlive = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnListViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {

        }

        /// <summary>
        /// Toggle stream on/off by sending a request to Launcher through AppService
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void OnBtnToggleStream_Click(object sender, RoutedEventArgs e)
        {
            if (UiState == UiStateType.Disconnected || UiState == UiStateType.Error)
            {
                UiState = UiStateType.Connecting;

                var options = new Options()
                {
                    Path = App.Settings.Path,
                    Encoder = App.Settings.Encoder,
                    // Not yet implemented
                    Capture = "desktop",
                    Profile = App.Settings.Profile,
                    Threads = App.Settings.Threads,
                    Sound = App.Settings.Sound,
                    IPPort = SelectedDevice.IPPort,
                    Service = SelectedDevice.Service,
                    Framerate = SelectedDevice.Properties.Framerate,
                    PixelFormat = SelectedDevice.Properties.PixelFormat,
                    Width = SelectedDevice.Properties.Resolution.Width,
                    Height = SelectedDevice.Properties.Resolution.Height,
                    Container = SelectedDevice.Properties.Container,
                    Codec = SelectedDevice.Properties.Codec,
                };
                
                var result = await App.RequestStartStream(options);

                if (!result)
                    UiState = UiStateType.Error;
            }

            else if (UiState == UiStateType.Connected)
            {
                var result = await App.RequestStopStream();
            }
        }

        public void OnBtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            DiscoverDevicesAsync();
        }

        private async void DiscoverDevicesAsync()
        {
            var prevUiState = UiState;
            UiState = UiStateType.DiscoverDevices;

            Devices = await ZeroconfDevice.GetDevicesAsync(App.Settings.Framerate);
            OnPropertyChanged("Devices");

            UiState = prevUiState;
            IsServicesAlive();
        }

        /// <summary>
        /// Verifies if advertised services responds and update devices accordingly
        /// </summary>
        private void IsServicesAlive()
        {
            for (var n = 0; n < Devices.Count; n++)
            {
                bool serviceAlive = false;
                int device = n;
                Devices[device].ServiceTested = false;
                Devices[device].ServiceAlive = false;

                #pragma warning disable CS4014
                Task.Run(() =>
                {
                    serviceAlive = ZeroconfDevice.IsServiceAlive(Devices[device]);

                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            Devices[device].ServiceTested = true;
                            Devices[device].ServiceAlive = serviceAlive;
                        });
                });
                #pragma warning restore CS4014
            }
        }

        private async void MinimizeApplicationAsync()
        {
            IList<AppDiagnosticInfo> infos = await AppDiagnosticInfo.RequestInfoForAppAsync();
            IList<AppResourceGroupInfo> resourceInfos = infos[0].GetResourceGroups();
            await resourceInfos[0].StartSuspendAsync();
        }
    }
}
