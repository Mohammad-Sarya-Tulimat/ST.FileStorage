using Google.Apis.Util.Store;

namespace ST.FileStorage.GoogleDrive.Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            GoogleDriveService service = new GoogleDriveService(new Options.GoogleDriveOptions
            {
                TokensStorage = new FileDataStore("", true),
                ClientSecret = new Google.Apis.Auth.OAuth2.ClientSecrets
                {
                    ClientId = "",
                    ClientSecret = "",
                },
                ApplicationName = "",
                User = ""
            });
            var stream = service.GetFileList("testtest",".*\\.txt", default).GetAwaiter().GetResult();  
            var x = stream;
        }
    }
}