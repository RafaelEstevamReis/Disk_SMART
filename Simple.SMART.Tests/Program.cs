using System;

namespace Simple.SMART.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var info = SmartInfo.Collect();

            foreach (var disk in info.DriveInformation)
            {
                Console.WriteLine($"Drive [{disk.Index}] {disk.Model} {disk.Serial}");
                Console.WriteLine($" Id {disk.DeviceID}");
                Console.WriteLine($" SMART:");
                if (disk.Attributes == null) continue;

                foreach (var att in disk.Attributes)
                {
                    Console.WriteLine($"  {att}");
                }
            }
        }
    }
}
