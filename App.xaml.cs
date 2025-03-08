using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using APlayer.SaveData;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.Storage;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        static App()
        {
            var local = Windows.Storage.ApplicationData.Current.LocalSettings;
            var user = local.Values["GamepadUserIndex"] as uint?;
            if (user == null || user >= 4)
                user = 0;
            var interval = local.Values["GamepadInterval"] as TimeSpan?;
            if (interval == null || interval  <= TimeSpan.Zero)
                interval = TimeSpan.FromMilliseconds(16);
            Gamepad = new(user.Value,interval.Value);
        }

        public static Window? MainWindow { get; private set; }

        public static Gamepad Gamepad { get; private set; }

        private static SoundPlayer soundPlayer = new();
        public static ISoundPlayer SoundPlayer { get => soundPlayer; }

        public static StorageFolder? SaveFolder { get; private set; } = null;
        public static SaveData.Contents SavedContents { get; private set; } = new SaveData.Contents();
        public static Dictionary<string,SaveData.List> SavedLists { get; private set; } = [];
        public static List<string> DeleteLists { get; private set; } = [];
        public static SaveData.List? CurrentList { get; set; } = null;

        public static SaveData.GamepadAssign.SaveData AssignData { get; private set; } = new();

        public static void SetAssignData(SaveData.GamepadAssign.SaveData data,Type lastDataType)
        {
            AssignData = data;
            AssignDataChanged?.Invoke(null, lastDataType);
        }
        public static event EventHandler<Type>? AssignDataChanged;

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var local_folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            SaveFolder = await local_folder.CreateFolderAsync("SaveData", Windows.Storage.CreationCollisionOption.OpenIfExists);

            var assign_data = await GamepadAssign.Load(SaveFolder);
            AssignData = assign_data;

            var contents = await SaveData.SaveData.LoadContents(SaveFolder, "index.json");
            if (contents != null)
            {
                SavedContents = contents;
                foreach (var item in SavedContents.Indexes)
                {
                    var list = await SaveData.SaveData.LoadList(SaveFolder, item.FileName);
                    if (list != null)
                        SavedLists.Add(item.FileName,list);
                    else
                        item.FileName = "";
                }
                if (SavedLists.Count != SavedContents.Indexes.Count)
                    SavedContents.Indexes = new List<SaveData.ListIndex>(SavedContents.Indexes.Where(item => item.FileName != ""));
            }
            else
            {
                SavedContents = new SaveData.Contents();
                var files = await SaveFolder.GetFilesAsync();
                int i = 0;
                foreach (var item in files)
                {
                    if (item.Name == "index.json")
                        continue;
                    var list = await SaveData.SaveData.LoadList(SaveFolder, item.Name);
                    if (list != null)
                    {
                        SavedLists.Add(item.Name,list);
                        SavedContents.Indexes.Add(new SaveData.ListIndex(list.Name, item.Name, i));
                        i++;
                    }
                }
            }
            SavedContents.Indexes.Sort((a, b) => a.Order - b.Order);

            MainWindow = new MainWindow();
            MainWindow.Activate();

            await soundPlayer.Initialize();
//            soundPlayer.InsertPeakDetector();

        }

//        private Window? m_window;
    }
}
