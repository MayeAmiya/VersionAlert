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
using System.Text.Json;
using System.Threading.Tasks;
using static Version_Alert.HardLink;
using static Version_Alert.MainWindow;
using System.Diagnostics;
using Windows.ApplicationModel;
using System.ComponentModel;
using Windows.UI;
using System.Diagnostics.CodeAnalysis;
using Windows.ApplicationModel.DataTransfer;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Windows.Storage.Pickers;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Version_Alert
{
    /// <summary>
    /// 一个空白页面，可以单独使用，也可以在 Frame 内导航到该页面。
    /// </summary>
    public struct playsetData
    {
        public string playsetName = "New PlaySet";
        public List<Package> selected = new List<Package>();
        public string targetExecutable = string.Empty;
        public playsetData(string name, List<Package> selected, string exec)
        {
            packagesPath = name;
            this.selected = selected;
            targetExecutable = exec;
        }
    }

    public sealed partial class PlaySet : Page
    {
        public PlaySet()
        {
            this.InitializeComponent();
            // 初始化 packages 列表
            playsetName = "New PlaySet";
            selected = new List<Package>();
            targetExecutable = string.Empty;

            ShowPackages();
            ShowSelectPackages();
        }

        public List<Package> selected;
        public string targetExecutable { get; set; }
        public string? playsetName { get; set; }
        public string? playsetNameTemp { get; set; }

        public void LoadPlaySet(string jsonPath)
        {
            if (jsonPath == "null")
            {
                return;
            }

            // 添加 /playset.json 到 jsonPath
            jsonPath = Path.Combine(jsonPath, "playset.json");

            // 读取 JSON 文件内容
            string json = File.ReadAllText(jsonPath);
            Debug.WriteLine(json);
            // 反序列化
            var playSet = JsonConvert.DeserializeObject<PlaySet>(json);

            // 检查 playSet 和 playSet.packages 是否为 null
            if (playSet != null)
            {
                if (playSet.playsetName != null)
                {
                    playsetName = playSet.playsetName;
                    playsetNameTemp = playSet.playsetName;
                }

                if (playSet.targetExecutable != null)
                {
                    targetExecutable = playSet.targetExecutable;
                }

                if (playSet.selected != null)
                {
                    // 遍历 packages 列表
                    foreach (var package in playSet.selected)
                    {
                        // 创建一个临时变量来存储 package
                        var tempPackage = package;
                        // 检查 package 是否存在
                        if (PackageFinder.PackageExists(tempPackage.Version))
                        {
                            tempPackage.exist = true;
                        }
                        else
                        {
                            tempPackage.exist = false;
                        }
                        // 添加 package 到 packages 列表
                        selected.Add(tempPackage);

                    }
                    ShowSelectPackages();
                }
            }
        }

        public void PlaySet_Save(object sender, RoutedEventArgs e)
        {
            // 序列化 packages 列表
            // 读取 JSON 文件
            if (playsetNameTemp == null || targetExecutable == null)
            {
                return;
            }
            
            playsetData playsetData = new(this.playsetNameTemp, this.selected, this.targetExecutable);
            var json = JsonConvert.SerializeObject(playsetData);

            // 保存 JSON 文件

            if (MainWindow.playsetsPath == null || playsetNameTemp == null)
            {
                return;
            }

            // 获取playset对应的文件夹路径
            var playsetFolderPath = Path.Combine(MainWindow.playsetsPath, playsetNameTemp);
            // 确保playset文件夹存在
            if (!Directory.Exists(playsetFolderPath))
            {
                Directory.CreateDirectory(playsetFolderPath);
            }

            var jsonPath = Path.Combine(playsetFolderPath, "playset.json");
            if (!File.Exists(jsonPath))
            {
                using (File.Create(jsonPath)) { }
            }
            File.WriteAllText(jsonPath, json);

            // 确保playset文件夹存在
            if (!Directory.Exists(playsetFolderPath))
            {
                Directory.CreateDirectory(playsetFolderPath);
            }

            // 定义要保存的文件夹
            var foldersToSave = new List<string> { "save", "Maps" };

            if (MainWindow.playgroundPath == null)
            {
                return;
            }

            foreach (var folder in foldersToSave)
            {
                var sourceFolderPath = Path.Combine(MainWindow.playgroundPath, folder);
                var destinationFolderPath = Path.Combine(playsetFolderPath, folder);

                if (Directory.Exists(sourceFolderPath))
                {
                    // 复制文件夹及其内容
                    CopyDirectory(sourceFolderPath, destinationFolderPath);
                }
            }

            var ResourcesFolderPath = Path.Combine(MainWindow.playgroundPath, "Resources");
            var destinationResourcesFolderPath = Path.Combine(playsetFolderPath, "Resources");
            if (Directory.Exists(ResourcesFolderPath))
            {
                // 确保目标文件夹存在
                if (!Directory.Exists(destinationResourcesFolderPath))
                {
                    Directory.CreateDirectory(destinationResourcesFolderPath);
                }

                // 复制ini文件
                foreach (var file in Directory.GetFiles(ResourcesFolderPath, "*.ini"))
                {
                    var destFile = Path.Combine(destinationResourcesFolderPath, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
            }
            MainWindow.mainWindow.loadPlaysets();
        }

        public void PlaySet_Clear(object sender, RoutedEventArgs e)
        {
            if (MainWindow.playgroundPath == null)
            {
                return;
            }

            if (Directory.Exists(MainWindow.playgroundPath))
            {
                var files = Directory.GetFiles(MainWindow.playgroundPath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                var directories = Directory.GetDirectories(MainWindow.playgroundPath);
                foreach (var directory in directories)
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        public void PlaySet_Init(object sender, RoutedEventArgs e)
        {
            OrderSelectedPackages();
            if (MainWindow.playgroundPath == null || MainWindow.playsetsPath == null)
            {
                return;
            }
            // 遍历 packages 列表  
            foreach (var package in selected)
            {
                // 打印 package 的 Path  
                Version_Alert.HardLink.CreateHardLinks(package.Path, MainWindow.playgroundPath);

                // 在 playground 中查找并删除 package.remove 中的文件  
                foreach (var removeFile in package.Remove)
                {
                    var filePath = Path.Combine(MainWindow.playgroundPath, removeFile);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            // 为 playset 创建硬链接
            Version_Alert.HardLink.CreateHardLinks(MainWindow.playsetsPath, MainWindow.playgroundPath);
            
        }

        public async void PlaySet_Game(object sender, RoutedEventArgs e)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            // 获取当前应用的窗口
            var window = (Application.Current as App)?.m_window;

            if (window == null)
            {
                return;
            }

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your file picker
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add(".exe");

            // Open the picker for the user to pick a file
            var file = await openPicker.PickSingleFileAsync();
            Debug.WriteLine(file.Name);
            if(file == null)
            {
                return;
            }
            // 获取文件名（不包含前缀路径）
            targetExecutable = file.Name;
        }

        public void PlaySet_Start(object sender, RoutedEventArgs e)
        {
            PlaySet_Clear(sender, e);
            PlaySet_Init(sender, e);
            if (MainWindow.playgroundPath == null)
            {
                return;
            }
            if (targetExecutable == null)
            {
                return;
            }

            string TargetExecutablePath = Path.Combine(MainWindow.playgroundPath, targetExecutable);

            Debug.WriteLine(TargetExecutablePath);

            if (!string.IsNullOrEmpty(TargetExecutablePath))
            {
                var processInfo = new ProcessStartInfo(TargetExecutablePath)
                {
                    UseShellExecute = true,
                    Verb = "runas", // 以管理员身份运行
                    WorkingDirectory = MainWindow.playgroundPath // 设置工作目录
                };

                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception ex)
                {
                    // 处理启动失败的情况
                    Debug.WriteLine($"启动失败: {ex.Message}");
                }
            }
            return;
        }

        public void PlaySet_Delete(object sender, RoutedEventArgs e)
        {
            if (MainWindow.playsetsPath == null || playsetName == null)
            {
                return;
            }
            // 获取 playset 文件夹路径
            var playsetFolderPath = Path.Combine(MainWindow.playsetsPath, playsetName);
            // 删除 playset 文件夹
            if (Directory.Exists(playsetFolderPath))
            {
                Directory.Delete(playsetFolderPath, true);
            }
            MainWindow.mainWindow.loadPlaysets();
        }

        public void ShowPackages()
        {
            // 清空现有的 ListView 条目
            PackagesList.Items.Clear();

            // 遍历 packages 列表
            foreach (var package in PackageFinder.Packages)
            {
                // 创建一个新的 ListViewItem
                Debug.WriteLine(package.Key);
                var item = new ListViewItem
                {
                    Content = $"{package.Key}"
                    
                };

                // 将 ListViewItem 添加到 ListView
                PackagesList.Items.Add(item);
            }
        }

        public void PackagesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (PackagesList.SelectedItem is ListViewItem selectedItem)
            {
                var packageKey = selectedItem.Content.ToString();

                if (packageKey == null)
                {
                    return;
                }

                if (PackageFinder.Packages.TryGetValue(packageKey, out var package))
                {
                    selected.Add(package);
                }
                // 创建一个新的 ListViewItem  
                var item = new ListViewItem
                {
                    Content = $"{package.Version}"
                };
                // 将 ListViewItem 添加到 ListView  
                SelectedList.Items.Add(item);
            }
        }

        public void ShowSelectPackages()
        {
            foreach (var package in selected)
            {
                // 创建一个新的 ListViewItem  
                var item = new ListViewItem
                {
                    Content = $"{package.Version}"
                };

                // 如果 package.exist 为 false，则设置红色背景  
                if (!package.exist)
                {
                    item.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
                }
                // Replace the line causing the error in ShowSelectPackages method

                // 将 ListViewItem 添加到 ListView  
                SelectedList.Items.Add(item);
            }
        }

        public void SelectedList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (SelectedList.SelectedItem is ListViewItem selectedItem)
            {
                // 从 selected 列表中移除对应的 package
                var packageVersion = selectedItem.Content.ToString();
                var packageToRemove = selected.FirstOrDefault(p => p.Version == packageVersion);
                selected.Remove(packageToRemove);
                // 移除该 ListViewItem
                SelectedList.Items.Remove(selectedItem);
            }
        }

        public void OrderSelectedPackages()
        {
            selected = selected.OrderBy(p => SelectedList.Items.IndexOf(SelectedList.Items.Cast<ListViewItem>().FirstOrDefault(item => item.Content.ToString() == p.Version))).ToList();
        }

        public async void SetFolder(object sender, RoutedEventArgs e)
        {
            await MainWindow.mainWindow.SaveInitPathToIni();
        }

        public void OpenFolder(object sender, RoutedEventArgs e)
        {
            if (MainWindow.initPath == null)
            {
                return;
            }
            // 打开文件夹
            Process.Start("explorer.exe", MainWindow.initPath);
        }
    }
}
