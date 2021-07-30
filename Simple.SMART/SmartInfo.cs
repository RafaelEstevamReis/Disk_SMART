using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace Simple.SMART
{
    public class SmartInfo
    {
        public DriveInfo[] DriveInformation { get; private set; }

        private SmartInfo() { }
        public static SmartInfo Collect()
        {
            var sm = new SmartInfo();
            sm.execute();
            return sm;
        }

        private void execute()
        {
            DriveInformation = listDrives().ToArray();

            // Attribute names
            Dictionary<int, string> dicNames = new Dictionary<int, string>();
            foreach (var data in AttributesInfo.attributeNames.Replace("\r", "").Split('\n'))
            {
                if (data.Length < 5) continue;

                dicNames.Add(Convert.ToInt32(data.Substring(0, 4), 16), data.Substring(5));
            }

            // Fill attribute names and Drive OK
            foreach (var d in DriveInformation)
            {
                if (d.Attributes == null) continue;

                d.IsOK = true;
                // Names and OK
                foreach (var att in d.Attributes)
                {
                    // OK
                    if (!att.IsOK) d.IsOK = false;
                    // Name
                    if (dicNames.ContainsKey(att.Register)) att.Name = dicNames[att.Register];
                }
            }
        }

        private IEnumerable<DriveInfo> listDrives()
        {
            foreach (var device in new ManagementObjectSearcher(@"SELECT * FROM Win32_DiskDrive").Get())
            {
                string pnpId = device.GetPropertyValue("PNPDeviceID").ToString();
                yield return new DriveInfo()
                {
                    DeviceID = device.GetPropertyValue("DeviceID").ToString(),
                    PNPDeviceID = pnpId,
                    Model = device["Model"]?.ToString().Trim(),
                    Type = device["InterfaceType"]?.ToString().Trim(),
                    Serial = device["SerialNumber"]?.ToString().Trim(),
                    Attributes = getDriveAttributes(pnpId).ToArray(),
                };
            }
        }

        private IEnumerable<AttributesInfo> getDriveAttributes(string pnpId)
        {
            var dicData = new Dictionary<int, AttributesInfo>();
            var scope = new ManagementScope("\\\\.\\ROOT\\WMI");

            // Base status
            var queryStatus = new ObjectQuery($"SELECT * FROM MSStorageDriver_FailurePredictData Where InstanceName like \"%{ pnpId.Replace("\\", "\\\\") }%\"");
            var searcher = new ManagementObjectSearcher(scope, queryStatus);

            foreach (var info in searcher.Get())
            {
                // Get vendor byte array
                var bytes = (byte[])info.Properties["VendorSpecific"].Value;

                for (int i = 0; i < 42; ++i)
                {
                    int id = bytes[i * 12 + 2];
                    if (id == 0) continue;

                    int flags = bytes[i * 12 + 4];
                    bool failureFlag = (flags & 0x1) == 0x1;

                    dicData[id] = new AttributesInfo()
                    {
                        Register = id,
                        Current = bytes[i * 12 + 5],
                        Worst = bytes[i * 12 + 6],
                        Data = BitConverter.ToInt32(bytes, i * 12 + 7),
                        Flags = flags,
                        IsOK = !failureFlag,
                    };
                }
            }
            // Threshold
            searcher.Query = new ObjectQuery($"SELECT * FROM MSStorageDriver_FailurePredictThresholds Where InstanceName like \"%{ pnpId.Replace("\\", "\\\\") }%\"");
            foreach (var info in searcher.Get())
            {
                var bytes = (byte[])info.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 42; ++i)
                {
                    int id = bytes[i * 12 + 2];
                    if (id == 0) continue;

                    int thresh = bytes[i * 12 + 3];

                    if (!dicData.ContainsKey(id))
                    {
                        dicData.Add(id, new AttributesInfo()
                        {
                            Register = id
                        });
                    }

                    dicData[id].Threshold = thresh;
                }
            }

            return dicData.Values;
        }
    }
}
