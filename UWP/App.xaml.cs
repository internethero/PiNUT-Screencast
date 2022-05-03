using Lib.Model;
using Lib.Service;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using UWP.Services;

namespace UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static event EventHandler AppServiceDisconnected;

        public static event EventHandler<AppServiceTriggerDetails> AppServiceConnected;

        public static event EventHandler<string> StreamStats;

        public static event EventHandler<StreamStateType> StreamState;

        private static BackgroundTaskDeferral ServiceDeferral = null;

        private static AppServiceConnection Connection = null;

        public static Settings Settings = new Settings();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Handles connection requests to the app service
        /// </summary>
        // TODO : Figure out why OnBackgroundActivated() throws an exception:
        // Exception thrown at 0x76AAB922 in UWP.exe: Microsoft C++ exception: EETypeLoadException at memory location 0x0853D0F0.
        // According to https://stackoverflow.com/a/41239245 "In UWP apps, you can ignore this exception especially in Mixed debug mode."
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details)
            {
                // Only accept connections from callers in the same package
                if (details.CallerPackageFamilyName == Package.Current.Id.FamilyName)
                {
                    // Connection established from the fulltrust process
                    // Get a deferral so that the service isn't terminated.
                    ServiceDeferral = args.TaskInstance.GetDeferral();
                    args.TaskInstance.Canceled += OnTaskCanceled;

                    Connection = details.AppServiceConnection;
                    Connection.RequestReceived += OnRequestReceived;

                    AppServiceConnected?.Invoke(this, args.TaskInstance.TriggerDetails as AppServiceTriggerDetails);
                }
            }
        }

        /// <summary>
        /// Handle incomming requests from AppService API
        /// </summary>
        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            foreach (KeyValuePair<string, object> message in args.Request.Message)
            {
                switch (message.Key)
                {
                    case "STREAMSTATS":
                        StreamStats?.Invoke(this, (string)message.Value);
                        break;

                    case "STREAMSTATE":
                        Enum.TryParse(message.Value.ToString(), out StreamStateType state);
                        StreamState?.Invoke(this, state);
                        break;
                }
            }
        }

        /// <summary>
        /// Task canceled here means the app service client is gone
        /// </summary>
        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            ServiceDeferral?.Complete();
            ServiceDeferral = null;
            Connection = null;
            AppServiceDisconnected?.Invoke(this, null);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }

            LaunchFullTrustProcessAsync();
        }

        private async void LaunchFullTrustProcessAsync()
        {
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                try
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                }
                // Most likely we are debugging the uwp app
                catch (Exception) { }
            }
        }

        private void OnAppServiceDisconnectedAsync(object sender, EventArgs e)
        {
            AppServiceDisconnected?.Invoke(this, null);
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// Sends AppService request to Launcher to start stream
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<bool> RequestStartStream(Options options)
        {
            // Json encode options
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Options));
            System.IO.MemoryStream msObj = new System.IO.MemoryStream();
            js.WriteObject(msObj, options);
            msObj.Position = 0;
            System.IO.StreamReader sr = new System.IO.StreamReader(msObj);
            string json = sr.ReadToEnd();
            
            ValueSet request = new ValueSet();
            request.Add("STARTSTREAM", json);
            AppServiceResponse response = await App.Connection.SendMessageAsync(request);
            
            bool result = false;
            bool.TryParse(response.Message["RESULT"].ToString(), out result);
            return result;
        }

        /// <summary>
        /// Sends a AppService request to Launcher to stop stream
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> RequestStopStream()
        {
            ValueSet request = new ValueSet();
            request.Add("STOPSTREAM", true.ToString());
            AppServiceResponse response = await App.Connection.SendMessageAsync(request);

            bool result = false;
            bool.TryParse(response.Message["RESULT"].ToString(), out result);

            return result;
        }
    }
}