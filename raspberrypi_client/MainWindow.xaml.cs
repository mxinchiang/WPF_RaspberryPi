using System;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Globalization;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;

namespace raspberrypi_client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket c;
        private Thread Listen;
        ObservableDataSource<Point> source1 = null;
        ObservableDataSource<Point> source2 = null;
        ObservableDataSource<Point> source3 = null;
        ObservableDataSource<Point> source4 = null;

        public MainWindow()
        {
            InitializeComponent();

            // Create first source
            source1 = new ObservableDataSource<Point>();
            // Set identity mapping of point in collection to point on plot
            source1.SetXYMapping(p => p);

            source2 = new ObservableDataSource<Point>();
            source2.SetXYMapping(p => p);

            source3 = new ObservableDataSource<Point>();
            source3.SetXYMapping(p => p);

            source4 = new ObservableDataSource<Point>();
            source4.SetXYMapping(p => p);

            // Add all three graphs. Colors are not specified and chosen random
            plotter.AddLineGraph(source1, 2, "Data row 1");
            plotter.AddLineGraph(source2, 2, "Data row 2");
            plotter.AddLineGraph(source3, 2, "Data row 3");
            plotter.AddLineGraph(source4, 2, "Data row 4");

        }

        private void InitClient()
        {
            int port = Convert.ToInt32(xport.Text);
            string host = xip.Text;
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = null;
            //foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            //{
            //    if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
            //    {
            //        ip = _IPAddress;
            //        ipe = new IPEndPoint(ip, port);//把ip和端口转化为IPEndPoint实例
            //    }
            //}
            ipe = new IPEndPoint(ip, port);//把ip和端口转化为IPEndPoint实例
            c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建一个Socket
            this.richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "连接到服务端...");
            c.Connect(ipe);//连接到服务器
            Listen = new Thread(new ThreadStart(this.StartListen));
            Listen.IsBackground = true;
            Listen.Start();
        }

        private void StartListen()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            try
            {
                string recvStr = "";
                byte[] recvBytes = new byte[64];
                int bytes = 0;
                int num = 0;
                while (true)
                {
                    bytes = c.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                    if (bytes >= 0)
                    {
                        recvStr += Encoding.Default.GetString(recvBytes, 0, bytes).Replace("\0", "");
                        this.richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "服务器返回信息：" + recvStr);
                        string line = recvStr.Replace("  "," ");
                        string[] values = recvStr.Split(' ');//new string[4];//line.Split(',');
                        values[0] = (num++).ToString();

                        double x = Double.Parse(values[0], culture);
                        double y1 = Double.Parse(values[2], culture);
                        double y2 = Double.Parse(values[4], culture);
                        double y3 = Double.Parse(values[6], culture);
                        double y4 = Double.Parse(values[8], culture);

                        Point p1 = new Point(x, y1);
                        Point p2 = new Point(x, y2);
                        Point p3 = new Point(x, y3);
                        Point p4 = new Point(x, y4);

                        source1.AppendAsync(Dispatcher, p1);
                        source2.AppendAsync(Dispatcher, p2);
                        source3.AppendAsync(Dispatcher, p3);
                        source4.AppendAsync(Dispatcher, p4);

                        recvStr = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InitClient();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "发送消息到服务端 "+ sendMessage.Text);
                string sendStr = sendMessage.Text;
                byte[] bs = Encoding.ASCII.GetBytes(sendStr);
                c.Send(bs, bs.Length, 0);
                sendMessage.Clear();

                //string recvStr = "";
                //byte[] recvBytes = new byte[1024];
                //int bytes;
                //bytes = c.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                //recvStr += Encoding.UTF8.GetString(recvBytes, 0, bytes);

                //ShowText("服务器返回信息：" + recvStr);
            }
            catch (ArgumentNullException ex1)
            {
                Console.WriteLine("ArgumentNullException:{0}", ex1);
            }
            catch (SocketException ex2)
            {
                Console.WriteLine("SocketException:{0}", ex2);
            }
        }

        private delegate void WriteDelegate(string str);
        private void ShowText(string text)
        {
            this.richTextBox.AppendText(text + "\n");

        }


        private void close_Click(object sender, RoutedEventArgs e)
        {
            c.Close();
            if (Listen != null)
            {
                if (Listen.ThreadState == ThreadState.Running)
                {
                    Listen.Abort();
                }
            }
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            Application.Current.Shutdown();
        }
    }
}
