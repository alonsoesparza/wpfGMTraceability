using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfGMTraceability.Helpers
{
    public class SettingsManager
    {
        public static string AppFolderPath = @"C:\GMTraceability\";
        public static string ConfigSettingsFilePath = $@"{AppFolderPath}DATA\Settings.json";
        public static string ConfigPortsFilePath = $@"{AppFolderPath}DATA\Ports.json";
        public static string ConfigWritePortsFilePath = $@"{AppFolderPath}DATA\WPorts.json";
        public static string SerialPortStatusMessage = "...";
        public static string APIUrlCheckSerial { get; set; }
        public static string APILoadBOMUrl;
        public static string APIRequestBoxUrl;
        public static string APIConsumeSerialUrl;
        public static string TraceType { get; set; }
    }
}
