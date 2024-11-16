
using ST.FileStorage.GoogleDrive;
using ST.FileStorage.GoogleDrive.Options;

namespace ST.FileStorage.Abstractions.Builders
{
    public static class GoogleDriveFileServiceExtension
    {
        public static FileServiceBuilder UseGoogleDriveStorage(this FileServiceBuilder builder, GoogleDriveOptions options)
        {
            builder.Set(new GoogleDriveService(options));
            return builder;
        } 
    }
}
