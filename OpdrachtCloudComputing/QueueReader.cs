using System;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using OpdrachtCloudComputing;

namespace QueueReaderOpdrachtCloudComputing
{
    public static class QueueReader
    {
        [FunctionName("QueueReader")]
        public static async System.Threading.Tasks.Task RunAsync([QueueTrigger("mapqueue", Connection = "AzureWebJobsStorage")]string queueMessage, TraceWriter log)
        {
            try
            {
                // Create the HttpClient
                var client = new HttpClient();
               
                // Convert Json message back to its original object (lon, lat, blobname, blobcontainerreference)
                QueueStorageMessage queueStorageMessage = JsonConvert.DeserializeObject<QueueStorageMessage>(queueMessage);

                // Retrieve storage account
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

                // Retrieve container if it exists, create one if not
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("mapblob");
                await container.CreateIfNotExistsAsync();

                // Create the blob
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(queueStorageMessage.blobName);

                // Retrieve the picture for the given city using its lat and lon
                var url = String.Format("https://atlas.microsoft.com/map/static/png?subscription-key={0}&api-version=1.0&center={1},{2}", Environment.GetEnvironmentVariable("MapsAPIKey"), queueStorageMessage.lon, queueStorageMessage.lat);
                client.BaseAddress = new Uri(url);
                HttpResponseMessage responseMessage = await client.GetAsync(url);

                if (responseMessage.IsSuccessStatusCode)
                {
                    Stream stream = await responseMessage.Content.ReadAsStreamAsync();
                    
                    // Check if the the temprature is high enough to have a beer                     
                    string bier = checkForBier(queueStorageMessage);

                    // Format the string for usability reasons
                    string temprature = String.Format("Temp: {0} \u2103", queueStorageMessage.temp.ToString());

                    // Draw on the image using the ImageHelper class
                    Stream renderedImage = ImageHelper.AddTextToImage(stream, (temprature, (10, 20)), (bier, (10, 50)));

                    // Upload the image to the blob
                    await blockBlob.UploadFromStreamAsync(renderedImage);
                    log.Info("City has been found and image was uploaded succesfully");
                }
                else
                    log.Error("Could not retrieve the map for the given coordinates");
            }
            catch
            {
                log.Error("Something went wrong.");
            }
        }

        public static string checkForBier(QueueStorageMessage queueStorageMessage)
        {
            const double BIERMIN = 15.00;
            string bier;

            if (queueStorageMessage.temp >= BIERMIN)
            {
                return bier = "Time for beer !";
            }
            else if(queueStorageMessage.temp == BIERMIN)
            {
                return bier = "Water maybe?";
            }
            else
                return bier = "Id go for a hot chocolate";
        }
    }
}
