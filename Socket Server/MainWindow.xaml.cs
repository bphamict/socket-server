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

        // Max customer
        private int maxCustomer;
        private int countCustomer = 0;

        // List name of customer
        private List<string> name_customer = null;

        private class product
        {
            public string index { get; set; }
            public string name { get; set; }
            public string price { get; set; }
            public string priceOrder { get; set; }
            public string nameCustomerOrder { get; set; }
        }

        // Product list
        private List<product> products = null;

        // Create server
        private Socket server;

        // Set the TcpListener on port 3000
        Int32 port = 3000;
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");

        // Create countdown timer
        private DateTime time;

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
            products = new List<product>();

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
            foreach (var el in product)
            {
                products.Add(new product
                {
                    index = el.index,
                    name = el.name,
                    price = el.price
                });
            }

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
            try
            {
                // name of customer
                string nameCS;

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
                    client.Disconnect(true);

                    return;
                }
                else if (name_customer.Contains(data))
                {
                    do
                    {
                        msg = Encoding.ASCII.GetBytes("NAME EXIST");
                        client.Send(msg);

                        while ((i = client.Receive(bytes)) != 0)
                        {
                            data = "";
                            data = Encoding.ASCII.GetString(bytes, 0, i);
                            break;
                        };
                    }
                    while (name_customer.Contains(data));
                }

                countCustomer++;
                nameCS = data;
                name_customer.Add(data);

                // Reset & Start timer
                Instance.Dispatcher.Invoke(() => { time = new DateTime(1, 1, 1, 0, 1, 0); dispatcherTimer.Start(); });

                // Append notification in at terminal
                Instance.Dispatcher.Invoke(() => Terminal_TextBox.AppendText("\nHave a connected"));

                // Send product list to client
                Instance.Dispatcher.Invoke(() => Terminal_TextBox.AppendText("\nSending products to client"));
                foreach (var e in products)
                {
                    msg = Encoding.ASCII.GetBytes(e.index + "-" + e.name + "-" + e.price + "*");
                    client.Send(msg);
                }
                msg = System.Text.Encoding.ASCII.GetBytes("EOF");
                client.Send(msg);

                // Listen order from client
                while ((i = client.Receive(bytes)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    break;
                };

                // Check & add info customer to products list
                Instance.Dispatcher.Invoke(() => Terminal_TextBox.AppendText("\nHave a order"));

                string[] words = data.Split('-');

                foreach (var el in products)
                {
                    if (words[0] == el.index)
                    {
                        if (el.priceOrder == null || (int.Parse(el.priceOrder) < int.Parse(words[1])))
                        {
                            el.priceOrder = words[1];
                            el.nameCustomerOrder = words[2];
                            break;
                        }
                    }
                }

                // Wait until time was up & send result
                while (true)
                {
                    if (time.Second == 0 && time.Minute != 1)
                    {
                        Instance.Dispatcher.Invoke(() => { Terminal_TextBox.AppendText("\nTime was up"); dispatcherTimer.Stop(); });

                        foreach (product p in products)
                        {
                            if (p.priceOrder != "" && p.nameCustomerOrder != "" && nameCS == p.nameCustomerOrder)
                            {
                                msg = Encoding.ASCII.GetBytes("WIN");
                                client.Send(msg);

                                while ((i = client.Receive(bytes)) != 0)
                                {
                                    data = Encoding.ASCII.GetString(bytes, 0, i);
                                    break;
                                };

                                break;
                            }
                        }

                        msg = Encoding.ASCII.GetBytes("LOSE");
                        client.Send(msg);

                        break;
                    }
                }
            }
            catch { Instance.Dispatcher.Invoke(() => Terminal_TextBox.AppendText("\nDisconnected")); }
        }

        private void Closing_Window(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure?", "Exit notification", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes) { server.Disconnect(true); e.Cancel = false; }
            else { e.Cancel = true; }
        }
    }
}
