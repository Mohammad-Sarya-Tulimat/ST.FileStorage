using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ST.FileStorage.Abstractions.Exceptions
{
    public class FileServiceException:IOException
    {
        public FileServiceException(string message) : base(message) { }
        public FileServiceException(string message,Exception ex) : base(message, ex) { }
    }
}
