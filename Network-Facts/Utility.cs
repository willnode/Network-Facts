using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Network_Facts
{
    public static class Utility
    {
        public static double ToFahrenheit(double K)
        {
            return 9.0 / 5.0 * (K - 273.15) + 32;
        }

        public static double ToCelcius(double K)
        {
            return (K - 273.15);
        }

        public static string ToStringBeauty(this NativeWifi.Wlan.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }

        public static string ToStringBeauty(this PhysicalAddress addr)
        {
            return string.Join(":", (from z in addr.GetAddressBytes() select z.ToString("X2")).ToArray());
        }

        // Returns the human-readable file size for an arbitrary, 64-bit file size 
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        public static string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024.0);
            // Return formatted number with suffix
            return readable.ToString("0.00 ") + suffix;
        }


        public static bool Startup
        {
            get
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                   ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                return (string)rk.GetValue("Net-Watcher", null) == Process.GetCurrentProcess().StartInfo.FileName + " --background";
            }
            set
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (value)
                    rk.SetValue("Net-Watcher", Process.GetCurrentProcess().StartInfo.FileName + " --background");
                else
                    rk.DeleteValue("Net-Watcher", false);
            }
        }

        public static RegistryKey GetRegPath()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);

            key.CreateSubKey("NetWatcher");
            return key.OpenSubKey("NetWatcher", true);
        }

        public static bool HaveAdminAccess
        {
            get
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            set
            {
                if (!value || HaveAdminAccess) return;
                // Restart
                var exe = Process.GetCurrentProcess().MainModule.FileName;
                var strt = new ProcessStartInfo(exe) { Verb = "runas" };
                Process.Start(strt);
                Application.Current.Shutdown(0);
            }
        }
    }
}
