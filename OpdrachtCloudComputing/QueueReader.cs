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
                // Aanmaken van de HttpClient
                var client = new HttpClient();
                // API keys
                const string MAPSAPIKEY = "n1X2rjJfbit5G4sno9oh245tzzEI-wdkKaYYoA_wVWs";
                const string AzureStorageKey = "DefaultEndpointsProtocol=https;AccountName=storageaccountccopdracht;AccountKey=PW9FsfikQCCvSaYm2ghsBM11WEEWXrk/HuhZwinSj5as5l6sbWIEyj/z6R6h0ExBp/i7CZZ+2Jzw4tbkp/PqMw==;EndpointSuffix=core.windows.net";

                // Json message terug zetten naar het originele object (lon, lat, blobname, blobcontainerreference)
                QueueStorageMessage queueStorageMessage = JsonConvert.DeserializeObject<QueueStorageMessage>(queueMessage);

                // Storage account ophalen
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(AzureStorageKey);

                // Container opnieuw ophalen en aanmaken als hij er niet is
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("mapblob");
                await container.CreateIfNotExistsAsync();

                // Maak de blob aan
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(queueStorageMessage.blobName);

                // Haal de lat en long op
                var url = String.Format("https://atlas.microsoft.com/map/static/png?subscription-key={0}&api-version=1.0&center={1},{2}", MAPSAPIKEY, queueStorageMessage.lon, queueStorageMessage.lat);
                client.BaseAddress = new Uri(url);
                HttpResponseMessage responseMessage = await client.GetAsync(url);

                if (responseMessage.IsSuccessStatusCode)
                {
                    Stream stream = await responseMessage.Content.ReadAsStreamAsync();
                    // Teken op het plaatje
                    string temprature = String.Format("Temp: {0} \u2103", queueStorageMessage.temp.ToString());
                    string bier = checkForBier(queueStorageMessage);
                    Stream renderedImage = ImageHelper.AddTextToImage(stream, (temprature, (10, 20)), (bier, (10, 50)));

                    //upload het plaatje
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
            const double bierMin = 15.00;
            string bier;

            if (queueStorageMessage.temp >= bierMin)
            {
                return bier = "Time for beer !";
            }
            else if(queueStorageMessage.temp == bierMin)
            {
                return bier = "Water maybe?";
            }
            else
                return bier = "Id go for a hot chocolate";
        }
    }
}
