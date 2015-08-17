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
        private int localport;
        private Thread Listen;
        private TcpListener tcpListener;
        private static string message = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartListen()
        {
            byte[] buffer = new byte[8192];
            message = "";
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, localport);//tcpListener = new TcpListener(ipLocalEndPoint);//
                tcpListener.Start();
                while (true)
                {
                    TcpClient tcpclient = tcpListener.AcceptTcpClient();
                    NetworkStream streamToClient = tcpclient.GetStream();
                    int bytesRead = streamToClient.Read(buffer, 0, 8192);
                    message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    richTextBox.AppendText(message);
                    //richTextBox.ScrollToCaret();
                }
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
                TcpClient client = new TcpClient();
                IPAddress ip1 = IPAddress.Parse(ip.Text);//{ 111, 186, 100, 46 }
                client.Connect(ip1, Convert.ToInt32(port.Text));
                localport = Convert.ToInt32(((IPEndPoint)client.Client.LocalEndPoint).Port.ToString());
                //if (this.tcpListener != null)
                //{
                //    tcpListener.Stop();
                //}
                //if (Listen != null)
                //{
                //    Listen.Abort();
                //    Listen = null;
                //}
                //if (Listen == null)
                //{
                //    Listen = new Thread(new ThreadStart(this.StartListen));
                //    Listen.IsBackground = true;
                //    Listen.Start();
                //}
                Stream streamToServer = client.GetStream();
                //lock (streamToServer)
                //{
                    byte[] buffer = Encoding.UTF8.GetBytes(sendMessage.Text.ToCharArray());
                    streamToServer.Write(buffer, 0, buffer.Length);
                    streamToServer.Flush();
                    streamToServer.Close();
                    client.Close();
                    richTextBox.AppendText(sendMessage.Text);
                    sendMessage.Clear();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            if (this.tcpListener != null)
            {
                tcpListener.Stop();
            }
            if (Listen != null)
            {
                if (Listen.ThreadState == ThreadState.Running)
                {
                    Listen.Abort();
                }
            }
            Application.Current.Shutdown();
        }
    }
}
