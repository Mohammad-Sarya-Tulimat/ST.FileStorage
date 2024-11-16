
using ST.FileStorage.Abstractions;
using ST.FileStorage.Abstractions.Enum;
using ST.FileStorage.LocalStorage.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace ST.FileStorage.LocalStorage
{
    public class LocalFileService : IFileService
    {
        private readonly LocalFileOptions _localFileOptions;
        public LocalFileService(LocalFileOptions localFileOptions)
        {
            _localFileOptions = localFileOptions;
        }
        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");
            fromPath = PathHelper.FixDirectorySeparatorChar(fromPath);
            toPath = PathHelper.FixDirectorySeparatorChar(toPath);
            if (toPath.StartsWith(fromPath))
            {
                toPath = toPath.Remove(0, fromPath.Length);
                if (toPath.StartsWith(Path.AltDirectorySeparatorChar + ""))
                {
                    toPath = toPath.Remove(0, 1);
                }
            }
            return toPath;
        }
        private string GetFullPath(string path)
        {
            return PathHelper.Combine(_localFileOptions.RootFolder, path);
        }
        private string GetRelativePath(string path)
        {
            return MakeRelativePath(_localFileOptions.RootFolder, path);
        }

        public Task<string> GetEncoding(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
             {
                 using (StreamReader file = new StreamReader(GetFullPath(filePath)))
                 {
                     file.Peek();
                     return file.CurrentEncoding.WebName;
                 }
             });
        }
        public Task Delete(string filePath, CancellationToken cancellationToken = default)
        {
            File.Delete(this.GetFullPath(filePath));
            return Task.CompletedTask;
        }
        private List<string> localGetFileList(string folder, string pattern = @".*", bool includeSubFolders = false)
        {
            if (!Directory.Exists(folder))
                return new List<string>();
            List<string> fileList = new List<string>();
            var directory = new DirectoryInfo(folder);
            fileList.AddRange(directory.GetFiles().Where(s => Regex.IsMatch(s.FullName, pattern)).Select(s => s.FullName).ToList());
            if (includeSubFolders)
            {
                foreach (var subFolder in directory.GetDirectories())
                {
                    var SubFiles = this.localGetFileList(subFolder.FullName, pattern, includeSubFolders);
                    fileList.AddRange(SubFiles);
                }
            }
            return fileList.Distinct().ToList();
        }
        public Task<List<string>> GetFileList(string folder, string pattern = @".*", bool includeSubFolders = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.localGetFileList(this.GetFullPath(folder), pattern, includeSubFolders).Select(s => this.GetRelativePath(s)).ToList());
        }
        public Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var localFilePath = this.GetFullPath(filePath);
            var localFolder = this.GetFullPath(folder);
            if (!Directory.Exists(localFolder)) Directory.CreateDirectory(localFolder);
            var fileInfo = new FileInfo(localFilePath);
            var newFileName = PathHelper.Combine(localFolder, fileInfo.Name);
            if (handlingType == FileExistsHandling.Rename) newFileName = this.ReName(localFolder, fileInfo.Name);

            if (handlingType == FileExistsHandling.Overwrite && File.Exists(newFileName))
            {
                File.Delete(newFileName);
            }
            File.Move(localFilePath, newFileName);

            return Task.FromResult(this.GetRelativePath(newFileName));
        }
        public Task<Stream> Read(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(File.OpenRead(this.GetFullPath(filePath)) as Stream);
        }
        private string ReName(string localFolder, string name)
        {
            var fileName = Path.GetFileNameWithoutExtension(name);
            var fileExtinision = Path.GetExtension(name);
            var filePath = PathHelper.Combine(localFolder, $"{fileName}{fileExtinision}");
            if (File.Exists(filePath))
            {
                filePath = PathHelper.Combine(localFolder, $"{fileName}({DateTime.UtcNow.ToString("dd-MM-yyyy")}){fileExtinision}");
                while (File.Exists(filePath))
                {
                    filePath = PathHelper.Combine(localFolder, $"{fileName}({DateTime.UtcNow.ToString("dd-MM-yyyy")})({Guid.NewGuid()}){fileExtinision}");
                }
            }
            return filePath;
        }
        public Task<string> Save(string folderPath, string name, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var localFolder = this.GetFullPath(folderPath);
            if (!Directory.Exists(localFolder)) Directory.CreateDirectory(localFolder);
            var filename = PathHelper.Combine(localFolder, name);
            if (handlingType == FileExistsHandling.Rename) filename = this.ReName(localFolder, name);
            using (var file = File.Open(filename, handlingType == FileExistsHandling.Overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write))
            {
                stream.Position = 0;
                stream.CopyTo(file);
                file.Flush();
            }
            return Task.FromResult(this.GetRelativePath(filename));
        }
        public Task<string> Copy(string srcFile, string distFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var surcefile = this.GetFullPath(srcFile);
            var destinationfile = new FileInfo(this.GetFullPath(distFile));
            if (!Directory.Exists(destinationfile.Directory.FullName)) Directory.CreateDirectory(destinationfile.Directory.FullName);
            if (handlingType == FileExistsHandling.Rename) destinationfile = new FileInfo(this.ReName(destinationfile.Directory.FullName, destinationfile.Name));
            File.Copy(surcefile, destinationfile.FullName);
            return Task.FromResult(this.GetRelativePath(destinationfile.FullName));
        }

        public void Dispose()
        {

        }
    }
}
