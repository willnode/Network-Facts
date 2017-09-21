using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NativeWifi;

namespace Network_Facts
{
    public class NetFi
    {

        public List<FiProfile> Profiles = new List<FiProfile>();

        public WlanClient.WlanInterface iFace;

        public NetFi(WlanClient.WlanInterface iFace)
        {
            this.iFace = iFace;
        }

        public void Connect(string ssid)
        {
            iFace.Connect(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, ssid);
        }

        public void Arcquire()
        {
            Profiles.Clear();

            WlanClient client = new WlanClient();

            // Lists all networks with WEP security
            Wlan.WlanAvailableNetwork[] networks = iFace.GetAvailableNetworkList(0);

            var x = new System.Xml.Serialization.XmlSerializer(typeof(WLANProfile));

            // Retrieves XML configurations of existing profiles.
            // This can assist you in constructing your own XML configuration
            foreach (Wlan.WlanProfileInfo profileInfo in iFace.GetProfiles())
            {
                string name = profileInfo.profileName; // this is typically the network's SSID

                string xml = iFace.GetProfileXml(name);

                using (TextReader reader = new StringReader(xml))
                {
                    var prov = x.Deserialize(reader) as WLANProfile;
                    Profiles.Add(new FiProfile(prov));
                }
            }

            foreach (var net in networks)
            {
                var ssid = net.dot11Ssid.ToStringBeauty();
                if (string.IsNullOrEmpty(ssid))
                    continue;
                var prof = Profiles.Where((p) => p.SSID == ssid).FirstOrDefault();
                if (prof != null)
                    prof.SetAvailability(net);
                else
                {
                    prof = new FiProfile(null);
                    prof.SetAvailability(net);
                    Profiles.Add(prof);
                }
            }

            Profiles.Sort(new FiProfileSorter());
        }



        public override string ToString()
        {
            return iFace.InterfaceName;
        }
    }

    public class FiProfile
    {

        public bool connected;
        public bool available;
        public bool profiled;

        public string SSID;
        public double Strength;
        public string Security;
        public string Encryption;

        public void SetAvailability(Wlan.WlanAvailableNetwork availability)
        {
            available = true;
            var a = availability;
            SSID = SSID ?? a.dot11Ssid.ToStringBeauty();
            Strength = a.wlanSignalQuality;
            Security = a.dot11DefaultAuthAlgorithm.ToString();
            Encryption = a.dot11DefaultCipherAlgorithm.ToString();
            connected |= (a.flags & Wlan.WlanAvailableNetworkFlags.Connected) > 0;
        }

        public FiProfile(WLANProfile profile)
        {
            available = false;
            if (profiled = profile != null)
            {
                SSID = profile.name;
                Strength = 0;
                Security = profile.MSM.security.authEncryption.authentication;
                Encryption = profile.MSM.security.authEncryption.encryption;
                connected = false;
            }
        }


        public string DisplaySSID
        {
            get
            {
                return SSID;
            }
        }


        public string DisplayAuth
        {
            get
            {
                return Security + " / " + Encryption;
            }
        }

        public string DisplayStrength
        {
            get
            {
                return Strength == 0 ? "" : Strength + "%";
            }
        }

        public bool IsConnected
        {
            get
            {
                return connected;
            }
        }

        public bool IsAvailable
        {
            get
            {
                return available;
            }
        }
    }

    public class FiProfileSorter : IComparer<FiProfile>
    {
        public int Compare(FiProfile x, FiProfile y)
        {
            if (x.connected ^ y.connected)
            {
                if (x.connected)
                    return -1;
                else
                    return 1;
            }
            if (x.available ^ y.available)
            {
                if (x.available)
                    return -1;
                else
                    return 1;
            }
            if (x.profiled ^ y.profiled)
            {
                if (x.profiled)
                    return -1;
                else
                    return 1;
            }

            return string.CompareOrdinal(x.SSID, y.SSID);
        }
    }


}
