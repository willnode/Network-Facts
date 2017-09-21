using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ListNetworkComputers;

namespace Network_Facts
{
    public class NetLocal
    {
        public List<LocalUnit> locals = new List<LocalUnit>();

        public void Arquire ()
        {
            try
            {
                locals.Clear();

                var brs = new NetworkBrowser();
                foreach (string name in brs.getNetworkComputers())
                {
                    var tim = new Stopwatch();
                    tim.Start();
                    var ips = Dns.GetHostAddresses(name);
                    tim.Stop();
                    var t = tim.Elapsed;
                    locals.Add(new LocalUnit() {
                        hostname = name,
                        ipv4 = (from ip in ips where ip.AddressFamily == AddressFamily.InterNetwork select ip).FirstOrDefault(),
                        ipv6 = (from ip in ips where ip.AddressFamily == AddressFamily.InterNetworkV6 select ip).ToArray(),
                        latency = t,
                    });
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occured during arcquiring local computers", e.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
 
        }
    }

    public class LocalUnit
    {
        public string hostname;
        public IPAddress ipv4;
        public IPAddress[] ipv6;
        public TimeSpan latency;

        public string DisplayHostname
        {
            get
            {
                return hostname;
            }
        }

        public string DisplayIPv4
        {
            get
            {
                return ipv4.ToString();
            }
        }

        public string DisplayIPv6
        {
            get
            {
                return string.Join("; ", Array.ConvertAll(ipv6, x => x.ToString()));
            }
        }


        public string DisplayLatency
        {
            get
            {
                return latency.TotalMilliseconds.ToString() + " ms";
            }
        }
    }
}
