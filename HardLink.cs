using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Version_Alert
{
    class HardLink
    {
        public static void CreateHardLinks(string sourcePath, string targetPath)
        {
            string sourceFull = Path.GetFullPath(sourcePath);
            string targetFull = Path.GetFullPath(targetPath);

            if (!Directory.Exists(sourceFull))
                throw new DirectoryNotFoundException($"源目录不存在: {sourceFull}");

            Directory.CreateDirectory(targetFull);

            foreach (var sourceItem in Directory.EnumerateFileSystemEntries(sourceFull, "*", SearchOption.AllDirectories))
            {
                try
                {
                    string relativePath = sourceItem.Substring(sourceFull.Length).TrimStart(Path.DirectorySeparatorChar);
                    string targetItem = Path.Combine(targetFull, relativePath);

                    if (Directory.Exists(sourceItem))
                    {
                        Directory.CreateDirectory(targetItem);
                    }
                    else
                    {
                        string targetDir = Path.GetDirectoryName(targetItem);
                        if (!Directory.Exists(targetDir))
                            Directory.CreateDirectory(targetDir);

                        if (File.Exists(targetItem))
                        {
                            try
                            {
                                File.Delete(targetItem);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"删除文件失败 {targetItem}: {ex.Message}");
                                continue;
                            }
                        }

                        if (!CreateHardLink(targetItem, sourceItem, IntPtr.Zero))
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            throw new Win32Exception(errorCode, $"创建硬链接失败: '{sourceItem}' 到 '{targetItem}'");
                        }
                        Debug.WriteLine(targetItem);

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"处理 {sourceItem} 时出错: {ex.Message}");
                }
            }
        }

        public static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destFile, overwrite: true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"复制文件失败 {file} 到 {destFile}: {ex.Message}");
                }
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}