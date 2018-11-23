namespace OpdrachtCloudComputing
{
    public class QueueStorageMessage
    {
        public string lon { get; set; }
        public string lat { get; set; }
        public double temp { get; set; }
        public string blobName { get; set; }
        public string blobContainerReference { get; set; }

        public QueueStorageMessage(string lon, string lat, double temp, string blobName, string blobContainerReference)
        {
            this.lon = lon;
            this.lat = lat;
            this.temp = temp;
            this.blobName = blobName;
            this.blobContainerReference = blobContainerReference;
        }
    }
}
