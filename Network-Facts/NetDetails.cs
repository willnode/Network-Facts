using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Network_Facts
{
    public class NetDetails
    {
        [NonSerialized]
        NetworkInterface nic;

        public string name;
        public string desc;
        public string id;

        public NetworkInterfaceType type;
        public OperationalStatus stat;
        public IPAddress ipv4;
        public IPAddress[] ipv6;
        public PhysicalAddress mac;
        public long maxSpeed;

        public NetDetails(NetworkInterface nic)
        {
            this.nic = nic;
            Init();
        }

        public void Init()
        {
            name = nic.Name;
            desc = nic.Description;
            id = nic.Id;
            type = nic.NetworkInterfaceType;
            stat = nic.OperationalStatus;
            maxSpeed = nic.Speed;

            mac = nic.GetPhysicalAddress();

            var ips = nic.GetIPProperties().DnsAddresses;

            ipv4 = (from ip in ips where ip.AddressFamily == AddressFamily.InterNetwork select ip).FirstOrDefault();
            ipv6 = (from ip in ips where ip.AddressFamily == AddressFamily.InterNetworkV6 select ip).ToArray();
        }

        public override string ToString()
        {
            return name;
        }
    }
}
