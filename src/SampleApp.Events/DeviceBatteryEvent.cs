namespace SampleApp.Events
{
    public class DeviceBatteryEvent : DeviceEvent
    {
        public double Value { get; set; }

        public override string ToString()
        {
            return string.Format($"{DeviceId}: {TimeStamp} - Battery: {Value}");
        }
    }
}
