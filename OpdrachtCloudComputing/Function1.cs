using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

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

        [FunctionName("GetCoord")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            HttpClient client = new HttpClient();

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
                // API keys
                const string WEATHERAPIKEY = "047841853ac6327c2d6bd8a8c22a1a4b";
                const string MAPSAPIKEY = "n1X2rjJfbit5G4sno9oh245tzzEI-wdkKaYYoA_wVWs";

                // Get the lon and lat for the given city and country
                var weatherApiUrl = string.Format("http://api.openweathermap.org/data/2.5/weather?q={0},{1}&appid={2}", city, country, WEATHERAPIKEY);
                weather weather = await GetCoordinates(weatherApiUrl);

                if (weather.coord.lon != null && weather.coord.lat != null)
                {                    
                    // Converting the commas in the lon and lat to dots
                    string lon = ConvertCommaToDot(weather.coord.lon);
                    string lat = ConvertCommaToDot(weather.coord.lat);

                    // Create a Globally  Unique Identifier (GUID)
                    var guid = Guid.NewGuid().ToString();

                    //string blobName = string.Format("map-{0}-{1}-{2}.png", city, country, guid);
                    //string blobAzureReference = "mapsblob";
                
                    var mapApiUrl = string.Format("https://atlas.microsoft.com/map/static/png?subscription-key={0}&api-version=1.0&center={1},{2}", MAPSAPIKEY, lon, lat);
                    HttpResponseMessage response = await client.GetAsync(mapApiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        System.IO.Stream responseStream = await response.Content.ReadAsStreamAsync();
                    }

                    return mapApiUrl == null
                    ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a city on the query string or in the request body")
                    : req.CreateResponse(HttpStatusCode.OK, "The city is: " + mapApiUrl);
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
                s.Replace(",", ".");
                return s;
            }
        }
    }
}

