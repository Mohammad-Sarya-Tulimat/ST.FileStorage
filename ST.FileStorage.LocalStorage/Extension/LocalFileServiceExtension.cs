
using ST.FileStorage.LocalStorage;
using ST.FileStorage.LocalStorage.Options;

namespace ST.FileStorage.Abstractions.Builders
{
    public static class LocalFileServiceExtension
    {
        public static FileServiceBuilder UseLocalStorage(this FileServiceBuilder builder, LocalFileOptions FileOptions)
        {
            builder.Set(new LocalFileService(FileOptions));
            return builder;
        }
    }
}
