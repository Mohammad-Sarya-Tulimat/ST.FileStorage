using System;
using System.Collections.Generic;
using System.Text;

namespace ST.FileStorage.Abstractions.Builders
{
    public class FileServiceBuilder
    {
        private IFileService fileService;
        public IFileService GetFileService()
        {
            return fileService;
        }
        public FileServiceBuilder Set(IFileService fileService)
        {
            this.fileService = fileService;
            return this;
        }

    }
}
