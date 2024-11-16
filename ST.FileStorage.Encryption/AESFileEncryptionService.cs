using ST.FileStorage.Abstractions;
using ST.FileStorage.Abstractions.Enum;
using ST.FileStorage.Encryption.Options;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ST.FileStorage.Encryption.Encryption
{
    public class AesFileEncryptionService : IFileService
    {
        public readonly IFileService _fileService;
        public readonly StorageEncryptionOption _encryptionPption;
        public AesFileEncryptionService(IFileService fileService, StorageEncryptionOption options)
        {
            _fileService = fileService;
            _encryptionPption = options;
        }
        private Stream EncryptStream(Stream stream)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionPption.Key);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor())
                {
                    using (var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
                    {
                        return cryptoStream;
                    }
                }
            }
        }
        private Stream DecryptStream(Stream stream)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionPption.Key);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor())
                {
                    using (var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
                    {
                        return cryptoStream;
                    }
                }
            }
        }
        public Task Delete(string filePath, CancellationToken cancellationToken = default)
        {
            return _fileService.Delete(filePath, cancellationToken);
        }

        public Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            return _fileService.Move(filePath, folder, handlingType, cancellationToken);
        }

        public async Task<Stream> Read(string filePath, CancellationToken cancellationToken = default)
        {
            var stream = await _fileService.Read(filePath, cancellationToken);
            return DecryptStream(stream);
        }

        public Task<string> Save(string folderPath, string fileName, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            Stream encryptedStream = EncryptStream(stream);
            return _fileService.Save(folderPath, fileName, encryptedStream, handlingType, cancellationToken);
        }

        public Task<List<string>> GetFileList(string folder, string pattern = @".*", bool includeSubFolders = false, CancellationToken cancellationToken = default)
        {
            return _fileService.GetFileList(folder, pattern, includeSubFolders, cancellationToken);
        }
        public Task<string> Copy(string srcFile, string destFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            return _fileService.Copy(srcFile, destFile, handlingType, cancellationToken);
        }
        public void Dispose()
        {
            if (this._fileService != null)
            {
                this._fileService.Dispose();
            }
        }
    }
}
