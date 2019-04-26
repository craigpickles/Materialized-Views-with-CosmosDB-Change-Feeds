namespace SampleApp.Functions
{
    using Newtonsoft.Json;

    public class DeviceMaterializedView
    {
        [JsonProperty("id")]
        public string Id => DeviceId;
        public string DeviceId { get; set; }
        public string BatteryLevel { get; set; }
        public int SensorMeasurements { get; set; }
        public double SensorAggregationSum { get; set; }
        public double SensorLastValue { get; set; }
        public string TimeStamp { get; set; }
    }
}
