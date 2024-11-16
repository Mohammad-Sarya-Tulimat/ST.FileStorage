using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace ST.FileStorage.GoogleDrive.Options
{
    public class GoogleDriveOptions
    {
        public ClientSecrets ClientSecret { get; set; } 
        public IDataStore TokensStorage { get; set; }
        public string ApplicationName { get; set; } 
        public string User { get; set; }

        public bool UsePipeStreams { get; set; } = false;
    }
}
