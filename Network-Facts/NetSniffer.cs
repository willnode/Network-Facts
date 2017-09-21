using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MJsniffer;

namespace Network_Facts
{
    public class NetSniffer
    {
        public Socket socket;
        public IPAddress IP;
        // IP in 32-bits
        public uint IP32;

        public List<SniffUnit> sniffed = new List<SniffUnit>();

        // IP, Index in the list
        private Dictionary<uint, int> sniffIdxs = new Dictionary<uint, int>();

        public byte[] buffer = new byte[4096];
        public int bufferC = 0;

        public bool sniffing = false;

        public bool Start()
        {

            if (!Utility.HaveAdminAccess)
            {
                if (MessageBox.Show("Network sniffing requires an elevated access. Continue?", "Elevated access for Raw socket", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    Utility.HaveAdminAccess = true;
                return false;
            }

            IP = (from i in Dns.GetHostEntry(Dns.GetHostName()).AddressList where i.AddressFamily == AddressFamily.InterNetwork select i).FirstOrDefault();

            if (IP == null)
            {

                MessageBox.Show("No Accessible IP Address found!");
                return false;
            }

            IP32 = BitConverter.ToUInt32(IP.GetAddressBytes(), 0);

            socket = new Socket(AddressFamily.InterNetwork,
                       SocketType.Raw, ProtocolType.IP);

            //Bind the socket to the selected IP address
            socket.Bind(new IPEndPoint(IP, 0));

            //Set the socket  options
            socket.SetSocketOption(SocketOptionLevel.IP,            //Applies only to IP packets
                                       SocketOptionName.HeaderIncluded, //Set the include the header
                                       true);                           //option to true

            byte[] byTrue = new byte[4] { 1, 0, 0, 0 };
            byte[] byOut = new byte[4] { 1, 0, 0, 0 }; //Capture outgoing packets

            //Socket.IOControl is analogous to the WSAIoctl method of Winsock 2
            socket.IOControl(IOControlCode.ReceiveAll,              //Equivalent to SIO_RCVALL constant
                                                                    //of Winsock 2
                                 byTrue,
                                 byOut);

            // Socket ready
            sniffing = true;

            new Thread(new ThreadStart(Arquire)).Start();

            return true;
        }

        public void Stop()
        {
            try
            {
                sniffing = false;
                if (socket != null)
                {
                    socket.Close();
                    socket = null;
                }

            }
            catch (Exception)
            {
            }
        }

        public void Arquire()
        {

            while (sniffing)
            {
                Array.Clear(buffer, 0, bufferC);
                bufferC = socket.Receive(buffer, SocketFlags.None);
                ParseData(buffer, bufferC);

                Thread.Sleep(5);
            }


        }

        private void ParseData(byte[] byteData, int nReceived)
        {

            //Since all protocol packets are encapsulated in the IP datagram
            //so we start by parsing the IP header and see what protocol data
            //is being carried by it
            IPHeader ipHeader = new IPHeader(byteData, nReceived);

            if (ipHeader.uiSourceIPAddress == ipHeader.uiDestinationIPAddress)
                MessageBox.Show("Supicious?");

            bool outgoing;
            var ip = (outgoing = ipHeader.uiSourceIPAddress == IP32) ? ipHeader.uiDestinationIPAddress : ipHeader.uiSourceIPAddress;

            SniffUnit unit;
            int unitIdx;
            if (!sniffIdxs.TryGetValue(ip, out unitIdx))
            {
                unit = new SniffUnit() { target = new IPAddress(ip) };
                sniffIdxs[ip] = sniffed.Count;
                sniffed.Add(unit);
                unit.LookupServer();
            }
            else
                unit = sniffed[unitIdx];

            if (outgoing)
                unit.outgoing++;
            else
                unit.ingoing++;

            unit.sizedata += ipHeader.usTotalLength;

            unit.lastgoing = DateTime.Now;
        }

    }

    public class SniffUnit
    {
        public IPAddress target;

        public string servername;

        public long ingoing;

        public long outgoing;

        public long sizedata;

        public DateTime lastgoing;

        public string DisplayIP
        {
            get
            {
                return target.ToString();
            }
        }

        public string DisplayIngoing
        {
            get
            {
                return ingoing.ToString();
            }
        }
        public string DisplayOutgoing
        {
            get
            {
                return outgoing.ToString();
            }
        }

        public string DisplayBytes
        {
            get
            {
                return Utility.GetBytesReadable(sizedata);
            }
        }

        public string DisplayServer
        {
            get
            {
                return servername;
            }
        }

        public string DisplayDate
        {
            get
            {
                return lastgoing.ToShortTimeString();
            }
        }

        public void LookupServer()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(LookupServerGo), this);
        }

        void LookupServerGo(object state)
        {
            if (target.ToString().Substring(0, 7) == "192.168")
            {
                servername = "(local)";
                return;
            }
            var p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = "nslookup.exe",
                Arguments = target.ToString(),
                CreateNoWindow = true,
            };

            if (p.Start())
            {
                var o = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                // Check to last line
                o = o.Trim().Replace("\r", "");
                var s = o.Split('\n');
                if (s.Length <= 2)
                    return;

                servername = s.FirstOrDefault(x => x.IndexOf("Name: ") >= 0);
                if (string.IsNullOrEmpty(servername))
                    servername = "(unknown)";
                else
                    servername = servername.Substring(servername.LastIndexOf(' '));
            }
        }
    }
}
