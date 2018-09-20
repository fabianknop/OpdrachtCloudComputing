using System;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace QueueReaderOpdrachtCloudComputing
{
    public class QueueStorageMessage
    {
        public string lon { get; set; }
        public string lat { get; set; }
        public string blobName { get; set; }
        public string blobContainerReference { get; set; }

        public QueueStorageMessage(string lon, string lat, string blobName, string blobContainerReference)
        {
            this.lon = lon;
            this.lat = lat;
            this.blobName = blobName;
            this.blobContainerReference = blobContainerReference;
        }
    }

    public static class Function1
    {
        [FunctionName("QueueReader")]
        public static async System.Threading.Tasks.Task RunAsync([QueueTrigger("mapqueue", Connection = "DefaultEndpointsProtocol=https;AccountName=storageaccountccopdracht;AccountKey=PW9FsfikQCCvSaYm2ghsBM11WEEWXrk/HuhZwinSj5as5l6sbWIEyj/z6R6h0ExBp/i7CZZ+2Jzw4tbkp/PqMw==;EndpointSuffix=core.windows.net")]string queueMessage, TraceWriter log)
        {
            // Aanmaken van de HttpClient
            var client = new HttpClient();
            // API keys
            const string MAPSAPIKEY = "n1X2rjJfbit5G4sno9oh245tzzEI-wdkKaYYoA_wVWs";
            const string BLOBSTORAGECONSTRING = "DefaultEndpointsProtocol=https;AccountName=storageaccountccopdracht;AccountKey=PW9FsfikQCCvSaYm2ghsBM11WEEWXrk/HuhZwinSj5as5l6sbWIEyj/z6R6h0ExBp/i7CZZ+2Jzw4tbkp/PqMw==;EndpointSuffix=core.windows.net";

            // Json message terug zetten naar het originele object (lon, lat, blobname, blobcontainerreference)
            QueueStorageMessage queueStorageMessage = JsonConvert.DeserializeObject<QueueStorageMessage>(queueMessage);          

            // Storage account ophalen
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BLOBSTORAGECONSTRING);

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
                System.IO.Stream stream = await responseMessage.Content.ReadAsStreamAsync();
                // upload het plaatje
                await blockBlob.UploadFromStreamAsync(stream);
            }
            else
                log.Info("gaat fout");
        }
    }
}
