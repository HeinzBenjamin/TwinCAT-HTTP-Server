using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT;
using TwinCAT.Ads;
using TwinCATAdsServer;

namespace TwinCATAdsServer
{
    public class TcIO
    {
        //global members
        TcAdsClient TcClient = null;
        TCSMessageEventArgs LogArgs;
        TCSStatusUpdateEventArgs StatusArgs;

        /// <summary>
        /// Default constructor. Instantiates TcAdsClient and logging arguments and status update arguments
        /// </summary>
        public TcIO()
        {
            TcClient = new TcAdsClient();
            LogArgs = new TCSMessageEventArgs();
            LogArgs.Recipient = Recipient.TwinCatTextBox;
            LogArgs.Sender = this;
            StatusArgs = new TCSStatusUpdateEventArgs();
            StatusArgs.Recipient = Recipient.TwinCatTextBox;
            StatusArgs.Sender = this;
        }

        /// <summary>
        /// Connects to TwinCAT Ads via TcAdsClient.Connect. Hooks up Ads events to logging text box
        /// </summary>
        /// <param name="amsNetId">As defined in TwinCAT Project (in Project > System > Routes > Project Routes). Something like 192.168.0.1.1.1 </param>
        /// <param name="port">As defined in TwinCAT. Normally 851 or 852</param>
        public void Connect(string amsNetId, int port)
        {
            try
            {
                if (TcClient == null)
                    TcClient = new TcAdsClient();

                TcClient.ConnectionStateChanged += TcClient_ConnectionStateChanged;
                TcClient.AdsNotification += TcClient_AdsNotification;
                TcClient.AdsNotificationError += TcClient_AdsNotificationError;
                TcClient.AdsNotificationEx += TcClient_AdsNotificationEx;
                TcClient.AdsStateChanged += TcClient_AdsStateChanged;
                TcClient.AdsSymbolVersionChanged += TcClient_AdsSymbolVersionChanged;
                TcClient.AmsRouterNotification += TcClient_AmsRouterNotification;

                AmsNetId id = new AmsNetId(amsNetId);
                TcClient.Connect(id, port);
            }
            catch(Exception e)
            {
                Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Error, string.Format("Could not connect to ADS Server: {0}", e));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Instance is valid and connected to TwinCAT</returns>
        public bool IsRunning()
        {
            return TcClient != null && TcClient.IsConnected;
        }

        /// <summary>
        /// Disconnects client and disposes all resources affiliate with it
        /// </summary>
        public void Disconnect()
        {
            TcClient?.Disconnect();
            TcClient?.Dispose();
            TcClient = null;
        }

        #region notification stuff

        private void TcClient_AmsRouterNotification(object sender, AmsRouterNotificationEventArgs e)
        {
            Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Info, string.Format("AMS Router notification {0}", e.State), Verbosity.Verbose);
        }

        private void TcClient_AdsSymbolVersionChanged(object sender, EventArgs e)
        {
            Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Info, string.Format("Ads Symbol Version changed."), Verbosity.Verbose);
        }

