using ST.FileStorage.Abstractions.Builders;
using ST.FileStorage.OneDrive.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.FileStorage.OneDrive.Extension
{
    public static class OneDriveServiceExtension
    {
        public static FileServiceBuilder UseLocalStorage(this FileServiceBuilder builder, OneDriveOptions FileOptions)
        {
            builder.Set(new OneDriveService(FileOptions));
            return builder;
        }
    }
}
