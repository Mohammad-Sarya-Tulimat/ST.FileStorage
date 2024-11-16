using ST.FileStorage.Abstractions;
using ST.FileStorage.Abstractions.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ST.FileStorage.GoogleCloudStorage
{
    public class GoogleCloudStorageService : IFileService
    {
        public Task<string> Copy(string srcFile, string destFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Delete(string filePath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetFileList(string folder, string pattern = ".*", bool includeSubFolders = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> Read(string filePath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> Save(string folderPath, string fileName, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
