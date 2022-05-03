using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Zeroconf;

namespace Lib.Model
{
    public class DeviceProperties
    {
        // Lowest value in settings
        private double _framerate = 30.00;
        public double Framerate
        { 
            get { return _framerate; } 
            private set { _framerate = value; }
        }

        private string _pixelFormat = "yuv420p";
        public string PixelFormat
        {
            get { return _pixelFormat; }
            private set { _pixelFormat = value; }
        }

        // default 1920X1080
        private DeviceResolution _resolution;
        public DeviceResolution Resolution
        {
            get { return _resolution; }
            private set { _resolution = value; }
        }

        private string _container = "nut";
        public string Container
        {
            get { return _container; }
            private set { _container = value; }
        }

        private string _codec = "h264";
        public string Codec
        {
            get { return _codec; }
            private set { _codec = value; }
        }

        public DeviceProperties(KeyValuePair<string, IService> service, double maxFramerate)
        {
            var props = service.Value.Properties;

            SetFrameRate(props.Select(k => k.ContainsKey("framerate")).FirstOrDefault() ?
                props.Select(v => v["framerate"]).FirstOrDefault() : null, maxFramerate);

            SetResolution(props.Select(k => k.ContainsKey("resolution")).FirstOrDefault() ?
                props.Select(v => v["resolution"]).FirstOrDefault() : null);

            SetPixelFormat(props.Select(k => k.ContainsKey("pixel_format")).FirstOrDefault() ?
                props.Select(v => v["pixel_format"]).FirstOrDefault() : null);

            SetContainer(props.Select(k => k.ContainsKey("container")).FirstOrDefault() ?
                props.Select(v => v["container"]).FirstOrDefault() : null);

            SetCodec(props.Select(k => k.ContainsKey("codec")).FirstOrDefault() ?
                props.Select(v => v["codec"]).FirstOrDefault() : null);
        }

        private void SetFrameRate(string value, double maxFramerate)
        {
            if(Double.TryParse(value, out double dFramerate))
            {
                if(dFramerate <= maxFramerate)
                {
                    switch (value)
                    {
                        case "30":
                            Framerate = 30;
                            break;
                        case "60":
                            Framerate = 60;
                            break;
                    }
                }
                else
                {
                    Framerate = maxFramerate;
                }
            }
        }

        private void SetPixelFormat(string value)
        {
            if (value != null)
            {
                switch (value.ToLower())
                {
                    case "yuv420p":
                        PixelFormat = "yuv420p";
                        break;
                    case "yuv444p":
                        PixelFormat = "yuv444p";
                        break;
                }
            }
        }

        private void SetResolution(string value)
        {
            if(value != null)
            {
                // Match delimter case-insensitive
                if (Regex.IsMatch(value, @"^[0-9]{3,4}(?i)x[0-9]{3,4}$"))
                {
                    var devRes = value.ToUpper();
                    int[] whArr = devRes.Split('X').Select(Int32.Parse).ToArray();

                    Resolution = new DeviceResolution(whArr[0], whArr[1]);
                }
            }
            else
            {
                Resolution = new DeviceResolution(1920, 1080);
            }
        }

        private void SetContainer(string value)
        {
            if (value != null)
            {
                switch (value.ToLower())
                {
                    case "nut":
                        Container = "nut";
                        break;
                }
            }
        }

        private void SetCodec(string value)
        {
            if (value != null)
            {
                switch (value.ToLower())
                {
                    case "h264":
                        Codec = "h264";
                        break;
                }
            }
        }
    }
}
