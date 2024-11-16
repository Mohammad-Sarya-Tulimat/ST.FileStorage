using ST.FileStorage.Abstractions.Enum;
using System.Threading;
using System.Threading.Tasks;

namespace ST.FileStorage.Abstractions
{
    public static class FileServiceExtinsions
    {
        public static async Task<string> CopyToOtherStorage(this IFileService srcStorage, string srcFile, IFileService targetStorage, string targetFolder, string targetFileName, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            return await targetStorage.Save(targetFolder, targetFileName, await srcStorage.Read(srcFile, cancellationToken), handlingType, cancellationToken);
        }
        public static async Task<string> MoveToOtherStorage(this IFileService srcStorage, string srcFile, IFileService targetStorage, string targetFolder, string targetFileName, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default)
        {
            var result= await targetStorage.Save(targetFolder, targetFileName, await srcStorage.Read(srcFile, cancellationToken), handlingType, cancellationToken);
            await srcStorage.Delete(srcFile, cancellationToken);
            return result; 
        }
    }
}
