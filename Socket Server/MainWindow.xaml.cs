using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Socket_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Instance = this;
        }

        // Instance to dispatcher invoke mainwindow
        private MainWindow Instance { get; set; }

        // Flag to start
        private bool flag = false;

        // Max customer
        private int maxCustomer;
        private int countCustomer = 0;

        // List name of customer
        private List<string> name_customer = null;

        // Product list
        private List<string> products = null;

        // Create server
        private Socket server;

        // Set the TcpListener on port 3000
        Int32 port = 3000;
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");

        // Create new playing time
        private DateTime time = new DateTime(1, 1, 1, 0, 1, 0);

        // Call func interval = 1s
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            CountTime_TextBlock.Text = time.ToString("mm:ss");
            time = time.AddSeconds(-1);
        }

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            name_customer = new List<string>();
            products = new List<string>();

            var screen = new MaxCustomerWindow();
            if (screen.ShowDialog() == false) { this.Close(); }

            maxCustomer = screen.maxCustomer;

            StartInfo_TextBlock.Text = localAddr + ":" + port;
            MaxCustomer_TextBlock.Text = maxCustomer.ToString();
            CountTime_TextBlock.Text = "";
            Terminal_TextBox.AppendText("Waiting for  connection...");

            // Path of product file
            string path = AppDomain.CurrentDomain.BaseDirectory + "/product-list.xml";

            // Load XML
            XDocument doc;
            try { doc = XDocument.Load(path); }
            catch (Exception el) { MessageBox.Show(el.Message); return; }

            // Read XML
            var product = from el in doc.Descendants("product")
                          select new
                          {
                              index = el.Attribute("index").Value,
                              name = el.Element("name").Value,
                              price = el.Element("price").Value
                          };

            // Format & add product to list
            foreach (var el in product) { products.Add(el.index + "-" + el.name + "-" + el.price); }

            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            server.Bind(new IPEndPoint(localAddr, port));

            new Thread(() => { Start_Server(); }).Start();
        }

        private void Start_Server()
        {
            server.Listen(maxCustomer);
            Socket_Connect();
        }

        private void Socket_Connect()
        {
            while (true)
            {
                Socket client = server.Accept();
                new Thread(() => { Handle_Socket(client); }).Start();
            }
        }

        private void Handle_Socket(Socket client)
        {
            while (true)
            {
                if (flag == false)
                {
                    flag = true;
                    //dispatcherTimer.Start();
                }

                // Buffer for reading data
                byte[] bytes = new Byte[1024];
                byte[] msg;
                string data = null;
                int i;

                while ((i = client.Receive(bytes)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    break;
                };

                if (countCustomer == maxCustomer)
                {
                    // Handle reject client
                    msg = Encoding.ASCII.GetBytes("FULL CUSTOMER");
                    client.Send(msg);
                    return;
                }
                else if (name_customer.Contains(data))
                {
                    msg = Encoding.ASCII.GetBytes("NAME EXIST");
                    client.Send(msg);
                    do { if ((i = client.Receive(bytes)) != 0) { data = Encoding.ASCII.GetString(bytes, 0, i); } } while (name_customer.Contains(data));
                }

                countCustomer++;
                name_customer.Add(data);
                Instance.Dispatcher.Invoke(() => Terminal_TextBox.AppendText("\nHave a connected"));

                // Send product list to client
                Instance.Dispatcher.Invoke(() => Terminal_TextBox.AppendText("\nSending products to client"));
                foreach (var e in products)
                {
                    msg = Encoding.ASCII.GetBytes(e + "*");
                    client.Send(msg);
                }
                msg = System.Text.Encoding.ASCII.GetBytes("EOF");
                client.Send(msg);

                while ((i = client.Receive(bytes)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    break;
                };

                // Send timer to client
                //msg = System.Text.Encoding.ASCII.GetBytes();
                //client.Send(msg);

                // Listen order from client
                while ((i = client.Receive(bytes)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    break;
                };

                // Add order to list

                //client.Close();
            }
        }



        private void Closing_Window(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure?", "Exit notification", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes) { e.Cancel = false; }
            else { e.Cancel = true; }
        }
    }
}
