using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace TimerSpent.ObjetMetier
{
    public class ProjectManager
    {
        public List<Project> Projects { get; set; }

        public int SelectedProjectNumber
        {
            get
            {
                return SelectedProject.Number;
            }
            set
            {
                SelectedProject = Projects.FirstOrDefault(a => a.Number == value);
            }
        }
        public Project SelectedProject { get; set; }

        public ProjectManager()
        {
            Projects = new List<Project>();
            Load();
        }

        public void Load()
        {
            Projects.Clear();
            //tempConverter();
            using (StreamReader r = new StreamReader(GetJsonPath()))
            {
                string json = r.ReadToEnd();
                if (json == "")
                    Projects = new List<Project>();
                else
                    Projects = JsonConvert.DeserializeObject<List<Project>>(json);
            }
        }

        public void Save()
        {
            var jsonString = new JavaScriptSerializer().Serialize(Projects);
            File.WriteAllText(GetJsonPath(), jsonString);
        }

        private string GetJsonPath()
        {
            return Path.Combine(MainWindow.GetOneDrivePath(), @"Documents\TimeSpent\Projects.json");
        }

        public void AddMenuItem(MenuItem menuItemProject_Selection)
        {
            foreach (var project in Projects)
            {
                var menuItem = new MenuItem_Project { Text = project.ToString(), Project = project };
                menuItem.Click += MenuItem_Click_ProjectSelection;
                menuItemProject_Selection.MenuItems.Add(menuItem);
            }
        }

        private void MenuItem_Click_ProjectSelection(object sender, EventArgs e)
        {
            SelectedProject = (sender as MenuItem_Project).Project;
            MainWindow.GetInstance().Refresh_TextNotifyIcon();
        }
    }

    class MenuItem_Project : MenuItem
    {
        public Project Project { get; set; }
        public MenuItem_Project() : base() { }

    }
}
