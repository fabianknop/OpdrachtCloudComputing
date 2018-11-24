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
using OpdrachtCloudComputing;

namespace LocationFunction
{
    public static class GetCoord
    {
        [FunctionName("GetCoord")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // Create the HttpClient
            var client = new HttpClient();
           
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
                // Get the lon and lat for the given city and country, with the units being metric to give back the temprature in celsius degrees
                var weatherApiUrl = String.Format("http://api.openweathermap.org/data/2.5/weather?q={0},{1}&units=metric&appid={2}", city, country, Environment.GetEnvironmentVariable("WeatherAPIKey"));
                Weather weather = await GetWeatherData(weatherApiUrl);
                HttpResponseMessage responseMessageWeatherApi = await client.GetAsync(weatherApiUrl);

                if (responseMessageWeatherApi.IsSuccessStatusCode)
                {
                    // Converting the commas in the lon and lat to dots
                    string lon = ConvertCommaToDot(weather.coord.lon);
                    string lat = ConvertCommaToDot(weather.coord.lat);

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                    
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

                    // Define the needed parameters for the image object
                    string blobName = String.Format("map-{0}-{1}-{2}.png", city, country, guid);
                    string blobContainerReference = "mapblob";
                    string bloburl = String.Format("https://bierapifabianknop.blob.core.windows.net/{0}/{1}", blobContainerReference, blobName);                    

                    // Make the image object 
                    var imageObj = new ImageObject(lon, lat, weather.main.temp, blobName, blobContainerReference);

                    // Convert the image object into Json
                    string json = JsonConvert.SerializeObject(imageObj);

                    // Create the queue if it doesnt exist
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                    CloudQueue queue = queueClient.GetQueueReference("mapqueue");
                    await queue.CreateIfNotExistsAsync();
                    CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                    
                    // Add the Json message to the queue
                    var queueMessage = new CloudQueueMessage(json);
                    queue.AddMessage(queueMessage);

                    return req.CreateResponse(HttpStatusCode.OK, "You will find your image at the following link: " + bloburl);

                }
                else
                    return req.CreateErrorResponse(HttpStatusCode.NotFound, "The city & country combination provived has not been found");
            }
            else
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Please enter a city and its matching country");
            }

            async Task<Weather> GetWeatherData(string url)
            {
                Weather weather = null;
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    weather = await response.Content.ReadAsAsync<Weather>();
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

