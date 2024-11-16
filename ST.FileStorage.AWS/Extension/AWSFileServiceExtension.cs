using ST.FileStorage.AWSS3;
using ST.FileStorage.AWSS3.Options;
namespace ST.FileStorage.Abstractions.Builders
{
    public static class AWSFileServiceExtension
    {
        public static FileServiceBuilder UseAWSStorage(this FileServiceBuilder builder, AWSFileOptions aWSFileOptions)
        {
            builder.Set(new AwsFileService(aWSFileOptions));
            return builder;
        }
    }
}
