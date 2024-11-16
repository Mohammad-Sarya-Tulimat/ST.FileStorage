using System;
using System.Collections.Generic;
using System.Text;

namespace ST.FileStorage.Abstractions.Enum
{
    public enum FileExistsHandling
    {
        Overwrite = 1,
        Rename,
        ThrowException
    }
}
