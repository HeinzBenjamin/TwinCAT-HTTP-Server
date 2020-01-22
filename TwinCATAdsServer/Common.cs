using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwinCATAdsServer
{

    public class TCSRequest
    {
        public string request_type { get; set; }
        public string[] names { get; set; }

        public string[] types { get; set; }

        public object[] values { get; set; }

        public string message { get; set; }
    }

    public enum LogTextCategory { Error, Warning, Info, Incoming, Outgoing };
    public enum Recipient { HttpServerTextBox, TwinCatTextBox};
    public enum Verbosity { Important, Verbose }
    

    class Logging
    {
        public static string NewLineTab = "\n                 ";
        public static event EventHandler<TCSMessageEventArgs> LogMessage;
        public static event EventHandler<TCSStatusUpdateEventArgs> StatusUpdate;

        public static void SendMessage(TCSMessageEventArgs e)
        {
            EventHandler<TCSMessageEventArgs> handler = LogMessage;
            handler?.Invoke(e.Sender, e);            
        }

        public static void UpdateStatus(TCSStatusUpdateEventArgs e)
        {
            EventHandler<TCSStatusUpdateEventArgs> handler = StatusUpdate;
            handler?.Invoke(e.Sender, e);
         }

    }
    public class TCSMessageEventArgs : EventArgs
    {
        public object Sender;
        public Verbosity Verbosity;
        public Recipient Recipient;
        public DateTime When;
        public string Message;
        public LogTextCategory Category;
    }

    public class TCSStatusUpdateEventArgs : EventArgs
    {
        public object Sender;
        public Recipient Recipient;
        public bool IsAlive;

        public TCSStatusUpdateEventArgs() { }
        public TCSStatusUpdateEventArgs(bool isAlive, Recipient recipient)
        {
            Sender = null;
            Recipient = recipient;
            IsAlive = isAlive;
        }
    }
}
