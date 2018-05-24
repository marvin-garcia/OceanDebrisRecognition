using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace PictureUploaderFunctionApp
{
    public static class UploadPicture
    {
        [FunctionName("UploadPicture")]
        public static void Run([TimerTrigger("0 0 */1 * * 1-6")]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageAccount"].ConnectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer srcContainer = blobClient.GetContainerReference(ConfigurationManager.AppSettings["SourceContainerName"]);
                CloudBlobContainer dstContainer = blobClient.GetContainerReference(ConfigurationManager.AppSettings["DestinationContainerName"]);

                var blobs = srcContainer.ListBlobs(useFlatBlobListing: true);

                var random = new Random();
                var sourceBlob = (CloudBlockBlob)blobs.ToList()[random.Next(blobs.Count())];

                CloudBlockBlob targetBlob = dstContainer.GetBlockBlobReference(Guid.NewGuid().ToString());
                var copied = targetBlob.StartCopy(sourceBlob);

                return;
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                throw e;
            }
        }
    }
}
