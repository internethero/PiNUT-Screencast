using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Zeroconf;
using Lib.Model;

namespace Lib.Service
{
    public static class ZeroconfDevice
    {
        // Supported streaming protocols
        // We only support basic tcp point to point in a NUT container for now
        private static List<string> SupportedServices = new List<string>()
        {
            "_ptpsink._tcp.local.",
        };

        public static async Task<Collection<Device>> GetDevicesAsync(double maxFramerate)
        {
            Collection<Device> devices = new Collection<Device>();
            
            ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync();

            var query = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

            foreach (var host in query)
            {
                if(!string.IsNullOrEmpty(host.IPAddress))
                {
                    foreach (KeyValuePair<string, IService> service in host.Services)
                    {
                        if (SupportedServices.Contains(service.Value.Name))
                            devices.Add(new Device(host, service, new DeviceProperties(service, maxFramerate)));
                    }
                }
            }
            return devices;
        }

        public static bool IsServiceAlive(Device device)
        {
            bool serviceAlive = false;
            var client = new TcpClient();

            try
            {
                if (client.ConnectAsync(device.IP, device.Port).Wait(2000))
                    serviceAlive = true;
            }
            catch (Exception) { }

            client.Close();

            return serviceAlive;
        }
    }
}
