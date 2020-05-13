using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Windows.Forms;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Web.Script.Serialization;

namespace TimerSpent
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string docPath;
        private List<CustomTimeStamp> text;
        private WordsManagerLib.WordsManager wordsManager;
        NotifyIcon notifyIcon = new NotifyIcon();
        private bool IsRecording;
        DispatcherTimer dispatcherTimer;
        string actualTime;
        private int limitHour = 6;

        public MainWindow()
        {
            InitializeComponent();
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Software\Microsoft\OneDrive";
            const string keyName = userRoot + "\\" + subkey;

            string onedrivePath = (string)Microsoft.Win32.Registry.GetValue(keyName,
            "UserFolder",
            "Return this default if NoSuchName does not exist.");

            notifyIcon.Icon = Properties.Resources.IconOff;
            notifyIcon.Visible = true;
            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItemClose = new MenuItem { Text = "Close" };
            menuItemClose.Click += MenuItem_Click_Close;
            contextMenu.MenuItems.Add(menuItemClose);

            MenuItem menuItemEdit = new MenuItem { Text = "Edit" };
            menuItemEdit.Click += MenuItemEdit_Click;
            contextMenu.MenuItems.Add(menuItemEdit);

            MenuItem menuItemELL = new MenuItem { Text = "Erase last line" };
            menuItemELL.Click += MenuItemELL_Click; ;
            contextMenu.MenuItems.Add(menuItemELL);

            MenuItem menuItemRestart = new MenuItem { Text = "Redémarrer" };
            menuItemRestart.Click += MenuItemRestart_Click; ; ;
            contextMenu.MenuItems.Add(menuItemRestart);

            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.DoubleClick += Click_Icon_ON;

            docPath = System.IO.Path.Combine(onedrivePath, @"Documents\TimeSpent\TimeFile.json");
            Read();
            wordsManager = new WordsManagerLib.WordsManager();

            //Temp();
            MainCall(new object(), new EventArgs());
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(MainCall);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 60);
            dispatcherTimer.Start();

            notifyIcon.Text = getAllTime();
            this.Hide();
        }

        private void MenuItemRestart_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Restart();
            System.Windows.Forms.Application.Exit();
            System.Windows.Forms.Application.ExitThread();
            this.Close();
        }

        private void MenuItemELL_Click(object sender, EventArgs e)
        {
            Read();
            if (text.Last().Balise == BaliseType.Stop)
            {
                text.Remove(text.Last());
                text.Reverse();
                text.Remove(text.ToList()[1]);
                text.Reverse();
                Save();
            }
        }

        private void MenuItemEdit_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(docPath);
        }

        private void Save()
        {
            var jsonString = new JavaScriptSerializer().Serialize(text);
            File.WriteAllText(docPath, jsonString);
        }

        private void Read()
        {
            //tempConverter();
            using (StreamReader r = new StreamReader(docPath))
            {
                string json = r.ReadToEnd();
                if (json == "")
                    text = new List<CustomTimeStamp>();
                else
                    text = JsonConvert.DeserializeObject<List<CustomTimeStamp>>(json);
            }
        }
        private void tempConverter()
        {
            List<OldCustomTimeStamp> tempText = new List<OldCustomTimeStamp>();
            using (StreamReader r = new StreamReader(docPath.Replace(".json", " - Copie (7).json")))
            {
                string json = r.ReadToEnd();
                if (json == "")
                    tempText = new List<OldCustomTimeStamp>();
                else
                    tempText = JsonConvert.DeserializeObject<List<OldCustomTimeStamp>>(json);
            }

            text = new List<CustomTimeStamp>();
            foreach (var item in tempText)
            {
                DateTime horodatage = Convert.ToDateTime(item.Date + " " + item.Time);
                BaliseType balise = (item.Balise == "StartTime") ? BaliseType.Start : BaliseType.Stop;
                text.Add(new CustomTimeStamp { Balise = balise, Horodatage = horodatage });
            }
        }


        private void MenuItem_Click_Close(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainCall(object sender, EventArgs e)
        {
            Read();
            if (text.Count() != 0 && text.LastOrDefault().Balise == BaliseType.Start)
            {
                Textbox.Text = "Recording";
                IsRecording = true;
                notifyIcon.Icon = Properties.Resources.IconOn;
            }
            actualTime = getAllTime();
            notifyIcon.Text = actualTime;
        }

        private void Click_Icon_ON(object sender, EventArgs e)
        {
            if (IsRecording)
            {
                Button_Stop(new object(), new RoutedEventArgs());
                notifyIcon.Icon = Properties.Resources.IconOff;
                IsRecording = false;
            }
            else
            {
                Button_Start(new object(), new RoutedEventArgs());
                notifyIcon.Icon = Properties.Resources.IconOn;
                IsRecording = true;
            }
        }

        private void Button_Stop(object sender, RoutedEventArgs e)   // Stop
        {
            Read();
            if (text.LastOrDefault().Balise != BaliseType.Stop)
            {
                Textbox.Text = "Not recording";
                text.Add(new CustomTimeStamp
                {
                    Horodatage = DateTime.Now,
                    Balise = BaliseType.Stop
                });
                Save();
                IsRecording = false;
            }
        }

        private void Button_Start(object sender, RoutedEventArgs e) // Start
        {
            Read();
            if (text.Count() == 0 || text.LastOrDefault().Balise != BaliseType.Start)
            {
                text.Add(new CustomTimeStamp
                {
                    Horodatage = DateTime.Now,
                    Balise = BaliseType.Start
                });
                Textbox.Text = "Recording";
                Save();
                IsRecording = true;
            }
        }

        private void Calcul_Button(object sender, RoutedEventArgs e)   // calcul line
        {
            string h = getAllTime();
            TimeResult.Text = h;
        }

        private string getAllTime()
        {
            Read();
            DateTime now = DateTime.Now;
            if (DateTime.Now.Hour < limitHour)
                now = now.AddDays(-1);
            DateTime yesterday = new DateTime(now.Year, now.Month, now.Day, limitHour, now.Minute, now.Second);
            TimeSpan timeInDay = new TimeSpan();
            double hour = 0;
            for (int i = 0; i < text.Count; i = i + 2)
            {
                CustomTimeStamp startItem = text[i];
                try
                {
                    CustomTimeStamp stopItem = text[i + 1];
                    if (startItem.Balise == BaliseType.Start && stopItem.Balise == BaliseType.Stop)
                    {
                        hour += (stopItem.Horodatage - startItem.Horodatage).TotalHours;
                        if (startItem.Horodatage >= yesterday)
                            timeInDay = timeInDay.Add(new TimeSpan((stopItem.Horodatage - startItem.Horodatage).Ticks));
                        if (startItem.Horodatage.Date == DateTime.Now.Date)
                            continue;
                    }
                    else
                    {
                        hour += (DateTime.Now - startItem.Horodatage).TotalHours;
                        if (startItem.Horodatage.Date >= yesterday)
                            timeInDay = timeInDay.Add(new TimeSpan((DateTime.Now - startItem.Horodatage).Ticks));
                    }
                }
                catch
                {
                    hour += (DateTime.Now - startItem.Horodatage).TotalHours;
                }
            }
            TimeSpan timeSpan = TimeSpan.FromHours(hour);
            int hours = (timeSpan.Days * 24) + timeSpan.Hours;
            int min = timeSpan.Minutes;
            string h = hours.ToString() + " h - " + min.ToString() + " min";
            h += "\nAujourd'hui : " + new DateTime(timeInDay.Ticks).ToString("HH:mm").Replace(":", "h");
            return h;

            /*List<DateTime> startList = text.Where(a => a.Balise.Contains("Start")).Select(a=>Convert.ToDateTime(a.Date+" "+a.Time)).ToList();
            List<DateTime> stopList = text.Where(a => a.Balise.Contains("Stop")).Select(a=>Convert.ToDateTime(a.Date + " " + a.Time)).ToList();

            double hour = 0;
            foreach (DateTime stamp in startList)
            {
                try
                {
                    hour += (stopList[startList.IndexOf(stamp)] - stamp).TotalHours;
                }
                catch (Exception)
                {
                    hour += (DateTime.Now - stamp).TotalHours;
                }

            }*/

        }

        //public List<DateTime> treatLine(List<CustomTimeStamp> list)
        //{
        //    List<DateTime> timeStamp = new List<DateTime>();
        //    foreach (CustomTimeStamp stamp in list)
        //    {
        //        timeStamp.Add(Convert.ToDateTime(stamp.Date));
        //    }
        //    return timeStamp;
        //}

        /*public void CalculMoyenne()
        {
            Read();
            wordsManager.stockLine(text, true);
            List<WordsManagerLib.Line> start = wordsManager.getLine("Start");
            List<DateTime> startList = treatLine(start);
            List<WordsManagerLib.Line> stop = wordsManager.getLine("Stop");
            List<DateTime> stopList = treatLine(stop);

            Dictionary<DateTime,double> deltaTime = new Dictionary<DateTime, double>();
            DateTime actualDay = new DateTime();
            double timeSpans = 0;
            foreach (var item in startList)
            {
                timeSpans += (stopList[startList.IndexOf(item)] - item).TotalHours;
                if (actualDay != item.Date)
                {
                    actualDay = item.Date;
                    deltaTime.Add(actualDay, timeSpans);
                    timeSpans = 0;
                }
            }
            double count = Convert.ToDouble(deltaTime.Count());
            double total = 0;
            foreach (var item in deltaTime)
            {
                total += item.Value;
            }
            double average = total / count;
        }*/


    }

    public class CustomTimeStamp
    {
        public DateTime Horodatage { set; get; }
        public BaliseType Balise { set; get; }
    }

    public class OldCustomTimeStamp
    {
        public string Date { set; get; }
        public string Time { set; get; }
        public string Balise { set; get; }
    }

    public enum BaliseType
    {
        Stop,
        Start
    }
}
