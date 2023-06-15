using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DirSyns
{
    internal class Program
    {
        static List<string> blackList = new List<string>() { };

        static string sourceDir;
        static string targetDir;
        static int sourceDirLen;
        static int targetDirLen;

        static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                sourceDir = args[0];
                targetDir = args[1];
                sourceDirLen = sourceDir.Length;
                targetDirLen = targetDir.Length;

                for (int i = 2; i < args.Length; i++)
                {
                    blackList.Add(args[i]);
                }

                Stopwatch stopwatch = new Stopwatch();

                Console.WriteLine($"Дублирование \"{sourceDir}\" в \"{targetDir}\"\n");

                bool err = false;
                if (err |= !Directory.Exists(sourceDir)) Console.WriteLine($"\"{sourceDir}\" не существует");
                if (err |= !Directory.Exists(targetDir)) Console.WriteLine($"\"{targetDir}\" не существует");

                if (err)
                {
                    Console.WriteLine($"\nДублирование \"{sourceDir}\" в \"{targetDir}\"");
                    Console.WriteLine($"\nОшибка!");
                }
                else
                {
                    stopwatch.Reset(); stopwatch.Start();

                    DoBackup(sourceDir, targetDir);

                    stopwatch.Stop();

                    Console.WriteLine($"\nДублирование \"{sourceDir}\" в \"{targetDir}\"");
                    Console.WriteLine($"\nУспех!  Выполнено за {FormatTime(stopwatch.Elapsed.TotalSeconds)}");
                }
            }
            else
            {
                Console.WriteLine("Формат команды: DirSyns.exe sourceDir targetDir blackListFolder [blackListFolder...]");
            }

            while (true) Console.ReadKey(true);
        }

        static void DoBackup(string curSourceDir, string curTargetDir)
        {
            // список файлов в исходной директории  
            var sourceDirFilesList = Directory.GetFiles(curSourceDir);
            for (int i = 0; i < sourceDirFilesList.Length; i++)
            {
                sourceDirFilesList[i] = sourceDirFilesList[i].Substring(sourceDirLen);
            }
            Array.Sort(sourceDirFilesList);

            // список файлов в целевой директории  
            var targetDirFilesList = Directory.GetFiles(curTargetDir);
            for (int i = 0; i < targetDirFilesList.Length; i++)
            {
                targetDirFilesList[i] = targetDirFilesList[i].Substring(targetDirLen);
            }
            Array.Sort(targetDirFilesList);

            //удаление файлов которых нет в исходной директории
            Parallel.ForEach(targetDirFilesList, targetDirItem =>
            {
                if (Array.BinarySearch(sourceDirFilesList, targetDirItem) < 0)
                {
                    if (File.Exists(targetDir + "\\" + targetDirItem))
                    {
                        try
                        {
                            //File.Delete(targetDir + "\\" + targetDirItem);
                            FileSystem.DeleteFile(targetDir + "\\" + targetDirItem, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            Console.WriteLine($"Deleted .\\{targetDirItem}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error   .\\{targetDirItem}");
                            Console.WriteLine($"        {e.ToString()}");
                        }
                    }
                }
            });

            // коприрование файлов которых нет в целевой директории либо они отличаются
            //foreach (var sourceDirItem in sourceDirFilesList)
            Parallel.ForEach(sourceDirFilesList, sourceDirItem =>
            {
                int index = Array.BinarySearch(targetDirFilesList, sourceDirItem);

                if (index >= 0)
                {
                    var item1FileInfo = new FileInfo(sourceDir + "\\" + sourceDirItem);
                    item1FileInfo.Refresh();
                    var item2FileInfo = new FileInfo(targetDir + "\\" + sourceDirItem);
                    item2FileInfo.Refresh();

                    if (item1FileInfo.LastWriteTime != item2FileInfo.LastWriteTime || item1FileInfo.Length != item2FileInfo.Length)
                    {
                        try
                        {
                            File.Copy(sourceDir + "\\" + sourceDirItem, targetDir + "\\" + sourceDirItem, true);
                            Console.WriteLine($"Copied  .\\{sourceDirItem}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error   .\\{sourceDirItem}");
                            Console.WriteLine($"        {e.ToString()}");
                        }
                    }
                }
                else
                {
                    try
                    {
                        File.Copy(sourceDir + "\\" + sourceDirItem, targetDir + "\\" + sourceDirItem, true);
                        Console.WriteLine($"Copied  .\\{sourceDirItem}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error   .\\{sourceDirItem}");
                        Console.WriteLine($"        {e.ToString()}");
                    }
                }
            }
            );


            // список папок в исходной директории              
            var sourceDirFoldersList = new List<string>();
            foreach (var item in Directory.GetDirectories(curSourceDir))
            {
                bool inBlackList = false;
                foreach (var blackListItem in blackList)
                {
                    if (item.EndsWith(blackListItem))
                    {
                        inBlackList = true;
                        break;
                    }
                }
                if (!inBlackList)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(item);
                    directoryInfo.Refresh();
                    if ((directoryInfo.Attributes & FileAttributes.System) == 0)
                    {
                        sourceDirFoldersList.Add(item.Replace(sourceDir, ""));
                    }
                }
            }

            // список папок в целевой директории  
            var targetDirFoldersList = new List<string>();
            foreach (var item in Directory.GetDirectories(curTargetDir))
            {
                bool inBlackList = false;
                foreach (var blackListItem in blackList)
                {
                    if (item.EndsWith(blackListItem))
                    {
                        inBlackList = true;
                        break;
                    }
                }
                if (!inBlackList)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(item);
                    directoryInfo.Refresh();
                    if ((directoryInfo.Attributes & FileAttributes.System) == 0)
                    {
                        targetDirFoldersList.Add(item.Replace(targetDir, ""));
                    }
                }
            }

            // список папок которые есть в обоих директориях 
            var subDirs = new List<string>();

            // коприрование папок которых нет в целевой директории 
            // а так же заполнение списка папок которые есть в обоих директориях 
            foreach (var sourceDirItem in sourceDirFoldersList)
            {
                bool isFind = false;
                foreach (var targetDirItem in targetDirFoldersList)
                {
                    if (sourceDirItem == targetDirItem)
                    {
                        subDirs.Add(sourceDirItem);

                        isFind = true;
                        break;
                    }
                }
                if (!isFind)
                {
                    CopyDirectory(sourceDirItem);
                }
            }

            // удаление папок которых нет в исходной директории 
            Parallel.ForEach(targetDirFoldersList, targetDirItem =>
            {
                bool isFind = false;
                foreach (var sourceDirItem in sourceDirFoldersList)
                {
                    if (sourceDirItem == targetDirItem)
                    {
                        isFind = true;
                        break;
                    }
                }
                if (!isFind)
                {
                    try
                    {
                        //DirectoryInfo directoryInfo = new DirectoryInfo(targetDir + "\\" + targetDirItem);
                        //directoryInfo.Refresh();
                        //directoryInfo.Attributes = FileAttributes.Normal;
                        //directoryInfo.Refresh();
                        FileSystem.DeleteDirectory(targetDir + "\\" + targetDirItem, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        //Directory.Delete(targetDir + "\\" + targetDirItem, true);
                        Console.WriteLine($"Deleted .\\{targetDirItem}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error   .\\{targetDirItem}");
                        Console.WriteLine($"        {e.ToString()}");
                    }
                }
            });

            // обработка папок которые есть в обоих директориях 
            Parallel.ForEach(subDirs, item =>
            {
                DoBackup(sourceDir + item, targetDir + item);
            }
            );
        }

        static void CopyDirectory(string qwe)
        {
            try
            {
                Directory.CreateDirectory(targetDir + qwe);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error   .\\{qwe}");
                Console.WriteLine($"        {e.ToString()}");
            }
            var filesList = Directory.GetFiles(sourceDir + qwe);
            foreach (string file in filesList)
            {
                try
                {
                    File.Copy(file, targetDir + file.Substring(sourceDirLen), true);
                    Console.WriteLine($"Copied  .\\{file.Substring(sourceDirLen)}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error   .\\{file.Substring(sourceDirLen)}");
                    Console.WriteLine($"        {e.ToString()}");
                }
            }

            var foldersList = Directory.GetDirectories(sourceDir + qwe);
            if (foldersList.Length == 0)
            {
                Console.WriteLine($"Copied  .\\{qwe}");
            }
            else
            {
                foreach (string folder in foldersList)
                {
                    CopyDirectory(folder.Substring(sourceDirLen));
                }
            }
        }

        static string FormatTime(double totalSeconds)
        {
            int intTotalSecond = (int)totalSeconds;
            int h = intTotalSecond / 3600;
            int m = (intTotalSecond - h * 3600) / 60;
            double s = totalSeconds - h * 3600 - m * 60;

            if (h != 0)
            {
                return $"{h} час {m:0} мин {s:0.000} сек";
            }
            else if (m != 0)
            {
                return $"{m:0} мин {s:0.000} сек";
            }
            else
            {
                return $"{s:0.000} сек";
            }
        }
    }
}
