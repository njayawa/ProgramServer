using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security.Principal;

namespace ProgramServer
{
    public class RegistryWatcher
    {
        private ManagementEventWatcher _watcher;
        private object _registryQueueLock = new object();
        private Queue<string> _currentShow = new Queue<string>();
        private const string Hkeylm = "HKEY_LOCAL_MACHINE";
        public const string KeyPath = @"SOFTWARE\TESTMSAS\PROGRAMINFO";
        public const string FullPath = Hkeylm + "\\" + KeyPath;
        public const string ValueName = "Name";
        public const string InvalidShow = "{0C73E008-4241-4F2C-AD9E-8C7E6AFEFF10}";
        private EventArrivedEventHandler _handler;

        public RegistryWatcher(EventArrivedEventHandler handler)
        {
            _handler = handler;
        }

        public void InitializeRegistryWatcher()
        {
            CreateRegIfNotFound();
                WqlEventQuery query = new WqlEventQuery(
                     "SELECT * FROM RegistryValueChangeEvent WHERE " +
                     "Hive = 'HKEY_LOCAL_MACHINE'" +
                     @"AND KeyPath = 'SOFTWARE\\testmsas\\programinfo' AND ValueName='name'");

                _watcher = new ManagementEventWatcher(query);
                Console.WriteLine("Waiting for an event...");


            if(_handler != null )
                _watcher.EventArrived += _handler;
            else
            {
                _watcher.EventArrived += KeyValueChanged;
            }
            _watcher.Start();
        }

        private void KeyValueChanged(object sender, EventArrivedEventArgs e)
        {
            string programName = (string)Registry.GetValue(FullPath,
                ValueName,
                InvalidShow);
            Console.Out.WriteLine("Found program {0}", programName);
            
        }

        public void CreateRegIfNotFound()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software", true))
            {

                key.CreateSubKey("TestMSAS");
                using (RegistryKey key2 = key.OpenSubKey("TestMSAS", true))
                {
                    key2.CreateSubKey("ProgramInfo");
                    using (RegistryKey key3 = key2.OpenSubKey("ProgramInfo", true))
                    {
                        if (key3.GetValue("Name") == null)
                        {
                            key3.SetValue("Name", InvalidShow);
                        }
                    }
                }
            }
        }
    }
}