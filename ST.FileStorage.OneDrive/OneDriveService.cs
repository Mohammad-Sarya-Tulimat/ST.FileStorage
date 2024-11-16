using Azure.Identity;
using Microsoft.Graph;
using ST.FileStorage.Abstractions;
using ST.FileStorage.Abstractions.Enum;
using ST.FileStorage.Abstractions.Exceptions;
using ST.FileStorage.OneDrive.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace ST.FileStorage.OneDrive
{
    public class OneDriveService : IFileService
    {
        OneDriveOptions _oneDriveOptions;
        GraphServiceClient _graphServiceClient;
        public OneDriveService(OneDriveOptions oneDriveOptions)
        {
            _oneDriveOptions = oneDriveOptions;
            var scope = new string[] { "https://graph.microsoft.com/.default" };
            var clientSecretCredential = new ClientSecretCredential(
                            _oneDriveOptions.TenantId, _oneDriveOptions.ClientId, _oneDriveOptions.ClientSecret);
            _graphServiceClient = new GraphServiceClient(clientSecretCredential, scope);
        }


        public async Task<string> Copy(string srcFile, string destFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {

            destFile = PathHelper.FixDirectorySeparatorChar(destFile);
            if (handlingType == FileExistsHandling.Rename) destFile = ReName(destFile);
            if (await this.ExistsAsync(srcFile))
            {
                if (handlingType == FileExistsHandling.ThrowException && await this.ExistsAsync(destFile, cancellationToken))
                    throw new FileServiceException($"Cannot copy the file{srcFile} because the file {destFile} already Exists");

                var destinationItemReference = new ItemReference { Path = PathHelper.GetDirectoryName(destFile) };
                var copyOperation = await _graphServiceClient.Me.Drive.Root
                    .ItemWithPath(srcFile)
                    .Copy(Path.GetFileName(destFile), destinationItemReference)
                    .Request().PostAsync();
            }
            else throw new FileNotFoundException("Can not find file", srcFile);
            return destFile;
        }

        public async Task Delete(string filePath, CancellationToken cancellationToken = default)
        {
            await _graphServiceClient.Me.Drive.Root.ItemWithPath(filePath).Request().DeleteAsync(cancellationToken);
        }

        public void Dispose()
        {
        }

        public async Task<List<string>> GetFileList(string folder, string pattern = ".*", bool includeSubFolders = false, CancellationToken cancellationToken = default)
        {
            List<string> result = new List<string>();

            foreach (var name in PathHelper.GetFolderNames(folder))
            {
                var blobs = await _graphServiceClient.Me.Drive.Root.ItemWithPath(folder).Children.Request().GetAsync();
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
            return result.Distinct().ToList();
        }

        public async Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            folder = PathHelper.FixDirectorySeparatorChar(folder);
            var destFile = PathHelper.Combine(folder, Path.GetFileName(filePath));
            destFile = PathHelper.FixDirectorySeparatorChar(destFile);
            if (handlingType == FileExistsHandling.Rename) destFile = ReName(destFile);

            if (await this.ExistsAsync(filePath, cancellationToken))
            {
                if (handlingType == FileExistsHandling.ThrowException && await this.ExistsAsync(destFile, cancellationToken))
                    throw new FileServiceException($"Cannot move the file{filePath} because the file {destFile} already Exists");
                var destinationItemReference = new ItemReference { Path = PathHelper.GetDirectoryName(destFile) };
                var copyOperation = await _graphServiceClient.Me.Drive.Root
                    .ItemWithPath(filePath)
                    .Copy(Path.GetFileName(destFile), destinationItemReference)
                    .Request().PostAsync();

            }
            else throw new FileNotFoundException("Can not find file", filePath);

            return destFile;
        }

        public async Task<Stream> Read(string filePath, CancellationToken cancellationToken = default)
        {
            if (!await this.ExistsAsync(filePath, cancellationToken)) throw new FileNotFoundException("Can not file", filePath);
            return await _graphServiceClient.Me.Drive.Root.ItemWithPath(filePath).Content.Request().GetAsync(cancellationToken);
        }

        public async Task<string> Save(string folderPath, string fileName, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {

            var filepath = PathHelper.Combine(folderPath, fileName);
            if (handlingType == FileExistsHandling.Rename) filepath = ReName(filepath);
            if (handlingType == FileExistsHandling.ThrowException && await this.ExistsAsync(filepath, cancellationToken))
            {
                throw new FileServiceException($"the File {filepath} is exists");
            }
            var driveItem =
                await _graphServiceClient.Me.Drive.Root
                .ItemWithPath(filepath)
                .Content.Request()
                .PutAsync<DriveItem>(stream);

            return filepath;
        }

        private bool Exists(string file)
        {
            try
            {
                var driveItem = _graphServiceClient.Me.Drive.Root.ItemWithPath(file).Request().GetAsync().GetAwaiter().GetResult();
                return driveItem != null;
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return false; }
                else { throw new ST.FileStorage.Abstractions.Exceptions.FileServiceException("Error while get file data", ex); }
            }
            catch (Exception ex)
            {
                throw new ST.FileStorage.Abstractions.Exceptions.FileServiceException("Error while get file data", ex);
            }
        }
        private async Task<bool> ExistsAsync(string file, CancellationToken cancellationToken = default)
        {
            try
            {
                var driveItem =
                    await _graphServiceClient.Me.Drive.Root.ItemWithPath(file).Request().GetAsync(cancellationToken);
                return driveItem != null;
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return false; }
                else { throw new ST.FileStorage.Abstractions.Exceptions.FileServiceException("Error while get file data", ex); }
            }
            catch (Exception ex)
            {
                throw new ST.FileStorage.Abstractions.Exceptions.FileServiceException("Error while get file data", ex);
            }
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
