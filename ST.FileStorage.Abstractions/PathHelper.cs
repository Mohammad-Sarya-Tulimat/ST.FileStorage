using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace ST.FileStorage.Abstractions
{
    public class PathHelper
    {
        public static string Combine(params string[] paths)
        {
            return FixDirectorySeparatorChar(Path.Combine(paths));
        }
        public static string FixDirectorySeparatorChar(string path)
        {
            if (path != null)
                return path.Replace("\\", "/");
            return path;
        }
        public static IEnumerable<string> GetFolderNames(string folder)
        {
            if (folder != null)
            {
                yield return folder.Replace("\\", "/");
                yield return folder.Replace("/", "\\");
            }
        }

        public static bool IsSubPath(string parent, string child)
        {
            try
            {
                // Normalize the paths to handle different formats (e.g., slashes, case sensitivity)
                parent = Path.GetFullPath(parent);
                if (!parent.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    parent = parent + Path.DirectorySeparatorChar;
                child = Path.GetFullPath(child);
                // If the child path is shorter than or equal to the parent path, it cannot be a subpath
                if (child.Length <= parent.Length)
                    return false;
                // Check if the parent path is a prefix of the child path
                var result= child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
                return result;
            }
            catch { return false; }
        }

        public static bool IsParent(string parentFolder, string childFolderOrFile)
        {
            try
            {
                // Normalize the paths to handle different formats (e.g., slashes, case sensitivity)
                var parent = Path.GetFullPath(parentFolder);
                if (!parent.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    parent = parent + Path.DirectorySeparatorChar; 
                var child = Path.GetFullPath(childFolderOrFile);

                child= new FileInfo(child).DirectoryName;
                if (!child.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    child = child + Path.DirectorySeparatorChar;


                child = FixDirectorySeparatorChar(child);
                parent=new DirectoryInfo(parent).FullName;
                parent = FixDirectorySeparatorChar(parent); 
                var result = child.Equals(parent, StringComparison.OrdinalIgnoreCase);
                return result;
            }
            catch { return false; }
        }

        public static string GetDirectoryName(string filename)
        {
            return FixDirectorySeparatorChar(Path.GetDirectoryName(filename));
        }

    }
}
