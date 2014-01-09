using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProgramServerMessages;

namespace ProgramServer
{
    public delegate void TvDisplayMessageResponseEventHandler(TvDisplayMessage msg, string response);

    public class TvMessageDisplayHandler
    {
        private static TvMessageDisplayHandler s_handler;

        public class TvMessageDisplayAndHandler
        {
            public TvDisplayMessage Message { get; set; }
            public TvDisplayMessageResponseEventHandler Handler { get; set; }
        }
        private Queue<TvMessageDisplayAndHandler> writeQueue = new Queue<TvMessageDisplayAndHandler>();

        private object wrObj = new object();
        private static object sObj = new object();

        private TvMessageDisplayHandler(){}

        public static TvMessageDisplayHandler Instance
        {
            get
            {
                lock(sObj)
                {
                    if (s_handler == null)
                    {
                        s_handler = new TvMessageDisplayHandler();
                        new Thread(s_handler.Looper).Start();
                    }
                }
                return s_handler;
            }
        }

        public void Process(TvDisplayMessage msg, TvDisplayMessageResponseEventHandler handler)
        {
            var msgHnad = new TvMessageDisplayAndHandler {Message = msg, Handler = handler};

            WriteQueuePush(msgHnad);
        }

        private void WriteQueuePush(TvMessageDisplayAndHandler msg)
        {
            if (msg != null)
            {
                lock (wrObj)
                {
                    writeQueue.Enqueue(msg);
                }
            }
        }


        private TvMessageDisplayAndHandler WriteQueuePull()
        {
            lock (wrObj)
            {
                if (writeQueue.Count > 0)
                    return writeQueue.Dequeue();
            }
            return null;
        }

        private void Looper()
        {
            try
            {
                while (true)
                {
                    TvMessageDisplayAndHandler msg = WriteQueuePull();
                    if (msg == null)
                    {
                        Thread.Sleep(100); //prevent running hot
                    }
                    else
                    {
                        PeekTvMessage(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("TvDisplayMessage::Looper: Caught exception of type " + ex.ToString());
            }
            finally
            {
            }
        }

        private void PeekTvMessage(TvMessageDisplayAndHandler msgAndHandler)
        {
            try
            {
                TvDisplayMessage tvMsg = msgAndHandler.Message;   
                string sUrlsUrlFormat = "http://localhost:40510/notboxrich  \" \" {0} \"{1}\" \"{2}\"";
                string message = " ";
                string duration = "5";

                if (tvMsg != null)
                {
                    if (tvMsg.Text != null)
                        message = tvMsg.Text;
                    if (tvMsg.DurationSeconds != null)
                        duration = tvMsg.DurationSeconds;


                }
                string url = string.Format(sUrlsUrlFormat, duration, message, "c:%5Cpics%5Ctwitter.png");
                WebRequest wrGETURL;
                wrGETURL = WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse) wrGETURL.GetResponse();
                Stream receiveStream = response.GetResponseStream();
                // Pipes the stream to a higher level stream reader with the required encoding format. 
                StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                string respStr = readStream.ReadToEnd();
                respStr = respStr.ToLower();
                int idx = -1;
                idx = respStr.IndexOf("<pre>response=");
                if (idx == 0)
                {
                    if (respStr.Substring(idx + "<pre>response=".Length).StartsWith(message.ToLower()))
                    {
                        HandleTvMessage(msgAndHandler);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("***Error in PeekTvMessage " + ex.ToString());
            }
        }



        private void HandleTvMessage(TvMessageDisplayAndHandler msg)
        {
            TvDisplayMessage tvMsg = msg.Message;
            string sUrlsUrlFormat = "http://localhost:40510/msgboxrich  \"{0}\" \"{1}\" {2} \"{3}\" \"nonmodal\" \"{4}\"";
            string caption = " ";
            string message = " ";
            string duration = "5";
            string buttons = "OK";
            string picture = WebUtility.UrlEncode(@"c:\pics\twitter.png");
            //"c:%5Cpics%5Ctwitter.png";

           

            if (tvMsg != null)
            {
                if (tvMsg.Caption != null)
                    caption = tvMsg.Caption;
                if (tvMsg.Text != null)
                    message = tvMsg.Text;
                if (tvMsg.DurationSeconds != null)
                    duration = tvMsg.DurationSeconds;
                if (tvMsg.Buttons != null)
                {
                    buttons = "";
                    for (int i = 0; i < tvMsg.Buttons.Length; i++)
                    {
                        if (i != 0)
                            buttons += ";";
                        buttons += tvMsg.Buttons[i];
                    }
                }
                    
            }
            string url = string.Format(sUrlsUrlFormat, caption, message, duration, buttons);
            WebRequest wrGETURL;
            wrGETURL = WebRequest.Create(url);
            var response = (HttpWebResponse)wrGETURL.GetResponse();
            if (msg.Handler != null)
            {
                Stream receiveStream = response.GetResponseStream();
                // Pipes the stream to a higher level stream reader with the required encoding format. 
                var readStream = new StreamReader(receiveStream, Encoding.UTF8);
                string respStr = readStream.ReadToEnd();
                respStr = respStr.ToLower();
                int idx = -1;
                idx = respStr.IndexOf("<pre>response=");
                if (idx == 0)
                {
                    respStr = respStr.Substring(idx + "<pre>response=".Length);
                    idx = respStr.IndexOf("\\r\\n");
                    if (idx > 0)
                    {
                        respStr = respStr.Substring(0, idx);
                        msg.Handler(msg.Message, respStr);
                    }
                }
            }

        }
    }
}
