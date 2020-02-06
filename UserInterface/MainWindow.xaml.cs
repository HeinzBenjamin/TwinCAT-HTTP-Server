using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using WebAPI;
using Common;

namespace UserInterface
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HttpServer InternalServer = null;
        HttpClient InternalWorkerClient = null;

        IWebHost AspHost = null;

        public TcIO TcIOClient = null;
        Verbosity ChoosenVerbosity = Verbosity.Verbose;
        Thread AspThread = null;
        Thread HttpThread = null;
        bool WindowRunning = false;
        Dictionary<string, string> ExampleDict = null;


        /// <summary>
        /// Checks if http server is up and running every 200ms and updates the status bar accordingly
        /// </summary>
        private void MonitorHttpStatus()
        {
            
            while (WindowRunning)
            {
                bool isAlive =
                    HttpThread != null &&
                    HttpThread.IsAlive &&
                    AspThread != null &&
                    AspThread.IsAlive &&
                    InternalServer != null &&
                    InternalServer.listener != null &&
                    InternalServer.listener.IsListening &&
                    InternalServer.Running;

                Logging.UpdateStatus(new TCSStatusUpdateEventArgs(isAlive, Recipient.HttpServerTextBox));
                Thread.Sleep(200);
            }
            
        }

        /// <summary>
        /// Checks if TwinCATAds client is up and running every 200ms and updates the status bar accordingly
        /// </summary>
        private void MonitorTwinCATStatus()
        {
            while (WindowRunning)
            {
                bool isAlive = TcIOClient.IsRunning();
                Logging.UpdateStatus(new TCSStatusUpdateEventArgs(isAlive, Recipient.TwinCatTextBox));
                Thread.Sleep(500);
            }
        }


        /// <summary>
        /// Main Window. This is the entry point to the program
        /// </summary>
        public MainWindow()
        {

            InitializeComponent();

            InternalWorkerClient = new HttpClient();
            TcIOClient = new TcIO();
            InternalServer = new HttpServer(ref TcIOClient);
            Logging.LogMessage += WriteToLog;
            Logging.StatusUpdate += StatusBarUpdate;
            WindowRunning = true;
            TaskCreationOptions opt = TaskCreationOptions.LongRunning;
            Task.Factory.StartNew(new Action(() => { MonitorHttpStatus(); }), opt);
            Task.Factory.StartNew(new Action(() => { MonitorTwinCATStatus(); }), opt);

            ExampleDict = new Dictionary<string, string>();
            ExampleDict.Add(" Read Items & Arrays", "RequestExamples/example1_read_items_arrays.json");
            ExampleDict.Add("Write Items & Arrays", "RequestExamples/example2_write_items_arrays.json");
            comboBox_examples.ItemsSource = ExampleDict.Keys;
        }

        /// <summary>
        /// Event that is raised when the main window is closed
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HttpThread?.Abort();
            AspThread?.Abort();
            TcIOClient?.Disconnect();
            WindowRunning = false;
        }

        /// <summary>
        /// Triggered when TwinCAT Connect button is hit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_connectTwinCAT_Click(object sender, RoutedEventArgs e)
        {
            if (!TcIOClient.IsRunning())
            {
                int port = int.Parse(this.textBox_tcPort.Text);
                TcIOClient.Connect(textBox_tcHostIn.Text, port);
            }

            else
            {
                TcIOClient.Disconnect();
            }
        }

        /// <summary>
        /// Triggered when http connect button is hit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_connectHttp_Click(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(this.textBox_httpPort.Text);

            if(AspThread == null)
            {
                var tmpLogArgs = new TCSMessageEventArgs();
                tmpLogArgs.Message = string.Format("Starting ASP.NET WebAPI to listen for incoming requests.");
                tmpLogArgs.Verbosity = Verbosity.Important;
                tmpLogArgs.Category = LogTextCategory.Info;
                tmpLogArgs.Recipient = Recipient.HttpServerTextBox;
                tmpLogArgs.When = DateTime.Now;
                Logging.SendMessage(tmpLogArgs);

                AspThread = new Thread(p => {
                    AspHost = WebAPISession.RunAsp((int)p);
                    AspHost.Run();                     
                });
                AspThread.Start(port);


                tmpLogArgs.When = DateTime.Now;
                string hostName = Dns.GetHostName();
                tmpLogArgs.Message = "..Done! Available request routes: ";
                tmpLogArgs.Message += Logging.NewLineTab + "http://"+hostName+":" + port + "/twincat/";
                tmpLogArgs.Message += Logging.NewLineTab + "http://localhost:" + port + "/twincat/";
                tmpLogArgs.Message += Logging.NewLineTab + "http://127.0.0.1:" + port + "/twincat/";

                foreach (IPAddress ip in Dns.GetHostAddresses(hostName))
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        tmpLogArgs.Message += Logging.NewLineTab + "http://"+ip.ToString() + ":" + port + "/twincat/";
                }

                tmpLogArgs.Verbosity = Verbosity.Verbose;
                Logging.SendMessage(tmpLogArgs);
            }

            else
            {
                AspHost?.StopAsync().Wait();
                AspThread?.Abort();
                AspThread = null;
            }
            

            if (InternalServer == null || InternalServer.listener == null || !InternalServer.listener.IsListening || !InternalServer.Running)
            {
                HttpThread = new Thread(p => { InternalServer.Start((int)p); });
                HttpThread.Start(port + 1);
            }

            else
            {
                Task.Run(new Action(() =>
                {
                    try
                    {
                        //will throw an error, bc it's shutting down the server it actively tries to call
                        InternalWorkerClient.GetAsync("http://localhost:" + InternalServer.Port.ToString() + "/btn_shutdown").Wait();
                    }
                    finally
                    {
                        HttpThread?.Abort();
                    }

                }));
            }
            
        }

        /// <summary>
        /// Updates a log window by adding text to it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void WriteToLog(object sender, TCSMessageEventArgs args)
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

        /// <summary>
        /// Updates the green or red status bars at the bottom of each log text bos and the text on 'Connect' buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void StatusBarUpdate(object sender, TCSStatusUpdateEventArgs args)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (args.Recipient == Recipient.HttpServerTextBox)
                {
                    statusBar_http.Background = args.IsAlive ? Brushes.DarkGreen : Brushes.DarkRed;
                    label_statusBar_http.Content = args.IsAlive ? "Listening" : "Not Listening";
                    label_statusBar_http.Foreground = args.IsAlive ? Brushes.ForestGreen : Brushes.IndianRed;
                    button_connectHttp.Content = args.IsAlive ? "Stop" : "Start";
                }

                else if (args.Recipient == Recipient.TwinCatTextBox)
                {
                    statusBar_twinCAT.Background = args.IsAlive ? Brushes.DarkGreen : Brushes.DarkRed;
                    label_statusBar_twinCAT.Content = args.IsAlive ? "Connected" : "Not Connected";
                    label_statusBar_twinCAT.Foreground = args.IsAlive ? Brushes.ForestGreen : Brushes.IndianRed;
                    button_connectTwinCAT.Content = args.IsAlive ? "Disconnect" : "Connect";
                }
            }));
        }

        TCRequest CurrentManRequest = null;

        /// <summary>
        /// Reformats text of manual request input box into json format whenever it is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_manReqIn_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (sender as TextBox);
            try
            {                
                var currentManRequest = JsonConvert.DeserializeObject<TCRequest>(textBox.Text);
                textBox.Text = JsonConvert.SerializeObject(currentManRequest, Formatting.Indented);
                textBox.Background = Brushes.White;
                if (textBox.Name == "textBox_manReqIn")
                    CurrentManRequest = currentManRequest;
            }
            catch
            {
                textBox.Background = Brushes.LightSalmon;
            }
        }

        /// <summary>
        /// Copies manual request into valid URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_copyManReqUrl_Click(object sender, RoutedEventArgs e)
        {
            string reqText = "http://localhost:" + textBox_httpPort.Text + "/twincat?request=" + System.Net.WebUtility.UrlEncode(JsonConvert.SerializeObject(CurrentManRequest));
            Clipboard.SetText(reqText);
        }

        /// <summary>
        /// Send manual request to http server where it is processed further
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_sendManReq_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentManRequest == null)
                return;

            Task.Run(() => {
                var ctrl = new WebAPI.Controllers.TwinCATController();
                var res = ctrl.Get(CurrentManRequest).Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
                

                Dispatcher.Invoke(new Action(() =>
                {
                    
                    this.textBox_manReqOut.Text = res.Value.ToString();
                }));
            });
        }

        /// <summary>
        /// Makes the actual http request to the http server
        /// </summary>
        /// <param name="url"></param>
        /// <param name="client"></param>
        /// <param name="method"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<TCRequest> SendHttpRequestAsync(string url, HttpClient client, HttpMethod method, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(method, url))
            using (var response = await client.SendAsync(request, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TCRequest>(content);
            }
        }

        //Some util
        private void RichTextBox_logHTTPServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.richTextBox_logHTTPServer.ScrollToEnd();
        }

        private void richTextBox_logTwinCAT_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.richTextBox_logTwinCAT.ScrollToEnd();
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            ChoosenVerbosity = Verbosity.Verbose;
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ChoosenVerbosity = Verbosity.Important;
        }

        private void comboBox_examples_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string exampleFile = ExampleDict[e.AddedItems[0].ToString()];
                using (StreamReader r = new StreamReader(exampleFile))
                {
                    string text = r.ReadToEnd();
                    textBox_manReqIn.Text = text;
                }
            }
            catch
            {
                MessageBox.Show(string.Format("Example file not found. Make sure that the folder 'RequestExamples' exists in the directory of this application and contains the following file:\n\r{0}", ExampleDict[e.AddedItems[0].ToString()]), "Example File Missing");
            }
        }
    }
}
