using Lib.Model;
using System;
using System.Globalization;

namespace Lib.Service
{
    /// <summary>
    /// Translates UI settings to FFmpeg params
    /// </summary>
    public class StreamProcessOptions
    {
        public string Arguments { get; private set; }
        public string Path { get; private set; }

        public StreamProcessOptions(Options options, int offsetX, int offsetY, int with, int height, double aspectRatio)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            string dar = aspectRatio.ToString(nfi);
                        
            Path = options.Path;

            Arguments = $"{Capture(options.Capture, options.Framerate, offsetX, offsetY, options.Width, options.Height)} " +
                $"{Sound(options.Sound)} " +
                $"{Encoder(options.Encoder, options.Codec, options.Profile, options.Framerate, options.PixelFormat, options.Threads, with, height, dar)} -progress - " +
                $"{Container(options.Container)} {Service(options.Service, options.IPPort)}";
        }

        private string Capture(string capture, double framerate, int offsetX, int offsetY, int width, int height)
        {
            switch(capture)
            {
                case "desktop":
                    return $"-v error -f gdigrab -framerate {framerate} -offset_x {offsetX} -offset_y {offsetY} -video_size {width}x{height} -i desktop";
                default:
                    throw new ArgumentOutOfRangeException(nameof(capture));
            }
        }

        private string Encoder(string encoder, string codec, string profile, double framerate, string pixelformat, int threads, int width, int height, string dar)
        {
            switch(encoder)
            {
                case "software":
                    switch(codec)
                    {
                        case "h264":
                            SoftwareH264 softwareH264 = new SoftwareH264(profile, framerate, pixelformat, threads, width, height, dar);
                            return softwareH264.Arguments;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(codec));
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoder));
            }
        }

        private string Sound(bool sound)
        {
            if (sound)
            {
                return "-an";
            }
            else
            {
                return "-an";
            }
        }
        
        private string Container(string container)
        {
            switch (container)
            {
                case "nut":
                    return $"-f nut";
                default:
                    throw new ArgumentOutOfRangeException(nameof(container));
            }
        }

        private string Service(string service, string ipport)
        {
            switch (service)
            {
                case "ptpsink":
                    return $"tcp://{ipport}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(service));
            }
        }

        private class SoftwareH264
        {
            private static string Format = "yuv444p"; // yuv420p
            private static string Dar = "1.78";
            private static string Scale = "1920:1080";
            private static string Tune = "zerolatency";
            private static string Preset = "ultrafast";
            private static string Vsync = "cfr";
            private static double R = 30.00;

            // -x264opts
            private static int Crf = 18;
            private static int Bitrate = 12500;
            private static int Vbv_maxrate = Bitrate;
            //private static int Vbv_bufsize = (int)Math.Ceiling(Vbv_maxrate * (double)0.04);
            private static int Vbv_bufsize { get { return (int)Math.Ceiling(Vbv_maxrate / R); } }
            private static int Intra_refresh = 1;
            private static int Sliced_threads = 1;
            private static int Threads = 2;
            public string Arguments { get; private set; }
            
            public SoftwareH264(string profile, double framerate, string pixelformat, int threads, int width, int height, string dar)
            {
                Scale = $"{width}:{height}";
                Dar = dar;

                // TODO: Tune film and slideshow
                // https://trac.ffmpeg.org/wiki/Encode/H.264
                // http://dev.beandog.org/x264_preset_reference.html
                // https://obsproject.com/sv/blog/streaming-with-x264
                switch (profile)
                {
                    case "screenshare":
                        Tune = "zerolatency";
                        Preset = "ultrafast";
                        break;
                    case "movie":
                        Tune = "film";
                        Sliced_threads = 0;
                        break;
                    case "slideshow":
                        Tune = "stillimage";
                        Sliced_threads = 0;
                        break;
                        throw new ArgumentOutOfRangeException(nameof(profile));
                }

                switch (framerate)
                {
                    case 29.97:
                        R = 29.97;
                        break;
                    case 30:
                        R = 30;
                        break;
                    case 60:
                        R = 60;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(framerate));
                }
                                                
                switch(pixelformat)
                {
                    case "yuv420p":
                        Format = "yuv420p";
                        break;
                    case "yuv444p":
                        Format = "yuv444p";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(pixelformat));
                }

                switch(threads)
                {
                    case 2:
                        Threads = 2;
                        break;
                    case 4:
                        Threads = 4;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(threads));
                }
                
                Arguments = $"-c:v {Param()} -x264opts {Opts()}";
            }

            private string Param()
            {
                return $"libx264 -vf \"scale={Scale},setdar={Dar},format={Format}\" -tune {Tune} -preset {Preset} -vsync {Vsync} -r {R}";
            }

            private string Opts()
            {
                return $"crf={Crf}:bitrate={Bitrate}:vbv-maxrate={Vbv_maxrate}:vbv-bufsize={Vbv_bufsize}" +
                    $":intra-refresh={Intra_refresh}:sliced-threads={Sliced_threads}:threads={Threads}";
            }
        }
    }
}
