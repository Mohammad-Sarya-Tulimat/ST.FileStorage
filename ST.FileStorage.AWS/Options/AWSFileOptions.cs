using System;
using System.Collections.Generic; 

namespace ST.FileStorage.AWSS3.Options
{
    public class AWSFileOptions
    { 
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string BucketName { get; set; }
        public string Region { get; set; } 
    }
}
