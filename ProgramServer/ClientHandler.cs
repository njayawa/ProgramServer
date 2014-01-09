using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using ProgramServerMessages;

namespace ProgramServer
{
    //Class to handle each client request separatly
    public class ClientHandler
    {
        TcpClient _clientSocket;
        private string clNo;
        private Queue<TvMessage> writeQueue = new Queue<TvMessage>();
        private object wrObj = new object();

        private void WriteQueuePush(TvMessage msg)
        {
            if (msg != null)
            {
                lock (wrObj)
                {
                    writeQueue.Enqueue(msg);
                }
            }
        }


        private TvMessage WriteQueuePull()
        {
            lock (wrObj)
            {
                if (writeQueue.Count > 0)
                    return writeQueue.Dequeue();
            }
            return null;
        }


        public void StartClient(TcpClient inClientSocket, string clineNo)
        {
            this._clientSocket = inClientSocket;
            this.clNo = clineNo;
            var ctThreadWriter = new Thread(RunSocketReader);
            var ctThreadReader = new Thread(RunSocketWriter);
            ctThreadWriter.Start();
            ctThreadReader.Start();
            _registryWatcher = new RegistryWatcher(KeyValueChanged);
            _registryWatcher.InitializeRegistryWatcher();
            SetInitialChannel();
        }

        private void SetInitialChannel()
        {
            KeyValueChanged(null, null);
        }

        private volatile bool _shouldRun = true;
        bool ShouldRun()
        {
            return _shouldRun && _clientSocket.Connected;
        }

        void StopRunning()
        {
            _shouldRun = false;
        }

        private void RunSocketReader()
        {
            NetworkStream networkStream = _clientSocket.GetStream();
            var ser = new JavaScriptSerializer();
            var sr = new StreamReader(networkStream);
            string str = "";
            try
            {
                while (ShouldRun())
                {
                    str = sr.ReadLine();
                    if (ShouldRun() && !string.IsNullOrEmpty(str))
                    {
                        TvMessage tvMsg = ser.Deserialize<TvMessage>(str);
                        if (tvMsg != null && tvMsg.Message == TvDisplayMessage.Message && tvMsg.DisplayMessage != null)
                        {
                            TvDisplayMessageResponseEventHandler handler = null;
                            if (tvMsg.DisplayMessage.MessageType == "DM")
                            {
                                handler = null;
                            }
                            TvMessageDisplayHandler.Instance.Process(tvMsg.DisplayMessage, handler);
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("ERROR*******************" + ex.ToString());
                Console.Out.WriteLine("String Read: " + str);
                StopRunning();
                _clientSocket.Close();
            }

        }



        private void RunSocketWriter()
        {
            NetworkStream networkStream = _clientSocket.GetStream();
            var sw = new StreamWriter(networkStream) {AutoFlush = true};
            var ser = new JavaScriptSerializer();
            try
            {
                while (ShouldRun())
                {
                    TvMessage msg = WriteQueuePull();
                    if (msg == null)
                    {
                        Thread.Sleep(100); //prevent running hot
                    }
                    else
                    {
                        string s = ser.Serialize(msg);
                        sw.WriteLine(s);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("RunSocketWriter: Caught exception of type " + ex.ToString());
                
                //Console.Out.WriteLine(ex.StackTrace);
                StopRunning();
            }
            finally
            {
                sw.Close();
            }
        }

        private string _lastProgram = null;

        private void KeyValueChanged(object sender, EventArrivedEventArgs e)
        {
            Program p = GetCurrentProgramFromRegistry();
            if (p != null && !p.Title.Equals(_lastProgram))
            {
                _lastProgram = p.Title;
                var msg = MessageCreator.CreateMessage(p);
                WriteQueuePush(msg);
            }
            //TODO: handle null shows
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
            Console.Out.WriteLine("**********ERROR: Unable to find show " + programName);
            return null;
        }

        private RegistryWatcher _registryWatcher;
    }
}
