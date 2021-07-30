namespace Simple.SMART
{
    public class DriveInfo
    {
        public int Index { get; set; }
        public string DeviceID { get; set; }
        public string PNPDeviceID { get; set; }

        public bool IsOK { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Serial { get; set; }
        public AttributesInfo[] Attributes {get;set;}
    }
}
