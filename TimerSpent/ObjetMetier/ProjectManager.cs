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
        private Project selectedProject;

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
        public Project SelectedProject { get => selectedProject; set
            {
                selectedProject = value;
                MenuItemList.FirstOrDefault(a => a.Project == SelectedProject).Checked = true;
                MenuItemList.Where(a => a.Project != SelectedProject)?.ToList()?.ForEach(a => a.Checked = false);
            }
        }
        public List<MenuItem_Project> MenuItemList { get; } = new List<MenuItem_Project>();

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
                MenuItemList.Add(menuItem);
            }
        }

        private void MenuItem_Click_ProjectSelection(object sender, EventArgs e)
        {
            SelectedProject = (sender as MenuItem_Project).Project;
            (sender as MenuItem_Project).Checked = true;
            MainWindow.GetInstance().Refresh_TextNotifyIcon();
        }
    }

    public class MenuItem_Project : MenuItem
    {
        public Project Project { get; set; }
        public MenuItem_Project() : base() { }

    }
}
