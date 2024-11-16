using ST.FileStorage.Abstractions.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ST.FileStorage.Abstractions
{ 
    public interface IFileService : IDisposable
    {

        /// <summary>
        /// save a new file
        /// </summary> 
        /// <param name="stream">file Content</param>
        /// <param name="handlingType">what should do if file with same name Exists</param>
        /// <returns>path with name of the file saved, if <paramref name="handlingType" /> is <see cref="FileExistsHandling.Rename"/> and the there is a file with same name the return will be new the file name otherwise the name you passed</returns>
        Task<string> Save(string folderPath, string fileName, Stream stream, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default);
        /// <returns>path with name of the new file, if <paramref name="handlingType" /> is <see cref="FileExistsHandling.Rename"/> and the there is a file with same name the return will be new the file name otherwise the name you passed</returns>

        Task<string> Copy(string srcFile, string destFile, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default);

        Task<Stream> Read(string filePath, CancellationToken cancellationToken = default);
        Task Delete(string filePath, CancellationToken cancellationToken = default);
        /// <returns>path with name of the file moved, if <paramref name="handlingType" /> is <see cref="FileExistsHandling.Rename"/> and the there is a file with same name the return will be new the file name otherwise the name you passed</returns>
        Task<string> Move(string filePath, string folder, FileExistsHandling handlingType = FileExistsHandling.ThrowException, CancellationToken cancellationToken = default);
        /// <summary>
        /// GetFiles List From Folder
        /// </summary>
        /// <param name="folder">Folder Name / Patth</param>
        /// <param name="pattern">Regex Pattern</param>
        /// <param name="includeSubFolders">includes Sub Folders</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<string>> GetFileList(string folder, string pattern = @".*", bool includeSubFolders = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// check if path exist
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        //Task<bool> Exists(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Create Directory
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        // Task<string> CreateDirectory(string directoryPath, CancellationToken cancellationToken);



    }
}
