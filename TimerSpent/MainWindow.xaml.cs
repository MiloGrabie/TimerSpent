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
using TimerSpent.ObjetMetier;

namespace TimerSpent
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string docPath;
        private List<InternalCustomTimeStamp> records;
        private List<StampCouple> stampCouples;
        private WordsManagerLib.WordsManager wordsManager;
        NotifyIcon notifyIcon = new NotifyIcon();
        private bool IsRecording;
        DispatcherTimer dispatcherTimer;
        string actualTime;
        private int limitHour = 6;

        public ProjectManager projectManager;
        private static MainWindow instance;

        public MainWindow()
        {
            InitializeComponent();

            projectManager = new ProjectManager();

            CreateMenuItem();

            docPath = System.IO.Path.Combine(GetOneDrivePath(), @"Documents\TimeSpent\TimeFile.json");
            if (!File.Exists(docPath))
            {
                docPath = ConfigFiles.Show();
            }
            records = new List<InternalCustomTimeStamp>();
            stampCouples = new List<StampCouple>();
            Read();
            wordsManager = new WordsManagerLib.WordsManager();
            InitSelectedProjectNumber(records.Last());

            //Temp();
            MainCall(new object(), new EventArgs());
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(MainCall);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 60);
            dispatcherTimer.Start();

            Refresh_TextNotifyIcon();
            this.Hide();
            instance = this;
            //ExportToExcelFormat();
        }

        private void CreateMenuItem()
        {
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
            menuItemRestart.Click += MenuItemRestart_Click;
            contextMenu.MenuItems.Add(menuItemRestart);

            MenuItem menuItemProject_Selection = new MenuItem { Text = "Projet..." };
            //menuItemRestart.Click += MenuItemRestart_Click;
            contextMenu.MenuItems.Add(menuItemProject_Selection);

            projectManager.AddMenuItem(menuItemProject_Selection);

            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.DoubleClick += Click_Icon_ON;

        }

        public static string GetOneDrivePath()
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Software\Microsoft\OneDrive";
            const string keyName = userRoot + "\\" + subkey;

            return (string)Microsoft.Win32.Registry.GetValue(keyName,
            "UserFolder",
            "Return this default if NoSuchName does not exist.");
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
            if (records.Last().Balise == BaliseType.Stop)
            {
                records.Remove(records.Last());
                records.Reverse();
                records.Remove(records.ToList()[1]);
                records.Reverse();
                Save();
            }
        }

        private void MenuItemEdit_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(docPath);
        }


        private void Save()
        {
            var temp = new List<ExternalCustomTimeStamp>();
            records.ForEach(a => temp.Add(new ExternalCustomTimeStamp(a)));
            var jsonString = new JavaScriptSerializer().Serialize(temp);
            File.WriteAllText(docPath, jsonString);
        }

        private void Read()
        {
            records.Clear();
            //tempConverter();
            using (StreamReader r = new StreamReader(docPath))
            {
                string json = r.ReadToEnd();
                if (json == "")
                    records = new List<InternalCustomTimeStamp>();
                else
                {
                    var temp = JsonConvert.DeserializeObject<List<ExternalCustomTimeStamp>>(json);
                    temp.ForEach(a => records.Add(new InternalCustomTimeStamp(a)));
                }
            }

            initCouple();
        }

        private void InitSelectedProjectNumber(InternalCustomTimeStamp stamp)
        {
            projectManager.SelectedProjectNumber = stamp.ProjectNumber;
        }

        private void initCouple()
        {
            stampCouples.Clear();
            bool isPair = records.Count() % 2 == 0;
            int stopCount = isPair ? records.Count : records.Count - 1 ;
            for (int i = 0; i < stopCount; i = i + 2)
            {
                if (records[i].Balise == BaliseType.Start)
                {
                    stampCouples.Add(new StampCouple
                    {
                        startTime = records[i],
                        stopTime = records[i + 1]
                    });
                }
            }
            if (records.Count() % 2 == 1)
                stampCouples.Add(new StampCouple
                {
                    startTime = records.Last(),
                    stopTime = new InternalCustomTimeStamp
                    {
                        Horodatage = DateTime.Now,
                        Balise = BaliseType.Stop
                    }
                });
        
        }

        private void tempConverter()
        {
            List<InternalCustomTimeStamp> tempText = new List<InternalCustomTimeStamp>();
            using (StreamReader r = new StreamReader(docPath.Replace(".json", " - Copie (8).json")))
            {
                string json = r.ReadToEnd();
                if (json == "")
                    tempText = new List<InternalCustomTimeStamp>();
                else
                    tempText = JsonConvert.DeserializeObject<List<InternalCustomTimeStamp>>(json);
            }

            records = tempText;
            //tempText.ForEach(a => text.Add(new InternalCustomTimeStamp(a)));
        }


        private void MenuItem_Click_Close(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainCall(object sender, EventArgs e)
        {
            Read();
            if (records.Count() != 0 && records.LastOrDefault().Balise == BaliseType.Start)
            {
                Textbox.Text = "Recording";
                IsRecording = true;
                notifyIcon.Icon = Properties.Resources.IconOn;
            }
            Refresh_TextNotifyIcon();
        }

        public void Refresh_TextNotifyIcon()
        {
            actualTime = getAllTime();
            string text = actualTime + $"\nProjet : {projectManager.SelectedProject.Description}";
            notifyIcon.Text = text;
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
            if (records.LastOrDefault().Balise != BaliseType.Stop)
            {
                Textbox.Text = "Not recording";
                records.Add(new InternalCustomTimeStamp
                {
                    Horodatage = DateTime.Now,
                    Balise = BaliseType.Stop,
                    ProjectNumber = projectManager.SelectedProjectNumber
                });
                Save();
                IsRecording = false;
            }
        }

        private void Button_Start(object sender, RoutedEventArgs e) // Start
        {
            Read();
            if (records.Count() == 0 || records.LastOrDefault().Balise != BaliseType.Start)
            {
                records.Add(new InternalCustomTimeStamp
                {
                    Horodatage = DateTime.Now,
                    Balise = BaliseType.Start,
                    ProjectNumber = projectManager.SelectedProjectNumber
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
            double seconds = 0;
            foreach (var item in stampCouples)
            {
                seconds += item.elapsedTime.TotalSeconds;
            }

           
            if (records.Last().Balise == BaliseType.Start)
                timeInDay = timeInDay.Add(new TimeSpan((DateTime.Now - records.Last().Horodatage).Ticks));
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            int hours = (timeSpan.Days*24) + timeSpan.Hours;
            int min = timeSpan.Minutes;
            string h = hours.ToString() + " h - " + min.ToString() + " min";
            //h += "\nAujourd'hui : " + new DateTime(timeInDay.Ticks).ToString("HH:mm").Replace(":", "h");
            return h;

        }

        public static MainWindow GetInstance()
        {
            return instance;
        }

        public void ExportToExcelFormat()
        {
            var temp = new List<DurationTimeStamp>();
            stampCouples.ForEach(a => temp.Add(new DurationTimeStamp(a, projectManager)));
            var jsonString = new JavaScriptSerializer().Serialize(temp);
            string path = System.IO.Path.Combine(GetOneDrivePath(), @"Documents\TimeSpent\TimeFileExcel.json");
            File.WriteAllText(path, jsonString);
        }


    }

    internal class DurationTimeStamp
    {
        public DurationTimeStamp(StampCouple a, ProjectManager projectManager)
        {
            StartTime = a.startTime.Horodatage.ToString();
            elapsedTime = a.elapsedTime.ToString();
            project = projectManager.Projects.First(p => p.Number == a.startTime.ProjectNumber).Description;
        }

        public string StartTime { get; set; }
        public string elapsedTime { get; set; }
        public string project { get; set; }

    }

    public class StampCouple
    {
        public TimeSpan elapsedTime { get
            {
                return (stopTime.Horodatage - startTime.Horodatage);
            }
        }
        public InternalCustomTimeStamp startTime { get; set; }
        public InternalCustomTimeStamp stopTime { get; set; }
    }

    public class InternalCustomTimeStamp
    {
        public InternalCustomTimeStamp()
        {}
        public InternalCustomTimeStamp(ExternalCustomTimeStamp item)
        {
            Horodatage = Convert.ToDateTime(item.Date + " " + item.Time);
            Balise = (item.Balise == "start") ? BaliseType.Start : BaliseType.Stop;
            ProjectNumber = item.ProjectNumber;
        }

        public DateTime Horodatage { set; get; }
        public BaliseType Balise { set; get; }
        public int ProjectNumber { set; get; }
    }

    public class ExternalCustomTimeStamp
    {
        public ExternalCustomTimeStamp() { }
        public ExternalCustomTimeStamp(InternalCustomTimeStamp item)
        {
            Date = item.Horodatage.ToShortDateString();
            Time = item.Horodatage.ToShortTimeString();
            Balise = (item.Balise == BaliseType.Start) ? "start" : "stop";
            ProjectNumber = item.ProjectNumber;
        }
        public string Date { set; get; }
        public string Time { set; get; }
        public string Balise { set; get; }
        public int ProjectNumber { set; get; }
    }

    public enum BaliseType
    {
        Stop,
        Start
    }
}
