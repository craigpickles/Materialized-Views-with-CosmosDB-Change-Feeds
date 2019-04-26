namespace SampleApp.Events
{
    using Microsoft.Azure.Documents;

    public class DeviceEvent
    {
        public string DeviceId { get; set; }

        public string TimeStamp { get; set; }

        public string Type => GetType().Name;

        public static DeviceEvent FromDocument(Document document)
        {
            if (document.GetPropertyValue<string>("Type") == nameof(DeviceSensorEvent))
            {
                return new DeviceSensorEvent
                {
                    DeviceId = document.GetPropertyValue<string>("DeviceId"),
                    Value = document.GetPropertyValue<double>("Value")
                };
            }

            if (document.GetPropertyValue<string>("Type") == nameof(DeviceBatteryEvent))
            {
                return new DeviceBatteryEvent
                {
                    DeviceId = document.GetPropertyValue<string>("DeviceId"),
                    Value = document.GetPropertyValue<double>("Value")
                };
            }

            return null;
        }
    }
}
