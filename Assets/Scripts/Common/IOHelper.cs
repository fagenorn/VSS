using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Common
{
    internal static class IOHelper
    {
        public static List<FileInfo> GetFilesRegexSearch(string path, string regex, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var files = new List<FileInfo>();
            var directory = new DirectoryInfo(path);
            if (!directory.Exists) return files;

            var allFiles = directory.GetFiles("*.*", searchOption);
            foreach (var file in allFiles)
            {
                if (Regex.IsMatch(file.Name, regex)) { files.Add(file); }
            }

            return files;
        }

        public static async Task<Texture2D> LoadImage(string path)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture($"file://{path}"))
            {
                var download = (DownloadHandlerTexture) uwr.downloadHandler;
                var response = await uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success) throw new System.Exception($"Image not found: {uwr.error}");

                return download.texture;
            }
        }

        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
    }
}
