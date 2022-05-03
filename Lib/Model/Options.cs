using System;
using System.Runtime.Serialization;

namespace Lib.Model
{
    public class Options
    {
        // Settings
        [DataMember]
        public string Path;

        [DataMember]
        public string Encoder;

        [DataMember]
        public string Capture;

        [DataMember]
        public string Profile;

        [DataMember]
        public int Threads;

        [DataMember]
        public bool Sound;

        // Device
        [DataMember]
        public string IPPort;

        [DataMember]
        public string Service;

        // DeviceProperties
        [DataMember]
        public double Framerate;

        [DataMember]
        public string PixelFormat;

        [DataMember]
        public int Width;

        [DataMember]
        public int Height;

        [DataMember]
        public string Container;

        [DataMember]
        public string Codec;
    }
}
