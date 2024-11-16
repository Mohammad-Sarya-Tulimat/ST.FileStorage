using Azure.Storage.Blobs;
using ST.FileStorage.Abstractions;
using ST.FileStorage.Abstractions.Enum;
using ST.FileStorage.Abstractions.Exceptions;
using ST.FileStorage.AzureBlob.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace ST.FileStorage.AzureBlob
{
    public class AzureFileService : IFileService
    {
        private readonly AzureFileServiceOptions _options;
        BlobContainerClient _client = null;
        public AzureFileService(AzureFileServiceOptions options)
        {
            _options = options;
            var blobServiceClient = new BlobServiceClient(_options.ConnectionString);
            _client = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        } 
        private string ReName(string BlobName)
        {
            BlobName = PathHelper.FixDirectorySeparatorChar(BlobName);
            var fileName = Path.GetFileNameWithoutExtension(BlobName);
            var fileExtinision = Path.GetExtension(BlobName);
            var directory = PathHelper.GetDirectoryName(BlobName);
            var filePath = BlobName;
            if (_client.GetBlobClient(filePath).Exists())
            {
                filePath = PathHelper.Combine(directory, $"{fileName}({DateTime.UtcNow.ToString("dd-MM-yyyy")}){fileExtinision}");

                while (_client.GetBlobClient(filePath).Exists())
                {
                    filePath = PathHelper.Combine(directory, $"{fileName}({DateTime.UtcNow.ToString("dd-MM-yyyy")})({Guid.NewGuid()}){fileExtinision}");
                }
            }
            return filePath;
        }
        public async Task<string> Copy(string srcFile, string destFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {

            destFile = PathHelper.FixDirectorySeparatorChar(destFile);
            if (handlingType == FileExistsHandling.Rename) destFile = ReName(destFile);
            var sourceBlob = this._client.GetBlobClient(srcFile);
            if (await sourceBlob.ExistsAsync(cancellationToken))
            {
                var destBlob = _client.GetBlobClient(destFile);
                if (handlingType == FileExistsHandling.ThrowException && destBlob.Exists())
                    throw new FileServiceException($"Cannot copy the file{srcFile} because the file {destFile} already Exists");
                var copyInfo = await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);
                copyInfo.WaitForCompletion();
            }
            else throw new FileNotFoundException("Can not find file", srcFile);
            return destFile;
        }
        public async Task Delete(string filePath, CancellationToken cancellationToken = default)
        {
            var blob = this._client.GetBlobClient(filePath);
            if (await blob.ExistsAsync(cancellationToken))
            {
                var result = await blob.DeleteAsync(cancellationToken: cancellationToken);
            }
            else throw new FileNotFoundException("Can not file", filePath);
        }
        public void Dispose()
        {

        } 
        public async Task<bool> Exists(string path, CancellationToken cancellationToken)
        {
            var blob = this._client.GetBlobClient(path);
            var response = await blob.ExistsAsync(cancellationToken);
            return response.Value;
        }

        public Task<List<string>> GetFileList(string folder, string pattern = ".*", bool includeSubFolders = false, CancellationToken cancellationToken = default)
        {
            List<string> result = new List<string>();
            foreach (var name in PathHelper.GetFolderNames(folder))
            {
                var blobs = this._client.GetBlobs(prefix: name).ToList();
                foreach (var blob in blobs)
                {
                    if (!includeSubFolders && !PathHelper.IsParent(name, blob.Name))
                    {
                        continue;
                    }
                    if (Regex.IsMatch(blob.Name, pattern))
                        result.Add(blob.Name);
                }
            }
            return Task.FromResult(result.Distinct().ToList());
        }

        public async Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            folder = PathHelper.FixDirectorySeparatorChar(folder);
            var newNamepath = PathHelper.Combine(folder, Path.GetFileName(filePath));
            newNamepath = await this.Copy(filePath, newNamepath, handlingType, cancellationToken);
            await this.Delete(filePath, cancellationToken);
            return newNamepath;
        }
        public async Task<Stream> Read(string filePath, CancellationToken cancellationToken = default)
        {
            var blob = this._client.GetBlobClient(filePath);
            if (await blob.ExistsAsync(cancellationToken))
            {
                return await blob.OpenReadAsync(cancellationToken: cancellationToken);
            }
            else throw new FileNotFoundException("Can not find  file", filePath);
        }
        public async Task<string> Save(string folderPath, string fileName, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var filepath = PathHelper.Combine(folderPath, fileName);
            if (handlingType == FileExistsHandling.Rename) filepath = ReName(filepath);
            var blob = this._client.GetBlobClient(filepath);
            if (handlingType == FileExistsHandling.ThrowException && blob.Exists())
            {
                throw new FileServiceException($"the File {filepath} is exists");
            }
            await blob.UploadAsync(stream, handlingType == FileExistsHandling.Overwrite, cancellationToken: cancellationToken);
            return filepath;
        }
    }
}
