using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Lib.Service;
using Lib.Model;
using Forms = System.Windows.Forms;
using System.Drawing;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppServiceConnection Connection = null;

        private StreamProcess StreamProcess = null;

        private bool ProcessExited = true;

        long LastStreamStatsUpdate;

        const int UpdateInterval = 1;

        // %appdata%\..\Local\Packages\bbb96199-2df6-427b-814c-0b5584f6f6ad_08werhym9yqe6\LocalCache\Local
        private readonly string PidFile;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAppService();

            try
            {
                PidFile = $"{Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path}\\Local\\StreamProcess.pid";
            }
            // We are debugging just the launcher
            catch { }

            // If this process is killed somehow with a StreamProcess running,
            // ffmpeg turns into a zombie that needs to be killed.
            KillZombieProcess();

            LastStreamStatsUpdate = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        private async void InitializeAppService()
        {
            try
            {
                Connection = new AppServiceConnection();
                Connection.AppServiceName = "appservice.pinutscreencast";
                Connection.PackageFamilyName = Package.Current.Id.FamilyName;
                Connection.RequestReceived += OnRequestReceived;
                Connection.ServiceClosed += OnAppServiceClosed;

                AppServiceConnectionStatus status = await Connection.OpenAsync();
                if (status != AppServiceConnectionStatus.Success)
                {
                    // Something unforeseen happened, shut down
                    ShutdownApplication();
                }
            }
            // We are probably debugging just the Launcher
            catch (Exception) { }
        }

        private bool StartStream(Options options, bool hookEvents = true)
        {
            if (ProcessExited)
            {
                ProcessExited = false;

                int width, height;
                Rectangle screen = PrimaryScreen();

                if (screen.Width * screen.Height >= options.Width * options.Height)
                {
                    width = options.Width;
                    height = options.Height;
                }
                else
                {
                    width = screen.Width;
                    height = screen.Height;
                }

                double aspectRatio = ((double)options.Width / options.Height);

                StreamProcess = new StreamProcess(new StreamProcessOptions(options, screen.X, screen.Y, width, height, aspectRatio), PidFile);               
                StreamProcess.Start();

                if (hookEvents)
                {
                    StreamProcess.StreamState += OnStreamState;
                    StreamProcess.StreamStats += OnStreamStats;
                }

                StreamProcess.ProcessExited += OnProcessExited;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to kill an zombie process with Pid from PidFile 
        /// </summary>
        private void KillZombieProcess()
        {
            if (File.Exists(PidFile))
            {
                var content = File.ReadAllText(PidFile);
                File.Delete(PidFile);

                if (int.TryParse(content, out int pid))
                {
                    Process[] process = Process.GetProcesses();

                    foreach (Process p in process)
                    {
                        if (p.Id == pid)
                        {
                            p.Kill();
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stops a running StreamProcess
        /// </summary>
        /// <returns></returns>
        private bool StopStream()
        {
            if (!ProcessExited)
            {
                StreamProcess.Stop();
                StreamProcess = null;

                if (File.Exists(PidFile))
                    File.Delete(PidFile);

                return true;
            }
            else
            {
                return false;
            }
        }

        private Rectangle PrimaryScreen()
        {
            Rectangle bounds = new Rectangle();

            foreach (Forms.Screen screen in Forms.Screen.AllScreens)
            {
                if (screen.Primary)
                {
                    bounds = screen.Bounds;
                    break;
                }
            }
            return bounds;
        }

        /// <summary>
        /// Cleanup and shutdown application
        /// </summary>
        private void ShutdownApplication()
        {
            if (StreamProcess != null)
                StreamProcess.Stop();

            Connection.Dispose();

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                Application.Current.Shutdown();
            }));
        }

        /// <summary>
        ///  Connection to UWP application lost so we shut down
        /// </summary>
        private void OnAppServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            ShutdownApplication();
        }

        /// <summary>
        /// Handle requests from AppService API
        /// </summary>
        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            foreach (KeyValuePair<string, object> message in args.Request.Message)
            {
                switch (message.Key)
                {
                    case "STARTSTREAM":
                        // Deserialization from JSON
                        var ms = new System.IO.MemoryStream(System.Text.Encoding.Unicode.GetBytes(message.Value.ToString()));
                        DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(Options));
                        Options options = (Options)deserializer.ReadObject(ms);
                        
                        var StartStreamResult = StartStream(options);

                        ValueSet StartStreamresponse = new ValueSet();
                        StartStreamresponse.Add("RESULT", StartStreamResult.ToString());
                        await args.Request.SendResponseAsync(StartStreamresponse);
                        break;

                    case "STOPSTREAM":
                        var StopStreamResult = StopStream();
                        
                        ValueSet StopStreamResponse = new ValueSet();
                        StopStreamResponse.Add("RESULT", StopStreamResult.ToString());
                        await args.Request.SendResponseAsync(StopStreamResponse);
                        break;
                    case "":
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(message));
                }
            }
        }
        
        private void OnProcessExited(object sender, EventArgs e)
        {
            ProcessExited = true;
        }

        /// <summary>
        /// Event handler for StreamProcess StreamStats
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="stats"></param>
        private void OnStreamStats(object sender, string stats)
        {
            long elapsed = (DateTimeOffset.Now.ToUnixTimeSeconds()) - LastStreamStatsUpdate;

            if (elapsed >= UpdateInterval)
            {
                ValueSet request = new ValueSet();
                request.Add("STREAMSTATS", stats);
                #pragma warning disable CS4014
                // Because this call is not awaited, execution of the current method continues before the call is completed
                Connection.SendMessageAsync(request);
                #pragma warning restore CS4014
                
                LastStreamStatsUpdate = DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }

        /// <summary>
        /// Event handler for StreamProcess StreamState
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state"></param>
        private void OnStreamState(object sender, StreamStateType state)
        {
            ValueSet request = new ValueSet();
            request.Add("STREAMSTATE", state.ToString());
            #pragma warning disable CS4014
            // Because this call is not awaited, execution of the current method continues before the call is completed
            Connection.SendMessageAsync(request);
            #pragma warning restore CS4014
        }
    }
}
