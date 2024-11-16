
using ST.FileStorage.Abstractions;
using ST.FileStorage.Abstractions.Enum;
using ST.FileStorage.Abstractions.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ST.FileStorage.InMemory
{
    /// <summary>
    /// this service use Dictionary&lt;string, MemoryStream&gt; to store streams.<br/>
    /// <b>use it for unitTest only </b>
    /// </summary> 
    public class InMemoryFileService : IFileService
    {
        static InMemoryFileService memoryFileService;
        public static InMemoryFileService Get()
        {
            if (memoryFileService == null)
                memoryFileService = new InMemoryFileService();
            return memoryFileService;
        }


        private string ReName(string BlobName)
        {
            BlobName = PathHelper.FixDirectorySeparatorChar(BlobName);
            var fileName = Path.GetFileNameWithoutExtension(BlobName);
            var fileExtinision = Path.GetExtension(BlobName);
            var directory = PathHelper.GetDirectoryName(BlobName);
            var filePath = BlobName;
            if (_files.ContainsKey(filePath))
            {
                filePath = PathHelper.Combine(directory, $"{fileName}({DateTime.UtcNow.ToString("dd-MM-yyyy")}){fileExtinision}");

                while (_files.ContainsKey(filePath))
                {
                    filePath = PathHelper.Combine(directory, $"{fileName}({DateTime.UtcNow.ToString("dd-MM-yyyy")})({Guid.NewGuid()}){fileExtinision}");
                }
            }
            return filePath;
        }

        private readonly Dictionary<string, MemoryStream> _files;

        private InMemoryFileService() { _files = new Dictionary<string, MemoryStream>(); }

        public async Task<string> Copy(string srcFile, string destFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            destFile = PathHelper.FixDirectorySeparatorChar(destFile);
            if (handlingType == FileExistsHandling.Rename) destFile = ReName(destFile);
            if (_files.ContainsKey(srcFile))
            {
                if (handlingType == FileExistsHandling.ThrowException && _files.ContainsKey(destFile))
                    throw new FileServiceException($"Cannot copy the file{srcFile} because the file {destFile} already Exists");
                _files[destFile] = new MemoryStream();
                var sourceStream = _files[srcFile];
                if (sourceStream.CanSeek)
                    sourceStream.Position = 0;
                sourceStream.CopyTo(_files[destFile]);
            }
            else throw new FileNotFoundException("Can not find file", srcFile);
            return destFile;
        }

        public Task Delete(string filePath, CancellationToken cancellationToken = default)
        {
            if (_files.ContainsKey(filePath))
            {
                _files[filePath].Dispose();
                _files.Remove(filePath);
            }
            else throw new FileNotFoundException("Can not file", filePath);
            return Task.CompletedTask;
        }

        public void Dispose()
        {

        }
        public Task<List<string>> GetFileList(string folder, string pattern = ".*", bool includeSubFolders = false, CancellationToken cancellationToken = default)
        {

            List<string> result = new List<string>();
            foreach (var name in PathHelper.GetFolderNames(folder))
            {
                var blobs = this._files.Where(s => s.Key.StartsWith(name)).ToList();
                foreach (var blob in blobs)
                {
                    if (!includeSubFolders && !PathHelper.IsParent(name, blob.Key))
                    {
                        continue;
                    }
                    if (Regex.IsMatch(blob.Key, pattern))
                        result.Add(blob.Key);
                }
            }
            return Task.FromResult(result.Distinct().ToList());

        }

        public async Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            folder = PathHelper.FixDirectorySeparatorChar(folder);
            var destFile = PathHelper.Combine(folder, Path.GetFileName(filePath));
            destFile = PathHelper.FixDirectorySeparatorChar(destFile);
            if (handlingType == FileExistsHandling.Rename) destFile = ReName(destFile);
            if (_files.ContainsKey(filePath))
            {
                if (handlingType == FileExistsHandling.ThrowException && _files.ContainsKey(destFile))
                    throw new FileServiceException($"Cannot move the file{filePath} because the file {destFile} already Exists");
                _files[destFile] = _files[filePath];
                _files.Remove(filePath);
            }
            else throw new FileNotFoundException("Can not find file", filePath);
            return destFile;
        }

        public async Task<Stream> Read(string filePath, CancellationToken cancellationToken = default)
        {
            if (_files.TryGetValue(filePath, out var stream))
            {
                var result = new MemoryStream();
                if (stream.CanSeek) stream.Position = 0;
                await stream.CopyToAsync(result);
                return result;
            }
            throw new FileNotFoundException($"Cannot find the file", filePath);
        }

        public async Task<string> Save(string folderPath, string fileName, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var filepath = PathHelper.Combine(folderPath, fileName);
            if (handlingType == FileExistsHandling.Rename) filepath = ReName(filepath);
            if (handlingType == FileExistsHandling.ThrowException && _files.ContainsKey(filepath))
            {
                throw new FileServiceException($"the File {filepath} is exists");
            }
            var newStram = new MemoryStream();
            if (stream.CanSeek) stream.Position = 0;
            await stream.CopyToAsync(newStram);
            _files[filepath] = newStram;
            return filepath;
        }
    }
}
