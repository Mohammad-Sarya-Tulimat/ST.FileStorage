using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ST.FileStorage.Abstractions;
using ST.FileStorage.Abstractions.Enum;
using ST.FileStorage.Abstractions.Exceptions;
using ST.FileStorage.GoogleDrive.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ST.FileStorage.GoogleDrive
{
    public class GoogleDriveService : IFileService
    {
        DriveService _driveService;
        GoogleDriveOptions _googleDriveOptions;
        private async Task<DriveService> GetDriveServiceAsync(CancellationToken cancellationToken = default)
        {
            if (_driveService != null) return _driveService;
            UserCredential _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(_googleDriveOptions.ClientSecret, new[] { DriveService.Scope.DriveFile }, _googleDriveOptions.User, cancellationToken, _googleDriveOptions.TokensStorage);
            _driveService = new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = _credential, ApplicationName = _googleDriveOptions.ApplicationName });
            return _driveService;
        }

        public GoogleDriveService(GoogleDriveOptions googleDriveOptions)
        {
            _googleDriveOptions = googleDriveOptions;
        }
        public async Task<string> Copy(string srcFile, string destFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            destFile = PathHelper.FixDirectorySeparatorChar(destFile);
            if (handlingType == FileExistsHandling.Rename) destFile = ReName(destFile);
            var ids = await this.GetIdsAsync(srcFile, cancellationToken);
            if (ids.Any())
            {
                if (handlingType == FileExistsHandling.ThrowException && await this.ExistsAsync(destFile, cancellationToken))
                    throw new FileServiceException($"Cannot copy the file{srcFile} because the file {destFile} already Exists");
                var service = await this.GetDriveServiceAsync(cancellationToken);

                var fileMetadata = new Google.Apis.Drive.v3.Data.File() { Name = destFile };
                await service.Files.Copy(fileMetadata, ids.FirstOrDefault()).ExecuteAsync();
            }
            else throw new FileNotFoundException("Can not find file", srcFile);
            return destFile;
        }

        public async Task Delete(string filePath, CancellationToken cancellationToken = default)
        {
            var ids = await this.GetIdsAsync(filePath, cancellationToken);
            if (ids.Count == 0) throw new FileNotFoundException("Can not file", filePath);
            var service = await this.GetDriveServiceAsync(cancellationToken);
            foreach (var id in ids)
                await service.Files.Delete(id).ExecuteAsync();
        }

        public void Dispose()
        {
            _driveService?.Dispose();
        }

        public async Task<List<string>> GetFileList(string folder, string pattern = ".*", bool includeSubFolders = false, CancellationToken cancellationToken = default)
        {
            List<string> result = new List<string>();
            var service = await this.GetDriveServiceAsync(cancellationToken);
            foreach (var name in PathHelper.GetFolderNames(folder))
            {
                FilesResource.ListRequest listRequest = service.Files.List();
                listRequest.Q = $"name contains '{folder}*'";
                listRequest.Fields = "files(id, name)";
                var blobs = await listRequest.ExecuteAsync();
                foreach (var blob in blobs.Files)
                {
                    if (!includeSubFolders && !PathHelper.IsParent(name, blob.Name))
                    {
                        continue;
                    }
                    if (Regex.IsMatch(blob.Name, pattern))
                        result.Add(blob.Name);
                }
            }
            return result.Distinct().ToList();
        }

        public async Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            folder = PathHelper.FixDirectorySeparatorChar(folder);
            var destFile = PathHelper.Combine(folder, Path.GetFileName(filePath));
            destFile = PathHelper.FixDirectorySeparatorChar(destFile);
            if (handlingType == FileExistsHandling.Rename) destFile = ReName(destFile);
            var ids = await this.GetIdsAsync(filePath, cancellationToken);
            if (ids.Any())
            {
                if (handlingType == FileExistsHandling.ThrowException && await this.ExistsAsync(destFile, cancellationToken))
                    throw new FileServiceException($"Cannot move the file{filePath} because the file {destFile} already Exists");
                var service = await this.GetDriveServiceAsync(cancellationToken);
                var fileMetadata = new Google.Apis.Drive.v3.Data.File() { Name = destFile };
                await service.Files.Update(fileMetadata, ids.FirstOrDefault()).ExecuteAsync();
            }
            else throw new FileNotFoundException("Can not find file", filePath);

            return destFile;
        }

        public async Task<Stream> Read(string filePath, CancellationToken cancellationToken = default)
        {
            var ids = await this.GetIdsAsync(filePath, cancellationToken);
            if (ids.Count == 0) throw new FileNotFoundException("Can not file", filePath);
            var service = await this.GetDriveServiceAsync(cancellationToken);
            var id = ids.FirstOrDefault();
            var request = service.Files.Get(id);
            if (_googleDriveOptions.UsePipeStreams)
                return this.GetPipeStream(request, cancellationToken);
            else return await this.GetMemoryStream(request, cancellationToken);

        }
        public async Task<string> Save(string folderPath, string fileName, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var filepath = PathHelper.Combine(folderPath, fileName);
            if (handlingType == FileExistsHandling.Rename) filepath = ReName(filepath);
            if (handlingType == FileExistsHandling.ThrowException && await this.ExistsAsync(filepath, cancellationToken))
            {
                throw new FileServiceException($"the File {filepath} is exists");
            }
            var service = await this.GetDriveServiceAsync(cancellationToken);
            var fileMetadata = new Google.Apis.Drive.v3.Data.File() { Name = filepath };
            await service.Files.Create(fileMetadata, stream, null).UploadAsync();
            return filepath;
        }


        private Stream GetPipeStream(FilesResource.GetRequest request, CancellationToken cancellationToken)
        {
            var pipe = new System.IO.Pipelines.Pipe();
            _ = Task.Run(() =>
            {
                using (var stream = pipe.Writer.AsStream())
                {
                    request.Download(stream);
                }
            }, cancellationToken);
            return pipe.Reader.AsStream();
        }

        private async Task<Stream> GetMemoryStream(FilesResource.GetRequest request, CancellationToken cancellationToken)
        {
            var stream = new MemoryStream();
            await request.DownloadAsync(stream, cancellationToken);
            if (stream.CanSeek)
                stream.Position = 0;
            return stream;
        }

        private async Task<List<string>> GetIdsAsync(string file, CancellationToken cancellationToken = default)
        {
            var service = await this.GetDriveServiceAsync(cancellationToken);
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Q = $"name='{file}'";
            listRequest.Fields = "files(id, name)";
            IList<Google.Apis.Drive.v3.Data.File> files = (await listRequest.ExecuteAsync()).Files;
            if (files == null) return new List<string>();
            return files.Select(s => s.Id).ToList();
        }
        private async Task<bool> ExistsAsync(string file, CancellationToken cancellationToken = default)
        {
            var ids = await this.GetIdsAsync(file, cancellationToken);
            return ids.Any();
        }
        private bool Exists(string file)
        {
            var ids = this.GetIdsAsync(file).GetAwaiter().GetResult();
            return ids.Any();
        }
        private string ReName(string BlobName)
        {
            BlobName = PathHelper.FixDirectorySeparatorChar(BlobName);
            var fileName = Path.GetFileNameWithoutExtension(BlobName);
            var fileExtinision = Path.GetExtension(BlobName);
            var directory = PathHelper.GetDirectoryName(BlobName);
            var filePath = BlobName;
            if (this.Exists(filePath))
            {
                filePath = PathHelper.Combine(directory, $"{fileName}({DateTime.UtcNow.ToString("dd-MM-yyyy")}){fileExtinision}");

                while (this.Exists(filePath))
                {
                    filePath = PathHelper.Combine(directory, $"{fileName}({DateTime.UtcNow.ToString("dd-MM-yyyy")})({Guid.NewGuid()}){fileExtinision}");
                }
            }
            return filePath;
        }

    }
}
