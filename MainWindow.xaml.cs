using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Reflection;
using Windows.Graphics;
using ABI.Windows.Foundation;
using System.Diagnostics;
using VersionAlert;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Version_Alert
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public static MainWindow mainWindow;

        public static string? initPath;
        public static string? packagesPath;
        public static string? playsetsPath;
        public static string? playgroundPath;

        Dictionary<string, string> playsets;

        public MainWindow()
        {
            this.InitializeComponent();
            AppWindow.MoveAndResize(new RectInt32(_X: 560, _Y: 280, _Width: 800, _Height: 600));
            playsets = new Dictionary<string, string>();
            GetInitPathFromIni();
            mainWindow = this;
        }
        private async void GetInitPathFromIni()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // 组合完整配置文件路径
            string iniFilePath = Path.Combine(documentsPath, "Option_PA.ini");

            while(!File.Exists(iniFilePath))
            {
                await SaveInitPathToIni();
            }

            try
            {
                initPath = File.ReadAllText(iniFilePath);
                Debug.WriteLine($"initPath loaded from {iniFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load initPath from {iniFilePath}: {ex.Message}");
            }

            if(initPath != null)
            {
                EnsureDirectoriesExist(initPath);
                loadPackages();
                loadPlaysets();
                contentFrame.Navigate(typeof(Welcome));
            }
            else
            {
                GetInitPathFromIni();
            }
        }

        public async Task<bool> SaveInitPathToIni()
        {

            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            // 获取当前窗口的句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            initPath = folder.Path;

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // 组合完整配置文件路径
            string iniFilePath = Path.Combine(documentsPath, "Option_PA.ini");

            try
            {
                File.WriteAllText(iniFilePath, string.Empty); // 清除文件内容
                File.WriteAllText(iniFilePath, initPath); // 再写入新的内容
                Debug.WriteLine($"initPath saved to {iniFilePath}");
                GetInitPathFromIni();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save initPath to {iniFilePath}: {ex.Message}");
            }
            return false;
        }

        private void EnsureDirectoriesExist(string basePath)
        {
            string[] directories = { "packages", "playsets", "playground" };

            foreach (var dir in directories)
            {
                string fullPath = Path.Combine(basePath, dir);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }

            packagesPath = Path.Combine(basePath, "packages");
            playsetsPath = Path.Combine(basePath, "playsets");
            playgroundPath = Path.Combine(basePath, "playground");
        }

        private void loadPackages()
        {
            if (packagesPath == null)
            {
                Debug.WriteLine("packagesPath is null");
                return;
            }
            Debug.WriteLine("loadpackages");
            Debug.WriteLine(packagesPath);
            PackageFinder.FindPackages(packagesPath);
        }

        public void loadPlaysets()
        {
            if (playsetsPath == null||playsets == null)
            {
                Debug.WriteLine("playsetsPath is null");
                return;
            }
            palysets.MenuItems.Clear();

            AddPlayset_Button();

            Debug.WriteLine("loadplaysets");
            Debug.WriteLine(playsetsPath);
            var directories = Directory.GetDirectories(playsetsPath);

            foreach (var dir in directories)
            {
                string folderName = Path.GetFileName(dir);
                Debug.WriteLine(folderName);
                playsets[folderName] = dir;

                NavigationViewItem navItem = new NavigationViewItem
                {
                    Content = folderName,
                    Tag = dir
                };

                navItem.Tapped += NavItem_Tapped;

                palysets.MenuItems.Add(navItem);
            }
            NavBack();
        }

        private void NavItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is NavigationViewItem navItem && navItem.Tag is string playsetPath)
            {
                contentFrame.Navigate(typeof(PlaySet));
                var currentPage = contentFrame.Content;
                if (currentPage is PlaySet playsetPage)
                {
                    playsetPage.LoadPlaySet(playsetPath);
                }
            }
        }

        public void AddPlayset_Button()
        {
            NavigationViewItem newNavItem = new NavigationViewItem
            {
                Content = "Add Playset",
            };
            newNavItem.Icon = new SymbolIcon(Symbol.Add);
            newNavItem.Tapped += AddPlayset_Tapped;

            palysets.MenuItems.Add(newNavItem);
        }

        private void AddPlayset_Tapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var item in palysets.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Content.ToString() == "New PlaySet")
                {
                    return;
                }
            }

            NavigationViewItem newNavItem = new NavigationViewItem
            {
                Content = "New PlaySet",
                Tag = "null"
            };

            newNavItem.Tapped += NavItem_Tapped;

            palysets.MenuItems.Add(newNavItem);
        }

        public void NavBack()
        {
            contentFrame.Navigate(typeof(Welcome));
        }
    }
}
