using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Network_Facts
{
    public class NetStat
    {
        [NonSerialized]
        NetworkInterface nic;

        public string name;
        public string id;
        public OperationalStatus status;

        public long bytesSent;
        public long bytesReceived;
        public long bytesSpeed;

        public bool logUpdated;

        public List<DataLog> Logs = new List<DataLog>();
        
        public NetStat() { }

        public NetStat(NetworkInterface nic)
        {
            this.nic = nic;
            name = nic.Name;
            id = nic.Id;
        }

        public void Update()
        {
            status = nic.OperationalStatus;

            var stat = nic.GetIPv4Statistics();

            logUpdated = UpdateLog(bytesSent = stat.BytesSent, bytesReceived = stat.BytesReceived, out bytesSpeed);
        }

        public override string ToString()
        {
            return name;
        }

        public string Save()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static NetStat Load(string data, NetworkInterface nic)
        {
            if (data == null)
            {
                return new NetStat(nic);
            }
            else
            {
                var job = JsonConvert.DeserializeObject<NetStat>(data);
                job.nic = nic;

                return job;
            }
        }

        // Returns overall speed
        bool UpdateLog(long up, long down, out long speed)
        {
            var date = DateTime.Now.Date;
            if (Logs.Count == 0)
            {
                Logs.Add(new DataLog() { date = date, up = up, down = down, upSession = up, downSession = down, sessions = 1 });
                speed = up + down;
                return true;
            }
            // The most recent is always in the last log
            var v = Logs[Logs.Count - 1];

            var dtu = up - v.upSession;
            var dtd = down - v.downSession;

            if (dtu < 0 | dtd < 0)
            {
                v.upSession = 0;
                v.downSession = 0;
                v.up += up;
                v.down += down;
                v.sessions++;
            }
            else
            {
                v.upSession = up;
                v.downSession = down;
                v.up += dtu;
                v.down += dtd;
            }

            if (v.date == date)
            {
                Logs[Logs.Count - 1] = v;
                speed = Math.Max(0, dtd + dtu);
                return false;
            }
            else
            {
                // Going to be another day
                v.sessions = 1;
                v.date = date;
                v.up = dtu;
                v.down = dtd;
                Logs.Add(v);
                speed = Math.Max(0, dtd + dtu);
                return true;
            }

        }
    }

    [Serializable]
    public struct DataLog
    {
        public DateTime date;
        public long up;
        public long down;

        public int sessions;

        // this additional counter is only used for 'today'
        public long upSession;
        public long downSession;

        public string DisplayDate
        {
            get
            {
                return date == DateTime.Now.Date ? "Today" : date.ToLongDateString();
            }
        }

        public string DisplayUp
        {
            get
            {
                return Utility.GetBytesReadable(up);
            }
        }


        public string DisplayDown
        {
            get
            {
                return Utility.GetBytesReadable(down);
            }
        }

        public string DisplaySessions
        {
            get
            {
                return "#" + sessions.ToString();
            }
        }
    }

}
