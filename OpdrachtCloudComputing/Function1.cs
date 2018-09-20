using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace LocationFunction
{
    public static class GetCoord
    {
        public class weather
        {
            public Coordinates coord { get; set; }
        }

        public class Coordinates
        {
            public double lon { get; set; }
            public double lat { get; set; }
        }

        public class imageObject
        {
            public string lon { get; set; }
            public string lat { get; set; }
            public string blobName { get; set; }
            public string blobContainerReference { get; set; }

            public imageObject (string lon, string lat, string blobName, string blobContainerReference)
            {
                this.lon = lon;
                this.lat = lat;
                this.blobName = blobName;
                this.blobContainerReference = blobContainerReference;
            }
        }

        [FunctionName("GetCoord")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            HttpClient client = new HttpClient();
            // API keys
            const string WEATHERAPIKEY = "047841853ac6327c2d6bd8a8c22a1a4b";
            const string MAPSAPIKEY = "n1X2rjJfbit5G4sno9oh245tzzEI-wdkKaYYoA_wVWs";
            const string BLOBSTORAGECONSTRING = "DefaultEndpointsProtocol=https;AccountName=storageaccountccopdracht;AccountKey=PW9FsfikQCCvSaYm2ghsBM11WEEWXrk/HuhZwinSj5as5l6sbWIEyj/z6R6h0ExBp/i7CZZ+2Jzw4tbkp/PqMw==;EndpointSuffix=core.windows.net";


            // parse query parameter for city
            string city = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "city", true) == 0)
                .Value;

            if (city == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                city = data?.city;
            }

            // parse query parameter for country
            string country = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "country", true) == 0)
                .Value;

            if (country == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                country = data?.country;
            }

            // Check if both city and country are not equal to null
            if (city != null && country != null)
            {                
                // Get the lon and lat for the given city and country
                var weatherApiUrl = String.Format("http://api.openweathermap.org/data/2.5/weather?q={0},{1}&appid={2}", city, country, WEATHERAPIKEY);
                weather weather = await GetCoordinates(weatherApiUrl);

                if (weather.coord.lon != null && weather.coord.lat != null)
                {
                    // Converting the commas in the lon and lat to dots
                    string lon = ConvertCommaToDot(weather.coord.lon);
                    string lat = ConvertCommaToDot(weather.coord.lat);

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BLOBSTORAGECONSTRING);
                    
                    // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    // Create the blobcontainer with permissions if it doesnt exist
                    var cloudBlobContainer = cloudBlobClient.GetContainerReference("mapblob");
                    await cloudBlobContainer.CreateIfNotExistsAsync();
                    BlobContainerPermissions permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    };
                    await cloudBlobContainer.SetPermissionsAsync(permissions);

                    // Create a Globally  Unique Identifier (GUID)
                    var guid = Guid.NewGuid().ToString();

                    // Define the needed parameters for the imaga object
                    string blobName = String.Format("map-{0}-{1}-{2}.jpg", city, country, guid);
                    string blobContainerReference = "mapblob";
                    string bloburl = String.Format("https://storageaccountccopdracht.blob.core.windows.net/{0}/{1}",blobContainerReference, blobName);

                    // Make the image object 
                    imageObject imageObj = new imageObject(lon, lat, blobName, blobContainerReference);

                    // Convert the image object into Json
                    string json = JsonConvert.SerializeObject(imageObj);

                    // Create the queue if it doesnt exist
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                    CloudQueue queue = queueClient.GetQueueReference("mapqueue");
                    await queue.CreateIfNotExistsAsync();
                    CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                    // Add the Json message to the queue
                    CloudQueueMessage queueMessage = new CloudQueueMessage(json);
                    queue.AddMessage(queueMessage);


                    return req.CreateResponse(HttpStatusCode.OK, "linkieeeeeeeee", bloburl);

                }
                else
                    return req.CreateErrorResponse(HttpStatusCode.NotFound, "The city & country combination provived has not been found");
            }
            else
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Please enter a city and it's matching country");
            }

            async Task<weather> GetCoordinates(string url)
            {
                weather weather = null;
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    weather = await response.Content.ReadAsAsync<weather>();
                }
                return weather;
            }

            string ConvertCommaToDot(double d)
            {
                string s = d.ToString();
                string output = s.Replace(",", ".");
                return output;
            }
        }
    }
}

