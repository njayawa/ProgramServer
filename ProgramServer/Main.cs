using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Management;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using ProgramServerMessages;


namespace ProgramServer
{
    class MainClass
    {
        private static void KeyValueChanged(object sender, EventArrivedEventArgs e)
        {
            Program p = ClientHandler.GetCurrentProgramFromRegistry();

            JavaScriptSerializer ser = new JavaScriptSerializer();
            string s = ser.Serialize(MessageCreator.CreateMessage(p));
            Console.Out.WriteLine(s);

        }

        static void Main(string[] args)
        {
            var serverSocket = new TcpListener(8675);
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
            }

            clientSocket.Close();
            serverSocket.Stop();
            Console.WriteLine(" >> " + "exit");
            Console.ReadLine();
        }
    }



    //Class to handle each client request separatly
    public class ClientHandler
    {
        TcpClient _clientSocket;
        private string clNo;

        public void StartClient(TcpClient inClientSocket, string clineNo)
        {
            this._clientSocket = inClientSocket;
            this.clNo = clineNo;
            var ctThread = new Thread(Run);
            ctThread.Start();
            _registryWatcher = new RegistryWatcher(KeyValueChanged);
        }

        private void Run()
        {
            int requestCount = 0;
            byte[] bytesFrom = new byte[10025];
            string dataFromClient = null;
            Byte[] sendBytes = null;
            string serverResponse = null;
            string rCount = null;
            requestCount = 0;
            _registryWatcher.InitializeRegistryWatcher();
            NetworkStream networkStream = _clientSocket.GetStream();

            while ((true))
            {
                try
                {
                    requestCount = requestCount + 1;

                    //networkStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                    //dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
                    //dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                    //Console.WriteLine(" >> " + "From client-" + clNo + dataFromClient);

                    //rCount = Convert.ToString(requestCount);
                    //serverResponse = "Server to clinet(" + clNo + ") " + rCount;
                    serverResponse = "sample text\n";
                    sendBytes = Encoding.ASCII.GetBytes(serverResponse);
                    networkStream.Write(sendBytes, 0, sendBytes.Length);
                    networkStream.Flush();
                    Console.WriteLine(" >> " + serverResponse);
                    Thread.Sleep(30000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" >> " + ex.ToString());
                }
            }
        }

        private void KeyValueChanged(object sender, EventArrivedEventArgs e)
        {
            var programName = (string)Registry.GetValue(RegistryWatcher.FullPath,
                RegistryWatcher.ValueName,
                RegistryWatcher.InvalidShow);
            Console.Out.WriteLine("Found program {0}", programName);

        }

        public static Program GetCurrentProgramFromRegistry()
        {
            var programName = (string)Registry.GetValue(RegistryWatcher.FullPath,
                RegistryWatcher.ValueName,
                RegistryWatcher.InvalidShow);

            Console.Out.WriteLine("Found program {0}", programName);
            using (var ent = new xmlTVDBEntities())
            {
                DateTime now = DateTime.Now;
                Program prog = null;
                var query = from p in ent.Programs
                            where p.Title.Contains(programName) && (p.StartTime <= now && p.EndTime >= now)
                            select p;
                foreach (Program pn in query)
                {
                    if (prog == null)
                        prog = pn;
                    Console.Out.WriteLine("Program {0} - {1} - {2}\n{3} - {4}", pn.Id, pn.Title, pn.SubTitle, pn.StartTime, pn.EndTime);
                }
                return prog;
            }
            return null;
        }

        private RegistryWatcher _registryWatcher;
    }
}
