using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Management;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using NativeWifi;
using ProgramServerMessages;


namespace ProgramServer
{
    class MainClass
    {
        private static void KeyValueChanged(object sender, EventArrivedEventArgs e)
        {
            Program p = ClientHandler.GetCurrentProgramFromRegistry();
            if (p != null)
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                string s = ser.Serialize(MessageCreator.CreateMessage(p));
                Console.Out.WriteLine(s);
            }
            else
            {
                Console.Out.WriteLine("ERROR****************: Found unknown program ");
            }

        }

        private static string _ssidStr;
        private static string GetSSIDName()
        {
            var wlan = new WlanClient();
            Collection<String> connectedSsids = new Collection<string>();
            Console.Out.WriteLine(LocalIPAddress());
            foreach (WlanClient.WlanInterface wlanInterface in wlan.Interfaces)
            {
                Wlan.Dot11Ssid ssid = wlanInterface.CurrentConnection.wlanAssociationAttributes.dot11Ssid;
                connectedSsids.Add(new String(Encoding.ASCII.GetChars(ssid.SSID, 0, (int)ssid.SSIDLength)));
                //wlanInterface.CurrentConnection.wlanAssociationAttributes
            }


            String strHostName = string.Empty;
            // Getting Ip address of local machine...
            // First get the host name of local machine.
            strHostName = Dns.GetHostName();
            Console.WriteLine("Local Machine's Host Name: " + strHostName);
            // Then using host name, get the IP address list..
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;

            for (int i = 0; i < addr.Length; i++)
            {
                Console.WriteLine("IP Address {0}: {1} ", i, addr[i].ToString());
            }
            //Console.ReadLine();
            if(connectedSsids.Count > 0)
                return connectedSsids[0];
            return null;
        }


        public static string LocalIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 )//|| ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    Console.WriteLine(ni.Name);
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            Console.WriteLine(ip.Address.ToString());
                        }
                    }
                }
            }


            IPAddress ipAddress = Dns.Resolve("localhost").AddressList[0];
            Console.Out.WriteLine(ipAddress.ToString());
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        public static Dictionary<int, ClientHandler> s_clientdictionary = new Dictionary<int, ClientHandler>();

        static void Main(string[] args)
        {
            var ssid = GetSSIDName();
            var serverSocket = new TcpListener(8675);
            EndPoint ep = serverSocket.LocalEndpoint;
            var clientSocket = default(TcpClient);
            int counter = 0;
            var watcher = new RegistryWatcher(KeyValueChanged);
            watcher.InitializeRegistryWatcher();

            serverSocket.Start();
            Console.WriteLine(" >> " + "Server Started");

            counter = 0;
            while (true)
            {
                counter += 1;
                clientSocket = serverSocket.AcceptTcpClient();
                Console.WriteLine(" >> " + "Client No:" + Convert.ToString(counter) + " started!");
                ClientHandler client = new ClientHandler();
                client.StartClient(clientSocket, Convert.ToString(counter));
                s_clientdictionary.Add(counter, client);
            }

            clientSocket.Close();
            serverSocket.Stop();
            Console.WriteLine(" >> " + "exit");
            Console.ReadLine();
        }
    }




}
