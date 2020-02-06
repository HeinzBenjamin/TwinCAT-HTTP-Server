using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace Common
{
    /// <summary>
    /// A simple http server class to receive and handle http POST and GET requests
    /// </summary>
    public class HttpServer
    {
        public HttpListener listener;
        public TCSMessageEventArgs LogArgs;
        public TCSStatusUpdateEventArgs StatusArgs;
        public TcIO TcIOClient;
        public IDisposable AspNetSession;
        public string URL { get; set; }

        public int Port { get; set; }
        public bool Running { get; set; }

        public HttpServer(ref TcIO tcIoClient)
        {
            URL = "";
            Port = -1;
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
            // Create a internal http server and start listening for incoming requests
            Port = port;
            URL = "http://localhost:" + port.ToString() + "/";
            listener = new HttpListener();
            listener.Prefixes.Add(URL);

            
            Running = true;
            try
            {
                listener.Start();
                
            }
            catch(Exception e)
            {
                LogArgs.When = DateTime.Now;
                LogArgs.Message = string.Format("Couldn't start http listener. Are you running this as Administrator?{0}{1}", Logging.NewLineTab, e.ToString());
                LogArgs.Verbosity = Verbosity.Important;
                LogArgs.Category = LogTextCategory.Error;
                Logging.SendMessage(LogArgs);
            }

            StatusArgs.IsAlive = listener.IsListening;
            Logging.UpdateStatus(StatusArgs);

            // Handle requests
            Task listenTask = HandleIncomingRequests();
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
                LogArgs.Message = string.Format("Critical error! Http Server needs to be shut down. Try restarting it.{0}{1}", Logging.NewLineTab, e.ToString());
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

        public async Task HandleIncomingRequests()
        {
            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (Running)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                LogArgs.When = DateTime.Now;

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                resp.ContentType = "application/json";
                resp.ContentEncoding = Encoding.UTF8;

                try
                {
                    //check if this is a shutdown request
                    if (req.Url.AbsolutePath == "/btn_shutdown")
                    {
                        Running = false;
                        byte[] resBytes = Encoding.UTF8.GetBytes("bye");
                        resp.OutputStream.Write(resBytes, 0, resBytes.Length);
                        break;
                    }

                    //turn http request into tcsrequest object
                    var tcRequest = JsonConvert.DeserializeObject<TCRequest>(req.Url.LocalPath.Remove(0, 1));

                    var hash = tcRequest.GetHashCode();

                    //notify user
                    LogArgs.Message = string.Format("Received {0} request from {1}. (hash: {2})", req.HttpMethod, req.RemoteEndPoint, hash);
                    LogArgs.Verbosity = Verbosity.Important;
                    LogArgs.Category = LogTextCategory.Incoming;
                    Logging.SendMessage(LogArgs);

                    //actually process the request using TcIOClient
                    var res = TcIOClient.ProcessTCRequest(tcRequest);
                    LogArgs.When = DateTime.Now;
                    var js = JsonConvert.SerializeObject(res);
                    byte[] data = Encoding.UTF8.GetBytes(js);

                    //formulate response

                    resp.ContentLength64 = data.LongLength;

                    await resp.OutputStream.WriteAsync(data, 0, data.Length);

                    LogArgs.Message = string.Format("Responded with data of size {0} to {1} request. (hash: {2})", resp.ContentLength64, req.HttpMethod, hash);
                    LogArgs.Category = LogTextCategory.Outgoing;
                    LogArgs.Verbosity = Verbosity.Important;
                    Logging.SendMessage(LogArgs);

                    resp.Close();
                }
                catch (Exception e)
                {
                    var tcNull = new TCRequest();
                    tcNull.message = string.Format("Error in Http Server: {0} - Please refer to TwinCATHttpServer log for more information.", e.Message);

                    LogArgs.Message = string.Format("Error in Http Server:{0}{1}{0}{2}", Logging.NewLineTab, e.Message, e.ToString());
                    LogArgs.Category = LogTextCategory.Error;
                    LogArgs.Verbosity = Verbosity.Important;
                    Logging.SendMessage(LogArgs);

                    var js = JsonConvert.SerializeObject(tcNull);
                    byte[] data = Encoding.UTF8.GetBytes(js);
                    resp.ContentLength64 = data.LongLength;

                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
            }
        }

        static Dictionary<string, object> NvcToDictionary(NameValueCollection nvc, bool handleMultipleValuesPerKey)
        {
            var result = new Dictionary<string, object>();
            foreach (string key in nvc.Keys)
            {
                if (handleMultipleValuesPerKey)
                {
                    string[] values = nvc.GetValues(key);
                    if (values.Length == 1)
                    {
                        result.Add(key, values[0]);
                    }
                    else
                    {
                        result.Add(key, values);
                    }
                }
                else
                {
                    result.Add(key, nvc[key]);
                }
            }

            return result;
        }
    }
}