using System;
using System.Collections.Generic;
using System.Text;

namespace Lib.Model
{
    public class DeviceResolution
    {
        public int Width { get; set; }
        public int Height { get; set; }
        
        public DeviceResolution(int width, int height)
        {
            Width = width;
            Height = height; 
        }
    }
}
