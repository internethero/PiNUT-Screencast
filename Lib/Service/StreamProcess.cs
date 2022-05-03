using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace Lib.Service
{
    public enum StreamStateType
    {
        Dissconnected = 0,
        Connected = 5,
        Error = 10,
    }
    
    public class StreamProcess
    {
        public event EventHandler<string> StreamStats;

        public event EventHandler<string> StreamError;

        public event EventHandler<StreamStateType> StreamState;

        public event EventHandler ProcessExited;

        public Process Process = null;

        private readonly string Path;

        private readonly string Arguments;

        private readonly string PidFile;

        readonly string[] Buffer = new string[MaxLines];

        const int MaxLines = 12;

        int BufferWrite = 0;

        private StreamStateType State = StreamStateType.Dissconnected;
        
        public StreamProcess(StreamProcessOptions options, string pidFile = null)
        {
            Path = options.Path;
            Arguments = options.Arguments;
            PidFile = pidFile;
        }

        public void Start()
        {
            if(Process == null)
            {
                // Create Process
                Process = new Process();
                Process.StartInfo.FileName = Path + @"\ffmpeg.exe";
                Process.StartInfo.WorkingDirectory = Path;
                Process.StartInfo.Arguments = Arguments;

                Process.StartInfo.UseShellExecute = false;
                
                // Used to pipe in audio
                Process.StartInfo.RedirectStandardInput = false; //true
                // FFmpeg -stats output on StandardError rather than StandardOutput
                Process.StartInfo.RedirectStandardError = false; //false
                //FFmpeg -progress - outputs on StandardOut and errors on StandardError
                Process.StartInfo.RedirectStandardOutput = true; //true

                // Don't create a window for process
                Process.StartInfo.CreateNoWindow = true; //true

                // Have to be enabled for Exited event
                Process.EnableRaisingEvents = true; //true

                Process.ErrorDataReceived += OnErrorDataReceived;
                Process.OutputDataReceived += OnOutputDataReceived;

                //Process Exited event
                Process.Exited += OnProcessExited;

                Task.Run(() =>
                {
                    Process.Start();
                    
                    // Have to be set after process is started
                    Process.PriorityClass = ProcessPriorityClass.AboveNormal;

                    if(Process.StartInfo.RedirectStandardOutput)
                        Process.BeginOutputReadLine();
                    
                    if(Process.StartInfo.RedirectStandardError)
                        Process.BeginErrorReadLine();

                    if (PidFile != null)
                        WritePidToFile(Process.Id);

                    Process.WaitForExit();
                });
            }
        }

        private void WritePidToFile(int id)
        {
            FileInfo file = new FileInfo(PidFile);
            File.WriteAllText(file.FullName, id.ToString());
        }

        private void SetState(StreamStateType newstate)
        {
            if(State != newstate)
                StreamState?.Invoke(this, newstate);

            State = newstate;
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                SetState(StreamStateType.Connected);

                Buffer[BufferWrite++] = e.Data.Trim();
                BufferWrite %= MaxLines;
                
                if(BufferWrite == MaxLines-1)
                    StreamStats?.Invoke(this, string.Join(",", Buffer).ToString());
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                SetState(StreamStateType.Error);
                StreamError?.Invoke(this, e.Data);
            }
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            // Dont overwrite error state
            if (State != StreamStateType.Error)
                SetState(StreamStateType.Dissconnected);

            ProcessExited?.Invoke(this, e);
        }

        public void Stop()
        {
            if (Process != null || !Process.HasExited)
            {
                SetState(StreamStateType.Dissconnected);

                Process.Kill();
                Process.Dispose();
            }
        }
    }
}