using System;
using System.ComponentModel;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ResourceLoader ResourceLoader = ResourceLoader.GetForCurrentView();

        public MainPage()
        {
            this.InitializeComponent();

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            //remove the solid-colored backgrounds behind the caption controls and system back button
            var viewTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            viewTitleBar.ButtonBackgroundColor = Colors.Transparent;
            viewTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            Window.Current.SetTitleBar(AppTitleBar);

            // Make sure Path is configured in settings
            App.Settings.PropertyChanged += OnSettingsChanged;
            NavigationViewConnect.IsEnabled = App.Settings.Path.Length != 0 ? true : false;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Path")
            {
                NavigationViewConnect.IsEnabled = App.Settings.Path.Length != 0 ? true : false;
            }
        }

        /// <summary>
        /// Launch FullTrushProcess when we are loaded
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.AppServiceDisconnected += OnAppServiceDisconnectedAsync;
        }

        /// <summary>
        /// Handles AppServiceDisconnected event
        /// </summary>
        private async void OnAppServiceDisconnectedAsync(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ReconnectDialogAsync();
            });
        }

        /// <summary>
        /// Show a dialog to restart FullTrustProcess or quit application
        /// </summary>
        private async void ReconnectDialogAsync()
        {
            Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(ResourceLoader.GetString("MainPage-DialogAppServiceLost"));

            Windows.UI.Popups.UICommand yesCommand = new Windows.UI.Popups.UICommand(ResourceLoader.GetString("Restart"), async (r) =>
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            });
            dialog.Commands.Add(yesCommand);

            Windows.UI.Popups.UICommand noCommand = new Windows.UI.Popups.UICommand(ResourceLoader.GetString("Quit"), (r) =>
            {
                Windows.ApplicationModel.Core.CoreApplication.Exit();
            });
            dialog.Commands.Add(noCommand);

            await dialog.ShowAsync();
        }

        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            // Load SettingsView if NavigationViewConnect.IsEnabled = false
            if (!NavigationViewConnect.IsEnabled)
            {
                NavigationView.SelectedItem = NavigationView.MenuItems[1];
                ContentFrame.Navigate(typeof(Views.SettingsView));
            }
            else
            {
                NavigationView.SelectedItem = NavigationView.MenuItems[0];
                ContentFrame.Navigate(typeof(Views.ConnectView));
            }
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();

            switch (navItemTag)
            {
                case "connect":
                    if (ContentFrame.SourcePageType != typeof(Views.ConnectView))
                        ContentFrame.Navigate(typeof(Views.ConnectView));
                    break;
                case "settings":
                    if (ContentFrame.SourcePageType != typeof(Views.SettingsView))
                        ContentFrame.Navigate(typeof(Views.SettingsView));
                    break;
                case "about":
                    if (ContentFrame.SourcePageType != typeof(Views.AboutView))
                        ContentFrame.Navigate(typeof(Views.AboutView));
                    break;
            }
        }
    }
}
