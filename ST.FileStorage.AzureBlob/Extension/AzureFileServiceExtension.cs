

using ST.FileStorage.AzureBlob;
using ST.FileStorage.AzureBlob.Options;

namespace ST.FileStorage.Abstractions.Builders
{
    public static class AzureFileServiceExtension
    {
        public static FileServiceBuilder UseAzureStorage(this FileServiceBuilder builder, AzureFileServiceOptions options)
        {
            builder.Set(new AzureFileService(options));
            return builder;
        }
    }
}
