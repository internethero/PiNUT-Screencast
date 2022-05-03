using System;
using Lib.Core;

namespace Lib.Model
{
    public class StreamStats : ObservableObject
    {
        private string _frame;
        public string Frame
        {
            get { return _frame; }
            private set
            {
                if(_frame != value )
                    _frame = value; OnPropertyChanged();
            }
        }

        private string _fps;
        public string Fps
        {
            get { return _fps; }
            private set
            { 
                if(_fps != value)
                    _fps = value; OnPropertyChanged();
            }
        }

        private string _bitrate;
        public string Bitrate
        {
            get { return _bitrate; }
            private set
            {
                if(_bitrate != value)
                    _bitrate = value; OnPropertyChanged();
            }
        }

        private string _size;
        public string Size
        {
            get { return _size; }
            private set
            { 
                if (_size != value)
                    _size = value; OnPropertyChanged();
            }
        }

        private string _time;
        public string Time
        {
            get { return _time; }
            private set
            { 
                if(_time != value)
                    _time = value; OnPropertyChanged();
            }
        }

        private string _speed;
        public string Speed
        {
            get { return _speed; }
            private set
            { 
                if(_speed != value)
                    _speed = value; OnPropertyChanged();
            }
        }

        public StreamStats(bool placeholder = false)
        {
            if(placeholder)
            {
                Frame = "0";
                Fps = "0";
                Bitrate = "0";
                Size = "0";
                Time = "--:--:--";
                Speed = "0";
            }
        }

        public void Update(string data)
        {
            string[] fields = data.Split(',');

            foreach (string field in fields)
            {
                string[] stats = field.Split('=');

                switch (stats[0])
                {
                    case "frame":
                        Frame = stats[1];
                        break;

                    case "fps":
                        Fps = stats[1];
                        break;

                    case "bitrate":
                        if (stats[1].IndexOf("kbits/s") != -1)
                            Bitrate = stats[1].Substring(0, stats[1].IndexOf("kbits"));
                        break;

                    case "total_size":
                        int n;
                        int.TryParse(stats[1], out n);
                        Size = (n / 1024 / 1024).ToString();
                        break;

                    case "out_time":
                        Time = stats[1].Substring(0, 8);
                        break;

                    case "speed":
                        if (stats[1].IndexOf("x") != -1)
                            Speed = stats[1].Substring(0, stats[1].IndexOf("x"));
                        break;
                }
            }
        }
    }
}
