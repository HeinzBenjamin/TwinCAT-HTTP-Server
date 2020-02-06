using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{

    /// <summary>
    /// This is the underlying structure of a request. Any call to and from http server and twinCAT client has to adhere to this standard. But not all fields must be filled upon request
    /// For example: the message field is only used if the server wants to send a message. In requests this field is ignored. 'values' must only be filled in write requests
    /// </summary>
    public class TCRequest
    {
        /// <summary>
        /// Can only be either 'read' or 'write'
        /// </summary>
        public string request_type { get; set; }

        /// <summary>
        /// Names of variables to read or write. Don't forget to add POU names in front and add a dot inbetween. For example a variable called myVar defined globally has the name GVL.myVar. If it's defined in MAIN, it's called MAIN.myVar
        /// </summary>
        public string[] names { get; set; }

        /// <summary>
        /// Types must be passed seperately to avoid confusion with signs and size
        /// Currently supported types are:
        /// bool, byte, sint, usint, int, uint, dint, udint, real, lreal, time, date, string (and string(n) for custom length strings)
        /// When arrays of these types are requested for read and write, the array length has to be passed along in square brackets, like so: 'bool[3]', 'real[200]' etc.
        /// Unless otherwise specified the length of 'string' variables is by default limited to 80 characters according to Beckhoff standard.
        /// Longer strings can be passed by adding the string length to the string typename, e.g. 'string144', matching the variable's definition in the TwinCAT project. Use:
        /// Default string item: 'string', default string array: 'string[3]', custom length string item: 'string144', custom length string array 'string144[3]'
        /// More info: https://infosys.beckhoff.com/english.php?content=../content/1033/tcplccontrol/html/tcplcctrl_plc_data_types_overview.htm&id
        /// </summary>
        public string[] types { get; set; }

        /// <summary>
        /// Variable values. If request_type is 'read', this doesn't have to be filled by the requester, but will be filled by TwinCAT
        /// </summary>
        public object[] values { get; set; }

        /// <summary>
        /// Only filled by TwinCATAdsServer to communicate error messages. Otherwise empty
        /// </summary>
        public string message { get; set; }
    }

    /// <summary>
    /// Defines what kind of message is passed to a log text box, so it's coloured accordingly
    /// </summary>
    public enum LogTextCategory { Error, Warning, Info, Incoming, Outgoing };

    /// <summary>
    /// Decides where to display a log message
    /// </summary>
    public enum Recipient { HttpServerTextBox, TwinCatTextBox };

    /// <summary>
    /// Defines how urgent a message is and if it can be ignored
    /// </summary>
    public enum Verbosity { Important, Verbose }


    public class Logging
    {
        public static string NewLineTab = "\n                 ";
        public static event EventHandler<TCSMessageEventArgs> LogMessage;
        public static event EventHandler<TCSStatusUpdateEventArgs> StatusUpdate;

        /// <summary>
        /// Invokes the log message event and passes its content
        /// </summary>
        /// <param name="e"></param>
        public static void SendMessage(TCSMessageEventArgs e)
        {
            EventHandler<TCSMessageEventArgs> handler = LogMessage;
            handler?.Invoke(e.Sender, e);
        }

        /// <summary>
        /// Invokes the UpdateStatus event and passes its content
        /// </summary>
        /// <param name="e"></param>
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
