using Google.Apis.Util.Store;
using Xunit;
namespace ST.FileStorage.GoogleDrive.Test
{
    public class UnitTest1
    {
        private GoogleDriveService FileService =>
            new GoogleDriveService(new Options.GoogleDriveOptions
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


        [Fact]
        public void Test1()
        {

            var stream = FileService.GetFileList("testtest", ".*\\.txt", default).GetAwaiter().GetResult();
            var x = stream;
        }
    }
}