namespace OpdrachtCloudComputing
{
    public class Weather
    {
        public Coordinates coord { get; set; }
        public Temprature main { get; set; }
    }

    public class Coordinates
    {
        public double lon { get; set; }
        public double lat { get; set; }
    }

    public class Temprature
    {
        public string temp { get; set; }
    }
}
