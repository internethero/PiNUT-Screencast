using System;
using System.Collections.Generic;
using Lib.Core;
using Zeroconf;

namespace Lib.Model
{
    public class Device : ObservableObject
    {
        public string ServiceName { get; private set; }

        public string Service { get; private set; }

        public string IP { get; private set; }

        public int Port { get; private set; }
        
        public string IPPort { get; private set; }
                
        public DeviceProperties Properties { get; set; }

        private bool _serviceAlive = false;
        public bool ServiceAlive
        {
            get { return _serviceAlive; }
            set { _serviceAlive = value; OnPropertyChanged(); }
        }

        private bool _serviceTested = false;
        public bool ServiceTested
        {
            get { return _serviceTested; }
            set { _serviceTested = value; OnPropertyChanged(); }
        }

        public Device(IZeroconfHost host, KeyValuePair<string, IService> service, DeviceProperties properties)
        {
            IP = host.IPAddress;
            Port = service.Value.Port;
            IPPort = $"{host.IPAddress}:{service.Value.Port.ToString()}";
            Service = service.Value.Name.Substring(1, service.Value.Name.IndexOf("._") - 1);
            ServiceName = service.Value.ServiceName.IndexOf(service.Value.Name) != -1 ? 
                service.Value.ServiceName.Substring(0, service.Value.ServiceName.IndexOf(service.Value.Name) - 1) : "";
            Properties = properties;
        }
    }
}