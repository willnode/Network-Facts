using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace Network_Facts
{
    public class NetPublic
    {

        public IPAddress IP;

        public IPAPI API;

        public OWEATHERMAP WER;

        public BitmapImage LOC;


        public void Arcquire()
        {
            try
            {
            if (LOC != null)
                LOC.StreamSource.Dispose();


                var web = new WebClient();

                var ip = web.DownloadString("http://ip-api.com/json").Trim();

                API = JsonConvert.DeserializeObject<IPAPI>(ip);

                IP = IPAddress.Parse(API.Query);

                var wer = web.DownloadString("http://api.openweathermap.org/data/2.5/weather?appid=bd5e378503939ddaee76f12ad7a97608&lat=" + API.Lat + "&lon=" + API.Lon).Trim();

                WER = JsonConvert.DeserializeObject<OWEATHERMAP>(wer);

                var img = web.DownloadData("https://maps.googleapis.com/maps/api/staticmap?zoom=10&size=700x350&maptype=roadmap&markers=color:red%7Clabel:C%7C" + API.Lat + "," + API.Lon + "&key=AIzaSyDWO8tV87DC4tCaHOLoADkL71G-jcyBdwk");
                var mem = new MemoryStream(img);            
                LOC = new BitmapImage();
                LOC.BeginInit();
                LOC.StreamSource = mem;
        //        LOC.CacheOption = BitmapCacheOption.OnLoad;
                LOC.EndInit();
            
                LOC.Freeze();
                //                mem.Dispose();                
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to receive Public IP. Please try again later",
                    e.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }


    public struct IPAPI
    {
        public string As;
        public string City;
        public string Country;
        public string CountryCode;
        public string Isp;
        public double Lat;
        public double Lon;
        public string Org;
        public string Query;
        public string RegionName;
        public string Status;
        public string Timezone;
        public string Zip;
    }

    public struct OWEATHERMAP
    {
        public struct COORD
        {
            public double lon;
            public double lat;
        }

        public struct SYS
        {
            public string message;
            public string country;
            public string sunrise;
            public string sunset;
        }

        public struct WEATHER
        {
            public long id;
            public string main;
            public string description;
            public string icon;
        }

        public struct MAIN
        {
            public double temp;
            public double humidity;
            public double pressure;
            public double temp_min;
            public double temp_max;
            public double sea_level;
            public double grnd_level;
        }

        public struct WIND
        {
            public double speed;
            public double deg;
        }
        public struct CLOUDS
        {
            public long all;
        }

        public COORD Coord;
        public SYS Sys;
        public WEATHER[] Weather;
        public MAIN Main;
        public WIND Wind;
        public CLOUDS Clouds;

        public long Id;
        public string Name;
        public long Cod;
    }

}
