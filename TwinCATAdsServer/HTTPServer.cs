// Filename:  HttpServer.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using TwinCATAdsServer;
using Newtonsoft.Json;

namespace TwinCATAdsServer
{


    class HttpServer
    {
        public HttpListener listener;
        public TCSMessageEventArgs LogArgs;
        public TCSStatusUpdateEventArgs StatusArgs;
        public TcIO TcIOClient;
        public string URL { get; set; }
        public bool Running { get; set; }

        public HttpServer(ref TcIO tcIoClient)
        {
            URL = "";
            Running = false;
            LogArgs = new TCSMessageEventArgs();
            LogArgs.Sender = this;
            LogArgs.Recipient = Recipient.HttpServerTextBox;
            StatusArgs = new TCSStatusUpdateEventArgs();
            StatusArgs.Recipient = Recipient.HttpServerTextBox;
            StatusArgs.Sender = this;
            TcIOClient = tcIoClient;
        }

        public void Start(int port)
        {
            // Create a Http server and start listening for incoming connections
            URL = "http://localhost:" + port.ToString() + "/";
            listener = new HttpListener();
            listener.Prefixes.Add(URL);

            LogArgs.Message = string.Format("Listening for connections on {0}", URL);
            LogArgs.Verbosity = Verbosity.Important;
            LogArgs.Category = LogTextCategory.Info;
            LogArgs.When = DateTime.Now;
            Logging.SendMessage(LogArgs);

            string hostName = Dns.GetHostName();
            LogArgs.Message = "Host Name: " + hostName;
            LogArgs.Message += Logging.NewLineTab + "Available IPs: ";

            foreach (IPAddress ip in Dns.GetHostAddresses(hostName))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    LogArgs.Message += Logging.NewLineTab + ip.ToString();
            }

            LogArgs.Verbosity = Verbosity.Verbose;
            Logging.SendMessage(LogArgs);
            Running = true;
            listener.Start();

            StatusArgs.IsAlive = listener.IsListening;
            Logging.UpdateStatus(StatusArgs);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            try
            {
                listenTask.GetAwaiter().GetResult();              
                

                LogArgs.Message = "Shutting down Http Server.";
                LogArgs.Category = LogTextCategory.Info;
                LogArgs.When = DateTime.Now;
                LogArgs.Verbosity = Verbosity.Important;
                Logging.SendMessage(LogArgs);
            }
            catch (Exception e)
            {
                LogArgs.When = DateTime.Now;
                LogArgs.Message = e.ToString();
                LogArgs.Category = LogTextCategory.Error;
                LogArgs.Verbosity = Verbosity.Important;
                Logging.SendMessage(LogArgs);
            }
            finally
            {
                // Close the listener
                Logging.UpdateStatus(StatusArgs);
                StatusArgs.IsAlive = listener.IsListening;
                listener.Close();
            }
        }

        public async Task HandleIncomingConnections()
        {
            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (Running)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();
                LogArgs.When = DateTime.Now;

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                //filter out icon requests, if this is called from a browser
                if (req.Url.AbsolutePath == "/favicon.ico")
                {
                    await resp.OutputStream.WriteAsync(new byte[0], 0, 0);
                    resp.Close();
                    continue;
                }

                //filter out invalid requests
                if (req.HttpMethod != "GET" && req.HttpMethod != "POST")
                {
                    LogArgs.Category = LogTextCategory.Warning;
                    LogArgs.Message = "Invalid request. Neither GET nor POST";
                    Logging.SendMessage(LogArgs);
                    await resp.OutputStream.WriteAsync(new byte[0], 0, 0);
                    resp.Close();
                    continue;
                }                

                //check if this is a shutdown request
                if (req.Url.AbsolutePath == "/btn_shutdown")
                {
                    Running = false;
                    byte[] resBytes = Encoding.UTF8.GetBytes("bye");
                    resp.OutputStream.Write(resBytes, 0, resBytes.Length);
                    break;
                }
                
                
                byte[] data = Encoding.UTF8.GetBytes("{\"response\": \"empty\"}");

                //turn http request into tcsrequest object
                var tcsRequest = JsonConvert.DeserializeObject<TCSRequest>(req.Url.LocalPath.Remove(0, 1));
                var hash = tcsRequest.GetHashCode();

                //notify user
                LogArgs.Message = string.Format("Received {0} request from {1}. (hash: {2})", req.HttpMethod, req.RemoteEndPoint, hash);
                LogArgs.Verbosity = Verbosity.Important;
                LogArgs.Category = LogTextCategory.Incoming;
                Logging.SendMessage(LogArgs);

                //actually process the request using TcIOClient
                var res = TcIOClient.ProcessTCSRequest(tcsRequest);
                LogArgs.When = DateTime.Now;
                var js = JsonConvert.SerializeObject(res);
                data = Encoding.UTF8.GetBytes(js);

                //formulate response
                resp.ContentType = "application/json";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                
                LogArgs.Message = string.Format("Responded with data of size {0} to {1} request. (hash: {2})", resp.ContentLength64, req.HttpMethod, hash);
                LogArgs.Category = LogTextCategory.Outgoing;
                LogArgs.Verbosity = Verbosity.Important;
                Logging.SendMessage(LogArgs);
                
                resp.Close();                
            }
        }
    }
}