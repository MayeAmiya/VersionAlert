using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Version_Alert
{
    public struct Package
    {
        public string Version { get; set; }
        public string Path { get; set; }
        public List<string> Remove { get; set; }
        public bool exist { get; set; }

        public Package(string Version, string Path)
        {
            this.Version = Version;
            this.Path = Path;
            this.Remove = new List<string>();
            this.exist = true;
        }
    }


    static public class PackageFinder
    {
        // 版本文件夹字典
        static public Dictionary<string, Package> Packages { get; private set; } = new Dictionary<string, Package>();

        // 构造函数，初始化文件夹路径和版本文件夹字典
        static PackageFinder(){}

        // 查找版本方法
        static public void FindPackages(string FolderPath)
        {
            Packages.Clear();
            Debug.WriteLine("findpackages");
            // 遍历文件夹路径下的所有子目录
            foreach (var dir in Directory.GetDirectories(FolderPath))
            {
                // 创建新的 Package 对象
                string dirname = Path.GetFileName(dir);
                Package newPackage = new Package(dirname, dir);
                string version = dirname;

                // 组合版本文件路径
                var packageFilePath = Path.Combine(dir, "Version");
                Debug.WriteLine("packageFile:");
                Debug.WriteLine(packageFilePath);

                // 如果版本文件存在
                if (File.Exists(packageFilePath))
                {

                    // 读取版本文件的所有行
                    var lines = File.ReadAllLines(packageFilePath);
                    // 遍历每一行
                    foreach (var line in lines)
                    {
                        // 如果行以 "[DTA]" 开头
                        if (line.StartsWith("Version")|| line.StartsWith("version"))
                        {
                            // 提取版本号并去除空格
                            version = line.Split('=')[1].Trim();
                            // 将版本号和目录添加到字典中
                            if (version != null)
                            {
                                newPackage.Version = version;
                                newPackage.Path = dir;
                            }
                            
                        }

                        if (line.StartsWith("delete"))
                        {
                            var remove = line.Split('=')[1].Trim();
                            newPackage.Remove.Add(remove);
                        }
                    }
                }

                if (version != null)
                {
                    Packages[version] = newPackage;
                }
            }
        }

        static public bool PackageExists(string packageName)
        {
            // 检查 PackageFolders 字典中是否包含指定的 packageName
            return Packages.ContainsKey(packageName);
        }

        static public string PackagePath(string packageName)
        {
            return Packages[packageName].Path;
        }
    }
}