        private void TcClient_AdsStateChanged(object sender, AdsStateChangedEventArgs e)
        {
            Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Info, string.Format("Ads State changed to{0}AdsState: {1}{0}Device State: {2}", Logging.NewLineTab, e.State.AdsState, e.State.DeviceState));
        }

        private void TcClient_AdsNotificationEx(object sender, AdsNotificationExEventArgs e)
        {
            Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Info,
                string.Format("Received Extended Ads Notification: {0}", e.ToString()));
        }

        private void TcClient_AdsNotificationError(object sender, AdsNotificationErrorEventArgs e)
        {
            Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Error,
                string.Format("Ads Notification Error: {0}", e.ToString()));
        }

        private void TcClient_AdsNotification(object sender, AdsNotificationEventArgs e)
        {
            Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Info,
                string.Format("Received Ads Notification: {0}", e.ToString()));
        }

        private void TcClient_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Info,
                string.Format("TwinCAT Client Connection State Change from {0} to {1}. Reason: {2}", e.OldState, e.NewState, e.Reason));
        }

        private void Send_TcClient_EventHandling(DateTime when, LogTextCategory category, string message, Verbosity verbosity = Verbosity.Important)
        {
            LogArgs.When = when;
            LogArgs.Category = category;
            LogArgs.Message = message;
            LogArgs.Verbosity = verbosity;
            Logging.SendMessage(LogArgs);
            StatusArgs.IsAlive = TcClient != null && TcClient.IsConnected;
            Logging.UpdateStatus(StatusArgs);
        }
        #endregion

        /// <summary>
        /// Processes incoming requests of request_type 'read' or 'write'
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Same as incoming request but with fields for 'values' (in case of read request) and / or 'message' (in case of error) filled.</returns>
        public TCSRequest ProcessTCSRequest(TCSRequest request)
        {
            var hash = request.GetHashCode();
            try
            {
                if (request.names.Length != request.types.Length || request.names.Length == 0 || (request.request_type != "read" && request.request_type != "write"))
                {
                    request.message = "Invalid request! Length of names and types must be equal and larger than zero. Request type must be either 'read' or 'write'";
                    Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Error, request.message, Verbosity.Important);
                    return request;
                }

                
                Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Incoming, string.Format("Received ADS {0} request for {1} items (hash: {2})", request.request_type, request.names.Length, hash));

                //read request
                if (request.request_type == "read")
                {
                    request.values = new object[request.names.Length];
                    for (int i = 0; i < request.names.Length; i++)
                    {
                        if (!request.types[i].EndsWith("]") || request.types[i].StartsWith("string"))
                            request.values[i] = ReadItem(request.names[i], request.types[i]);
                        else
                        {
                            var length = int.Parse(request.types[i].Split('[')[1].Split(']')[0]);
                            request.values[i] = ReadArray(request.names[i], request.types[i], length);
                        }
                    }
                }

                //write request
                else
                {
                    for (int i = 0; i < request.names.Length; i++)
                    {
                        if (!request.types[i].EndsWith("]") || request.types[i].StartsWith("string"))
                            WriteItem(request.names[i], request.types[i], request.values[i]);
                        else
                        {
                            var length = int.Parse(request.types[i].Split('[')[1].Split(']')[0]);
                            var arr = (request.values[i] as Newtonsoft.Json.Linq.JArray).ToObject<object[]>();
                            if (arr.Length != length)
                                throw new Exception(
                                    string.Format(
                                        "Write request for {0}: Array length in 'values' ({1}) doesn't match the indicated array length in 'types' property ({2}). " +
                                        "Make sure they are the same\nFor example if your item is an integer array of length three ('values' = [[2, 4, 1]]) " +
                                        "make sure to indicate the correct length in 'types' = ['int[3]']", request.names[i], arr.Length, length));

                            WriteArray(request.names[i], request.types[i], arr);
                        }
                    }
                }
                

                Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Outgoing, string.Format("Responded to ADS {0} request for {1} items (hash: {2})", request.request_type, request.names.Length, hash));
            }
            catch (Exception e)
            {
                request.message = string.Format("TwinCATAds Server error: {0}. Please refer to TwinCATAdsServer log for more info.",e.Message);
                Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Error,
                    string.Format("Exception occured during processing of request:{0}Request hash: {1}Exception: {2}", Logging.NewLineTab, hash, e.ToString()));
            }
            return request;
        }

        /// <summary>
        /// Read singular item from TwinCAT variables
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ReadItem(string name, string type)
        {
            return ReadArray(name, type, 1)[0];
        }

        /// <summary>
        /// Read item array from TwinCAT Variables
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="arrLength"></param>
        /// <returns></returns>
        public object[] ReadArray(string name, string type, int arrLength)
        {
            object[] values = new object[arrLength];

            int streamLength = StreamLength(type);

            using (AdsStream dataStream = new AdsStream(arrLength * streamLength))
            using (AdsBinaryReader reader = new AdsBinaryReader(dataStream))
            {
                int varHandle = TcClient.CreateVariableHandle(name);
                TcClient.Read(varHandle, dataStream);
                dataStream.Position = 0;

                for (int i = 0; i < arrLength; i++)
                {
                    values[i] = ReadObjectFromReader(reader, type);
                }

                TcClient.DeleteVariableHandle(varHandle);
            }

            return values;
        }

        /// <summary>
        /// Write singular item to TwinCAT Variables
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void WriteItem(string name, string type, object value)
        {
            WriteArray(name, type, new object[1] { value });
        }

        /// <summary>
        /// Write item array to TwinCAT Variables
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        public void WriteArray(string name, string type, object[] values)
        {
            int arrLength = values.Length;
            int streamLength = StreamLength(values[0]);

            using (AdsStream dataStream = new AdsStream(arrLength * streamLength))
            using (AdsBinaryWriter writer = new AdsBinaryWriter(dataStream))
            {
                int varHandle = TcClient.CreateVariableHandle(name);
                dataStream.Position = 0;

                foreach(object val in values)
                {
                    WriteObjectToWriter(writer, type, val);
                    if (val is String) dataStream.Position += 81 - val.ToString().Length;

                }
                TcClient.Write(varHandle, dataStream);
                writer.Flush();
            }
        }

        /*
        public static string ReadStruct(int varHandle, List<string> types, int[] size, ref TcAdsClient tcClient, out List<List<object>> output)
        {
            output = new List<List<object>>();
            try
            {
                string message = "";
                int streamLength = 0;
                for (int i = 0; i < size.Length; i++) streamLength += StreamLength(types[i]) * size[i];

                AdsStream dataStream = new AdsStream(streamLength);
                AdsBinaryReader reader = new AdsBinaryReader(dataStream);
                for (int i = 0; i < size.Length; i++)
                {
                    List<object> o = new List<object>();
                    for (int j = 0; j < size[i]; j++)
                    {
                        object obj = new object();
                        if (!ReadObject(reader, types[i], out obj))
                            message = String.Format("Error while reading " + types[i] + " at struct position (i, j): (" + i + ", " + j + ")");
                        o.Add(obj);
                    }
                    output.Add(o);
                }

                return message;
            }
            catch (Exception e)
            {
                return e.ToString();
            }

        }

        public static string WriteStruct(int varHandle, ref TcAdsClient tcClient, object[][] values)
        {
            try
            {
                int dataStreamLength = 0;
                foreach (object[] branch in values)
                {
                    foreach (object obj in branch)
                    {
                        dataStreamLength += StreamLength(obj);
                    }
                }

                AdsStream dataStream = new AdsStream(dataStreamLength);
                AdsBinaryWriter writer = new AdsBinaryWriter(dataStream);


                dataStream.Position = 0;
                string message = "";

                foreach (object[] branch in values)
                {
                    foreach (object obj in branch)
                    {
                        if (!WriteObject(writer, obj)) message = String.Format("Error while writing object: " + obj.ToString() + " in branch: " + branch.ToString());
                        else if (obj is String) dataStream.Position += 81 - obj.ToString().Length;
                    }
                }

                tcClient.Write(varHandle, dataStream);
                writer.Flush();
                return message;
            }

            catch (Exception e)
            {
                return e.ToString();
            }
        }
        */

        /// <summary>
        /// Internal util function that maps arbitrary object read actions to the corresponding AdsBinaryReader read function
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private object ReadObjectFromReader(AdsBinaryReader reader, string typeName)
        {
            object value = "";
            if (typeName == "bool") value = reader.ReadBoolean();
            else if (typeName == "byte") value = reader.ReadByte();
            else if (typeName == "sint") value = reader.ReadInt16();
            else if (typeName == "usint") value = reader.ReadUInt16();
            else if (typeName == "int") value = reader.ReadInt32();
            else if (typeName == "uint") value = reader.ReadUInt32();
            else if (typeName == "dint") value = reader.ReadInt64();
            else if (typeName == "udint") value = reader.ReadUInt64();
            else if (typeName == "real") value = reader.ReadSingle();
            else if (typeName == "lreal") value = reader.ReadDouble();
            else if (typeName == "time") value = reader.ReadPlcTIME();
            else if (typeName == "date") value = reader.ReadPlcDATE();
            else if (typeName.StartsWith("string")){
                var length = 0;
                try
                {
                    length = int.Parse(typeName.Split('[')[1].Split(']')[0]); }
                catch
                {
                    throw new Exception("Could not read requested string length. For types of string please supply the requested number of characters. For example like so 'string[8]'");
                }
                value = reader.ReadPlcAnsiString(length); }

            else
            {
                Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Error,
                    string.Format("Can not read from AdsBinaryReader. Data type '{0}' not supported.", typeName));
            }
            return value;
        }

        /// <summary>
        /// Internal util function that maps arbitrary object write actions to the corresponding AdsBinaryWriter write function
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        private void WriteObjectToWriter(AdsBinaryWriter writer, string typeName, object value)
        {
            if (typeName == "bool") writer.Write((bool)value);
            else if (typeName == "byte") writer.Write((byte)value);
            else if (typeName == "sint") writer.Write((Int16)value);
            else if (typeName == "int") writer.Write((Int32)value);
            else if (typeName == "dint") writer.Write((Int64)value);
            else if (typeName == "usint") writer.Write((UInt16)value);
            else if (typeName == "uint") writer.Write((UInt32)value);
            else if (typeName == "udint") writer.Write((UInt64)value);
            else if (typeName == "real") writer.Write((Single)value);
            else if (typeName == "lreal") writer.Write((Double)value);                       
            else if (typeName == "time") writer.WritePlcType((TimeSpan)value);
            else if (typeName == "date") writer.WritePlcType((DateTime)value);
            else if (typeName.StartsWith("string")) writer.WritePlcAnsiString(value.ToString(), value.ToString().Length);
            else
            {
                Send_TcClient_EventHandling(DateTime.Now, LogTextCategory.Error,
                   string.Format("Can not write to AdsBinaryReader. Data type '{0}' not supported", typeName));
            }
        }

        /// <summary>
        /// Internal util function that determins lengths of stream reads based on object type. Not very elegant. Can probably be simplified. Strings are a special case
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Stream length in byte</returns>
        private static int StreamLength(object value)
        {
            int streamLength = 0;
            if (value is Boolean) streamLength = 1;
            else if (value is Byte) streamLength = 1;
            else if (value is Int16) streamLength = 2;
            else if (value is Int32) streamLength = 4;
            else if (value is Int64) streamLength = 8;
            else if (value is UInt16) streamLength = 2;
            else if (value is UInt32) streamLength = 4;
            else if (value is UInt64) streamLength = 8;
            else if (value is Single) streamLength = 4;
            else if (value is Double) streamLength = 8;
            else if (value is String) streamLength = 81;
            else if (value is TimeSpan) streamLength = 4;
            else if (value is DateTime) streamLength = 4;

            return streamLength;
        }

        /// <summary>
        /// Internal util function that determins lengths of stream reads based on type name. Not very elegant. Can probably be simplified. Strings are a special case
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Stream length in byte</returns>
        private static int StreamLength(string typeName)
        {
            ///CONTINUE HERE
            int streamLength = 0;
            if (typeName == "bool") streamLength = 1;
            else if (typeName == "byte") streamLength = 1;
            else if (typeName == "sint" || typeName == "usint") streamLength = 2;
            else if (typeName == "int" || typeName == "uint") streamLength = 4;
            else if (typeName == "dint" || typeName == "udint") streamLength = 8;
            else if (typeName == "real") streamLength = 4;
            else if (typeName == "lreal") streamLength = 8;
            else if (typeName == "string") streamLength = 81;
            else if (typeName == "time") streamLength = 4;
            else if (typeName == "date") streamLength = 4;
            else return -1;

            return streamLength;
        }
    }
}