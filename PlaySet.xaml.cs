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
    /// һ���հ�ҳ�棬���Ե���ʹ�ã�Ҳ������ Frame �ڵ�������ҳ�档
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
            // ��ʼ�� packages �б�
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

            // ��� /playset.json �� jsonPath
            jsonPath = Path.Combine(jsonPath, "playset.json");

            // ��ȡ JSON �ļ�����
            string json = File.ReadAllText(jsonPath);
            Debug.WriteLine(json);
            // �����л�
            var playSet = JsonConvert.DeserializeObject<PlaySet>(json);

            // ��� playSet �� playSet.packages �Ƿ�Ϊ null
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
                    // ���� packages �б�
                    foreach (var package in playSet.selected)
                    {
                        // ����һ����ʱ�������洢 package
                        var tempPackage = package;
                        // ��� package �Ƿ����
                        if (PackageFinder.PackageExists(tempPackage.Version))
                        {
                            tempPackage.exist = true;
                        }
                        else
                        {
                            tempPackage.exist = false;
                        }
                        // ��� package �� packages �б�
                        selected.Add(tempPackage);

                    }
                    ShowSelectPackages();
                }
            }
        }

        public void PlaySet_Save(object sender, RoutedEventArgs e)
        {
            // ���л� packages �б�
            // ��ȡ JSON �ļ�
            if (playsetNameTemp == null || targetExecutable == null)
            {
                return;
            }
            
            playsetData playsetData = new(this.playsetNameTemp, this.selected, this.targetExecutable);
            var json = JsonConvert.SerializeObject(playsetData);

            // ���� JSON �ļ�

            if (MainWindow.playsetsPath == null || playsetNameTemp == null)
            {
                return;
            }

            // ��ȡplayset��Ӧ���ļ���·��
            var playsetFolderPath = Path.Combine(MainWindow.playsetsPath, playsetNameTemp);
            // ȷ��playset�ļ��д���
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

            // ȷ��playset�ļ��д���
            if (!Directory.Exists(playsetFolderPath))
            {
                Directory.CreateDirectory(playsetFolderPath);
            }

            // ����Ҫ������ļ���
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
                    // �����ļ��м�������
                    CopyDirectory(sourceFolderPath, destinationFolderPath);
                }
            }

            var ResourcesFolderPath = Path.Combine(MainWindow.playgroundPath, "Resources");
            var destinationResourcesFolderPath = Path.Combine(playsetFolderPath, "Resources");
            if (Directory.Exists(ResourcesFolderPath))
            {
                // ȷ��Ŀ���ļ��д���
                if (!Directory.Exists(destinationResourcesFolderPath))
                {
                    Directory.CreateDirectory(destinationResourcesFolderPath);
                }

                // ����ini�ļ�
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
            // ���� packages �б�  
            foreach (var package in selected)
            {
                // ��ӡ package �� Path  
                Version_Alert.HardLink.CreateHardLinks(package.Path, MainWindow.playgroundPath);

                // �� playground �в��Ҳ�ɾ�� package.remove �е��ļ�  
                foreach (var removeFile in package.Remove)
                {
                    var filePath = Path.Combine(MainWindow.playgroundPath, removeFile);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            // Ϊ playset ����Ӳ����
            Version_Alert.HardLink.CreateHardLinks(MainWindow.playsetsPath, MainWindow.playgroundPath);
            
        }

        public async void PlaySet_Game(object sender, RoutedEventArgs e)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            // ��ȡ��ǰӦ�õĴ���
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
            // ��ȡ�ļ�����������ǰ׺·����
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
                    Verb = "runas", // �Թ���Ա�������
                    WorkingDirectory = MainWindow.playgroundPath // ���ù���Ŀ¼
                };

                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception ex)
                {
                    // ��������ʧ�ܵ����
                    Debug.WriteLine($"����ʧ��: {ex.Message}");
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
            // ��ȡ playset �ļ���·��
            var playsetFolderPath = Path.Combine(MainWindow.playsetsPath, playsetName);
            // ɾ�� playset �ļ���
            if (Directory.Exists(playsetFolderPath))
            {
                Directory.Delete(playsetFolderPath, true);
            }
            MainWindow.mainWindow.loadPlaysets();
        }

        public void ShowPackages()
        {
            // ������е� ListView ��Ŀ
            PackagesList.Items.Clear();

            // ���� packages �б�
            foreach (var package in PackageFinder.Packages)
            {
                // ����һ���µ� ListViewItem
                Debug.WriteLine(package.Key);
                var item = new ListViewItem
                {
                    Content = $"{package.Key}"
                    
                };

                // �� ListViewItem ��ӵ� ListView
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
                // ����һ���µ� ListViewItem  
                var item = new ListViewItem
                {
                    Content = $"{package.Version}"
                };
                // �� ListViewItem ��ӵ� ListView  
                SelectedList.Items.Add(item);
            }
        }

        public void ShowSelectPackages()
        {
            foreach (var package in selected)
            {
                // ����һ���µ� ListViewItem  
                var item = new ListViewItem
                {
                    Content = $"{package.Version}"
                };

                // ��� package.exist Ϊ false�������ú�ɫ����  
                if (!package.exist)
                {
                    item.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
                }
                // Replace the line causing the error in ShowSelectPackages method

                // �� ListViewItem ��ӵ� ListView  
                SelectedList.Items.Add(item);
            }
        }

        public void SelectedList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (SelectedList.SelectedItem is ListViewItem selectedItem)
            {
                // �� selected �б����Ƴ���Ӧ�� package
                var packageVersion = selectedItem.Content.ToString();
                var packageToRemove = selected.FirstOrDefault(p => p.Version == packageVersion);
                selected.Remove(packageToRemove);
                // �Ƴ��� ListViewItem
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
            // ���ļ���
            Process.Start("explorer.exe", MainWindow.initPath);
        }
    }
}
