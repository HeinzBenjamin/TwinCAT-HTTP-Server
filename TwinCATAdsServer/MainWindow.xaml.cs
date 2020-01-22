using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using Newtonsoft.Json;
using Ads.Client;
using Ads.Client.Common;
using Ads.Client.Commands;
using Ads.Client.CommandResponse;
using Ads.Client.Helpers;
using Ads.Client.Special;

using TwinCAT.Ads;


namespace TwinCATAdsServer
{

    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Random rnd = new Random();
        HttpServer ExternalServer = null;
        HttpClient InternalWorkerClient = null;
        //AdsClient AdsClient = null;
        TcIO TcIOClient = null;
        Verbosity ChoosenVerbosity = Verbosity.Verbose;
        Thread HttpThread = null;
        bool WindowRunning = false;

        /// <summary>
        /// Checks if http server is up and running every 200ms and updates the status bar accordingly
        /// </summary>
        void MonitorHttpStatus()
        {
            while (WindowRunning)
            {
                bool isAlive =
                    HttpThread != null && HttpThread.IsAlive && ExternalServer != null && ExternalServer.listener != null && ExternalServer.listener.IsListening && ExternalServer.Running;

                Logging.UpdateStatus(new TCSStatusUpdateEventArgs(isAlive, Recipient.HttpServerTextBox));
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Checks if TwinCATAds client is up and running every 200ms and updates the status bar accordingly
        /// </summary>
        void MonitorTwinCATStatus()
        {
            while (WindowRunning)
            {
                bool isAlive = TcIOClient.IsRunning();
                Logging.UpdateStatus(new TCSStatusUpdateEventArgs(isAlive, Recipient.TwinCatTextBox));
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Main Window. This is the netry point to the program
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            InternalWorkerClient = new HttpClient();
            TcIOClient = new TcIO();
            ExternalServer = new HttpServer(ref TcIOClient);
            Logging.LogMessage += WriteToLog;
            Logging.StatusUpdate += StatusBarUpdate;
            WindowRunning = true;
            TaskCreationOptions opt = TaskCreationOptions.LongRunning;
            Task.Factory.StartNew(new Action(() => { MonitorHttpStatus(); }), opt);
            Task.Factory.StartNew(new Action(() => { MonitorTwinCATStatus(); }), opt);
        }

        /// <summary>
        /// Event that is raised when the main window is closed
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HttpThread?.Abort();
            TcIOClient?.Disconnect();
            WindowRunning = false;
        }

        private void Button_connectTwinCAT_Click(object sender, RoutedEventArgs e)
        {
            if(!TcIOClient.IsRunning())
            {
                int port = int.Parse(this.textBox_tcPort.Text);
                TcIOClient.Connect(textBox_tcHostIn.Text, port);
            }

            else
            {
                TcIOClient.Disconnect();
            }
        }

        private void TcClient_ConnectionStateChanged(object sender, TwinCAT.ConnectionStateChangedEventArgs e)
        {
            TCSMessageEventArgs ev = new TCSMessageEventArgs();
            ev.When = DateTime.Now;
            ev.Message = e.NewState.ToString();
            ev.Category = LogTextCategory.Info;
            ev.Recipient = Recipient.TwinCatTextBox;
            ev.Sender = sender;
            ev.Verbosity = Verbosity.Important;

            Logging.SendMessage(ev);
           
        }

        private void Button_connectHttp_Click(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(this.textBox_httpPort.Text);
            if (ExternalServer == null || ExternalServer.listener == null || !ExternalServer.listener.IsListening || !ExternalServer.Running)
            {
                HttpThread = new Thread(p => { ExternalServer.Start((int)p); });
                HttpThread.Start(port);
            }

            else
            {
                Task.Run(new Action(() =>
                {
                    try
                    {                        
                        //will throw an error, bc it's shutting down the server it actively tries to call
                        InternalWorkerClient.GetAsync(ExternalServer.URL + "btn_shutdown").Wait();
                    }
                    finally
                    {
                        HttpThread.Abort();
                    }
                }));
            }
        }



        

        /// <summary>
        /// this updates a log window by adding text to it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void WriteToLog(object sender, TCSMessageEventArgs args)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (ChoosenVerbosity == Verbosity.Important && args.Verbosity == Verbosity.Verbose)
                    return;

                RichTextBox recipientTextBox =
                    args.Recipient == Recipient.HttpServerTextBox ? richTextBox_logHTTPServer :
                    args.Recipient == Recipient.TwinCatTextBox ? richTextBox_logTwinCAT :
                    null;

                TextRange tr = new TextRange(recipientTextBox.Document.ContentEnd, recipientTextBox.Document.ContentEnd);

                tr.Text = Environment.NewLine + "[" + args.When.ToString("hh-mm-ss.fff") + "] ";
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);

                TextRange tr2 = new TextRange(recipientTextBox.Document.ContentEnd, recipientTextBox.Document.ContentEnd);
                string filler =
                    args.Category == LogTextCategory.Incoming ? "> " :
                    args.Category == LogTextCategory.Outgoing ? "< " :
                    "  ";

                tr2.Text = filler + args.Message;
                var brush =
                    args.Category == LogTextCategory.Error ? Brushes.Red :
                    args.Category == LogTextCategory.Warning ? Brushes.Orange :
                    args.Category == LogTextCategory.Info ? Brushes.LightGray :
                    args.Category == LogTextCategory.Incoming ? Brushes.LightBlue :
                    Brushes.LightGreen;
                tr2.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }));
        }

        public void StatusBarUpdate(object sender, TCSStatusUpdateEventArgs args)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if(args.Recipient == Recipient.HttpServerTextBox)
                {
                    statusBar_http.Background = args.IsAlive ? Brushes.DarkGreen : Brushes.DarkRed;
                    label_statusBar_http.Content = args.IsAlive ? "Listening" : "Not Listening";
                    label_statusBar_http.Foreground = args.IsAlive ? Brushes.ForestGreen : Brushes.IndianRed;
                    button_connectHttp.Content = args.IsAlive ? "Stop" : "Start";
                }

                else if(args.Recipient == Recipient.TwinCatTextBox)
                {
                    statusBar_twinCAT.Background = args.IsAlive ? Brushes.DarkGreen : Brushes.DarkRed;
                    label_statusBar_twinCAT.Content = args.IsAlive ? "Connected" : "Not Connected";
                    label_statusBar_twinCAT.Foreground = args.IsAlive ? Brushes.ForestGreen : Brushes.IndianRed;
                    button_connectTwinCAT.Content = args.IsAlive ? "Disconnect" : "Connect";
                }
            }));
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            ChoosenVerbosity = Verbosity.Verbose;
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ChoosenVerbosity = Verbosity.Important;
        }


        TCSRequest CurrentManRequest = null;
        private void textBox_manReqIn_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CurrentManRequest = JsonConvert.DeserializeObject<TCSRequest>(textBox_manReqIn.Text);
                textBox_manReqIn.Text = JsonConvert.SerializeObject(CurrentManRequest, Formatting.Indented);
                textBox_manReqIn.Background = Brushes.White;
            }
            catch
            {
                CurrentManRequest = null;
                textBox_manReqIn.Background = Brushes.LightSalmon;
            }

        }

        private void button_copyManReqUrl_Click(object sender, RoutedEventArgs e)
        {
            string reqText = "http://localhost: " + this.textBox_httpPort.Text + "?"+ JsonConvert.SerializeObject(CurrentManRequest);
            Clipboard.SetText(reqText);
        }

        private void button_sendManReq_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentManRequest == null)
                return;

                string url = ExternalServer.URL + JsonConvert.SerializeObject(CurrentManRequest);

                CancellationToken token;
                Task.Run(() => {
                    var res = CheckNetworkErrorCallAsync(url, InternalWorkerClient, HttpMethod.Get, token).Result;
                    
                    Dispatcher.Invoke(new Action(() =>
                    {
                        this.textBox_manReqOut.Text = JsonConvert.SerializeObject(res, Formatting.Indented);
                    }));
                });
        }

        private static async Task<TCSRequest> CheckNetworkErrorCallAsync(string url, HttpClient client, HttpMethod method, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(method, url))
            using (var response = await client.SendAsync(request, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TCSRequest>(content);
            }
        }

        private void RichTextBox_logHTTPServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.richTextBox_logHTTPServer.ScrollToEnd();
        }
        private void richTextBox_logTwinCAT_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.richTextBox_logTwinCAT.ScrollToEnd();
        }
    }
}
