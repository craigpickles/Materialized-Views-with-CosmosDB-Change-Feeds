namespace SampleApp.Events
{
    public class DeviceSensorEvent : DeviceEvent
    {
        public double Value { get; set; }

        public override string ToString()
        {
            return string.Format($"{DeviceId}: {TimeStamp} - Sensor: {Value}");
        }
    }
}