

using ST.FileStorage.InMemory;

namespace ST.FileStorage.Abstractions.Builders
{
    public static class InMemoryStorageServiceExtension
    {
        /// <summary>
        /// this service use Dictionary&lt;string, MemoryStream&gt; to store streams.<br/>
        /// <b>use it for unitTest only </b>
        /// </summary> 
        public static FileServiceBuilder UseInMemoryStorage(this FileServiceBuilder builder)
        {
            builder.Set(InMemoryFileService.Get());
            return builder;
        }
    }
}
