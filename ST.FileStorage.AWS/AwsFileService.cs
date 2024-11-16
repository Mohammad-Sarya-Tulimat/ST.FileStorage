using Amazon.S3;
using Amazon.S3.Model;
using ST.FileStorage.Abstractions;
using ST.FileStorage.Abstractions.Enum;
using ST.FileStorage.Abstractions.Exceptions;
using ST.FileStorage.AWSS3.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ST.FileStorage.AWSS3
{
    public class AwsFileService : IFileService
    {

        private readonly AmazonS3Client _s3Client;
        private readonly AWSFileOptions _options;
        public AwsFileService(AWSFileOptions options)
        {
            if (options != null)
            {
                
                _s3Client = new AmazonS3Client(options.AccessKeyId, options.SecretAccessKey, Amazon.RegionEndpoint.GetBySystemName(options.Region));
            }

            this._options = options;
        }

        private string Rename(string key)
        {
            key = PathHelper.FixDirectorySeparatorChar(key);
            var fileName = Path.GetFileNameWithoutExtension(key);
            var fileExtension = Path.GetExtension(key);
            var directory = PathHelper.GetDirectoryName(key);
            var filePath = key;
            if (TryGetObjectMetadata(filePath, out var _))
            {
                filePath = PathHelper.Combine(directory, $"{fileName}({DateTime.UtcNow:dd-MM-yyyy}){fileExtension}");

                while (TryGetObjectMetadata(filePath, out var _))
                {
                    filePath = PathHelper.Combine(directory, $"{fileName}({DateTime.UtcNow:dd-MM-yyyy})({Guid.NewGuid()}){fileExtension}");
                }
            }
            return filePath;
        }
        private bool TryGetObjectMetadata(string fileKey, out GetObjectMetadataResponse output)
        {
            try
            {
                GetObjectMetadataRequest request = new GetObjectMetadataRequest()
                {
                    BucketName = _options.BucketName,
                    Key = fileKey
                };
                var result = _s3Client.GetObjectMetadataAsync(request).GetAwaiter().GetResult();
                if (result.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    output = null;
                    return false;
                }

                output = result;
                return true;
            }
            catch (Exception ex)
            {
                //_logger.Error(ex, "Error while getting object metadata");   
            }

            output = null;
            return false;
        }
        public async Task<string> Copy(string srcFile, string destFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var response = new CopyObjectResponse();
            try
            {
                destFile = PathHelper.FixDirectorySeparatorChar(destFile);
                if (handlingType == FileExistsHandling.Rename) destFile = this.Rename(destFile);
                var request = new CopyObjectRequest
                {
                    SourceBucket = this._options.BucketName,
                    DestinationBucket = this._options.BucketName,
                    SourceKey = srcFile,
                    DestinationKey = destFile,
                };
                response = await _s3Client.CopyObjectAsync(request, cancellationToken);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.NotFound) throw new FileNotFoundException("File is not found", srcFile);
            }
            catch (AmazonS3Exception ex)
            {
                throw new FileServiceException(ex.Message, ex);
            }
            return destFile;
        }

        public async Task Delete(string filePath, CancellationToken cancellationToken = default)
        {
            var response = new DeleteObjectResponse();
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = this._options.BucketName,
                    Key = filePath,
                };
                response = await _s3Client.DeleteObjectAsync(request, cancellationToken);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.NotFound) throw new FileNotFoundException("File is not found", filePath);
            }
            catch (AmazonS3Exception ex)
            {
                throw new FileServiceException(ex.Message, ex);
            }
        }
         
        public async Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var result = await this.Copy(filePath, PathHelper.Combine(folder, Path.GetFileName(filePath)), handlingType, cancellationToken);
            await this.Delete(filePath, cancellationToken);
            return result;
        }
        public async Task<string> Save(string folderPath, string fileName, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var key = PathHelper.Combine(folderPath, fileName);
            if (handlingType == FileExistsHandling.Rename) key = this.Rename(key);
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = this._options.BucketName,
                    Key = key,
                    InputStream = stream
                };
                var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);
                return key;
            }
            catch (AmazonS3Exception ex)
            {
                throw new FileServiceException(ex.Message, ex);
            }

        }


        public async Task<List<string>> GetFileList(string folder, string pattern = ".*", bool includeSubFolders = false, CancellationToken cancellationToken = default)
        {
            List<string> result = new List<string>();
            foreach (var name in PathHelper.GetFolderNames(folder))
            {
                var request = new ListObjectsRequest
                {
                    BucketName = this._options.BucketName,
                    Prefix = name,
                };
                var response = await this._s3Client.ListObjectsAsync(request, cancellationToken);
                var S3Objects = response.S3Objects;
                foreach (var ob in S3Objects)
                {
                    if (!includeSubFolders && !PathHelper.IsParent(name, ob.Key))
                    {
                        continue;
                    }
                    if (Regex.IsMatch(ob.Key, pattern))
                        result.Add(ob.Key);
                }
            }
            return result.Distinct().ToList();
        }
        public async Task<Stream> Read(string filePath, CancellationToken cancellationToken = default)
        {
            var response = await _s3Client.GetObjectAsync(this._options.BucketName, filePath, cancellationToken);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.NotFound) throw new FileNotFoundException("File is not found", filePath);
            return response.ResponseStream;
        }

        public void Dispose()
        {
            if (this._s3Client != null)
                this._s3Client.Dispose();
        }
    }
}
