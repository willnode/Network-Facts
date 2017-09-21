using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Net.NetworkInformation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using NativeWifi;

namespace Network_Facts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var reg = Utility.GetRegPath();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Real device should not have invalid MAC address
                var addr = nic.GetPhysicalAddress()?.GetAddressBytes();
                if (addr?.Length > 0 && addr[0] > 0)
                    _nStatDev.Items.Add(NetStat.Load(reg.GetValue("data_" + nic.Id) as string, nic));

                _nDetailsDev.Items.Add(new NetDetails(nic));
            }

            try
            {
                foreach (var nic in new WlanClient().Interfaces)
                {
                    _nFiDev.Items.Add(new NetFi(nic));
                }

            }
            catch (Exception) { }

            try
            {
                _nStatDev.SelectedIndex = 0;
                _nDetailsDev.SelectedIndex = 0;
                _nFiDev.SelectedIndex = 0;
            }
            catch (Exception) { }

            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();

            UpdateDetails();
        }

        long i = 0;

        private void Timer_Tick(object sender, EventArgs e)
        {
            i++;

            foreach (NetStat nic in _nStatDev.Items)
            {
                nic.Update();
            }

            if (_nStat.IsExpanded | i % 10 == 0)
            UpdateStat(false);

            if (_nFi.IsExpanded)
            {
                if (i % 2 == 0)
                    UpdateFi();
            }
            if (_sniffStarted && _nSniff.IsExpanded)
            {
                UpdateSniff();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var path = Utility.GetRegPath();
            foreach (NetStat nic in _nStatDev.Items)
            {
                path.SetValue("data_" + nic.id, nic.Save());
            }
            sniff.Stop();
        }

        // ----------------------------------- NetStat --------------------------------------------------------------

        void UpdateStat(bool updateDev)
        {

            var nic = _nStatDev?.SelectedItem as NetStat;

            if (nic == null)
                return;

            if (_nStat.IsExpanded)
            {
                _nStatUp.Content = Utility.GetBytesReadable(nic.bytesSent);
                _nStatDown.Content = Utility.GetBytesReadable(nic.bytesReceived);
                _nStatSpeed.Content = Utility.GetBytesReadable(nic.bytesSpeed) + "ps";
            }
            _nStatS.Content = nic.status.ToString();

            if (nic.logUpdated || updateDev)
            {
                _nStatLog.BeginInit();
                _nStatLog.Items.Clear();

                foreach (var log in nic.Logs)
                {
                    _nStatLog.Items.Add(log);
                }

                _nStatLog.EndInit();
            }
            else if (_nStatLog.Items.Count > 0)
                // Just update today's log
                _nStatLog.Items[_nStatLog.Items.Count - 1] = nic.Logs[nic.Logs.Count - 1];
        }


        private void _nStatDev_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStat(true);
        }

        private void _nStat_Expanded(object sender, RoutedEventArgs e)
        {
            UpdateStat(true);
        }

        // ----------------------------------- NetDetails --------------------------------------------------------------

        private void _nDetailsDev_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDetails();
        }

        private void UpdateDetails()
        {
            var nic = _nDetailsDev.SelectedItem as NetDetails;
            nic.Init();
            _nDet0.Text = nic.desc;
            _nDet1.Text = nic.type.ToString();
            _nDet2.Text = nic.ipv4?.ToString() ?? "--";
            _nDet3.Text = string.Join("\r\n", Array.ConvertAll(nic.ipv6, x => x.ToString()));
            _nDet4.Text = nic.mac?.ToStringBeauty() ?? "--";
            _nDet5.Text = Utility.GetBytesReadable(nic.maxSpeed) + "ps";

        }

        // ----------------------------------- NetPublic --------------------------------------------------------------

        NetPublic nPublic = new NetPublic();

        void UpdatePublicFinal()
        {
            _nPublicDo.IsEnabled = true;
            _nPublicDo.Content = "Update";
            if (nPublic.IP == null)
                return; // Mostly fail

            _nPublicIP.Text = nPublic.IP.ToString();
            _nPublicVen.Text = nPublic.API.Org;
            _nPublicGeo.Text = nPublic.API.City + ", " + nPublic.API.Country;
            _nPublicLoc.Text = "Lat: " + nPublic.API.Lat + "° Lon: " + nPublic.API.Lon + "°";
            _nPublicTemp.Text = nPublic.WER.Weather?[0].main + " " + Utility.ToFahrenheit(nPublic.WER.Main.temp).ToString("F2") + "°F/" + Utility.ToCelcius(nPublic.WER.Main.temp).ToString("F2") + "°C";
            _nPublicTempStat.Text = "Ps: " + nPublic.WER.Main.pressure + " hPa  Hm: " + nPublic.WER.Main.humidity + "%  Wnd: " + nPublic.WER.Wind.speed + " mph";
            _nPublicGeoImg.Source = nPublic.LOC;
        }

        private void _nPublicDo_Click(object sender, RoutedEventArgs e)
        {
            _nPublicGeoImg.Source = null;
            _nPublicDo.IsEnabled = false;
            _nPublicDo.Content = "Gathering...";
            new Thread(new ThreadStart(() =>
            {
                nPublic.Arcquire();
                Thread.Sleep(500);
                Application.Current.Dispatcher.Invoke(UpdatePublicFinal);
            })).Start();
        }


        // ----------------------------------- NetLocal --------------------------------------------------------------

        NetLocal nLocal = new NetLocal();

        void UpdateLocalFinal()
        {
            _nLocalBut.IsEnabled = true;
            _nLocalBut.Content = "Update";

            _nLocalList.BeginInit();
            _nLocalList.Items.Clear();
            foreach (var l in nLocal.locals)
            {
                _nLocalList.Items.Add(l);
            }
            _nLocalList.EndInit();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _nLocalBut.IsEnabled = false;
            _nLocalBut.Content = "Gathering...";
            new Thread(new ThreadStart(() =>
            {
                nLocal.Arquire();
                Application.Current.Dispatcher.Invoke(UpdateLocalFinal);
            })).Start();
        }

        // ----------------------------------- NetFi --------------------------------------------------------------

        void UpdateFi()
        {

            if (_nFiDev.SelectedItem == null)
                return;

            var fi = _nFiDev.SelectedItem as NetFi;
            var idx = _nFiList.SelectedIndex;
            var foc = _nFiList.IsFocused;

            fi.Arcquire();

            _nFiList.BeginInit();
            _nFiList.Items.Clear();


            foreach (var nic in fi.Profiles)
            {
                _nFiList.Items.Add(nic);
            }

            _nFiList.EndInit();

            _nFiList.SelectedIndex = idx;
            if (foc)
                _nFiList.Focus();
        }

        private void _nFi_Expanded(object sender, RoutedEventArgs e)
        {
            UpdateFi();
        }

        // ----------------------------------- NetSniff --------------------------------------------------------------

        public NetSniffer sniff = new NetSniffer();
        public bool _sniffStarted;

        private void _nSniffDo_Click(object sender, RoutedEventArgs e)
        {
            if (!_sniffStarted)
            {
                if (!sniff.Start())
                    return;
                _nSniffDo.Content = "Stop";
            }
            else
            {
                sniff.Stop();
                _nSniffDo.Content = "Start";
            }
            _sniffStarted = !_sniffStarted;
        }

        void UpdateSniff ()
        {
            _nSniffList.BeginInit();
            int i = 0;
            foreach (var unit in sniff.sniffed)
            {
                if (_nSniffList.Items.Count == i)
                    _nSniffList.Items.Add(unit);
                else
                    _nSniffList.Items[i] = unit;
                i++;
            }
            _nSniffList.EndInit();
        }
    }
}
