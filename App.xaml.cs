using System;
using System.IO;
using System.Windows;

namespace wpfGMTraceability
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string AppFolderPath = @"C:\GMTraceability\";
        public static string ConfigSettingsFilePath = $@"{AppFolderPath}DATA\Settings.json";
        public static string ConfigPortsFilePath = $@"{AppFolderPath}DATA\Ports.json";
        public static string SerialPortStatusMessage = "...";
        public static string APIUrlCheckSerial = "";
        public static string TraceType = "";
        protected override void OnStartup(StartupEventArgs e)
        {
            string sPath = $@"{App.AppFolderPath}DATA\";
            if (!Directory.Exists(sPath)) { Directory.CreateDirectory(sPath); }

            base.OnStartup(e);

            //Si ocurre en un background thread o en el arranque, se guarda en error.log.
            AppDomain.CurrentDomain.UnhandledException += (s, exArgs) =>
            {
                var ex = exArgs.ExceptionObject as Exception;
                System.IO.File.WriteAllText("error.log", ex?.ToString());
            };

            //Si el error ocurre en el UI thread, se guarda en ui-error.log
            DispatcherUnhandledException += (s, exArgs) =>
            {
                System.IO.File.WriteAllText("ui-error.log", exArgs.Exception.ToString());
                exArgs.Handled = true;
            };
        }
    }
}
