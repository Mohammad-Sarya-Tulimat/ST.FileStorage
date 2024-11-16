

using ST.FileStorage.Encryption.Encryption;
using ST.FileStorage.Encryption.Options;
using System;

namespace ST.FileStorage.Abstractions.Builders
{
    public static class EncryptionFileServiceExtension
    {
        /// <summary>
        /// add encryption layer to file storage<br/>
        /// be aware, each call for this method will add a new encryption layer.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="storageEncryptionOption"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static FileServiceBuilder DecorateWithAESEncryptor(this FileServiceBuilder builder, StorageEncryptionOption storageEncryptionOption)
        {
            var old = builder.GetFileService();
            if (old != null)
            {
                throw new NullReferenceException("AES Encryptor is a decorator pattern so you should add the base file service first then do the decoration.");
            }
            builder.Set(new AesFileEncryptionService(old, storageEncryptionOption));
            return builder;
        }
    }
}
