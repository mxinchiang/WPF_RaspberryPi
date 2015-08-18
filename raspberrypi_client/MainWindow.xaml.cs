using System;
using System.Text;
using System.Windows;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace raspberrypi_client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket c;

        public MainWindow()
        {
            InitializeComponent();
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
            ShowText("连接到服务端...");
            c.Connect(ipe);//连接到服务器
        }

        private void StartListen()
        {
            byte[] buffer = new byte[8192];
            try
            {

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
                ShowText("发送消息到服务端...");
                string sendStr = sendMessage.Text;
                byte[] bs = Encoding.ASCII.GetBytes(sendStr);
                c.Send(bs, bs.Length, 0);

                string recvStr = "";
                byte[] recvBytes = new byte[1024];
                int bytes;
                bytes = c.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                recvStr += Encoding.UTF8.GetString(recvBytes, 0, bytes);

                ShowText("服务器返回信息：" + recvStr);
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

        private void ShowText(string text)
        {
            richTextBox.AppendText(text + "\n");
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            Application.Current.Shutdown();
        }
    }
}
