﻿using System;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Globalization;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.IO;

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
            ipe = new IPEndPoint(ip, port);
            c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//Create a Socket
            c.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "Connect to Server...");
            c.Connect(ipe);
            Listen = new Thread(new ThreadStart(this.StartListen));
            Listen.IsBackground = true;
            Listen.Start();
        }

        private void DrawingLines(string recvStr)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            int num = 0;
            string[] values = recvStr.Split(' ');//new string[4];//line.Split(',');
            values[0] = (num++).ToString();

            double x = Double.Parse(values[0], culture);
            double y1 = Double.Parse(values[1], culture);
            double y2 = Double.Parse(values[2], culture);
            double y3 = Double.Parse(values[3], culture);
            double y4 = Double.Parse(values[4], culture);

            Point p1 = new Point(x, y1);
            Point p2 = new Point(x, y2);
            Point p3 = new Point(x, y3);
            Point p4 = new Point(x, y4);

            source1.AppendAsync(Dispatcher, p1);
            source2.AppendAsync(Dispatcher, p2);
            source3.AppendAsync(Dispatcher, p3);
            source4.AppendAsync(Dispatcher, p4);
        }

        private void StartListen()
        {
            //CultureInfo culture = CultureInfo.InvariantCulture;
            //Assembly executingAssembly = Assembly.GetExecutingAssembly();
            //try
            //{
            string recvStr = "";
            byte[] recvBytes = new byte[64];
            int bytes = 0;
            int num = 0;
            while (true)
            {
                bytes = c.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                if (bytes > 0)
                {
                    recvStr += Encoding.UTF8.GetString(recvBytes, 0, bytes).TrimEnd('\n');
                    richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "Recv from Server：" + recvStr);
                    DrawingLines(recvStr);
                    //string[] values = recvStr.Split(' ');//new string[4];//line.Split(',');
                    //values[0] = (num++).ToString();

                    //double x = Double.Parse(values[0], culture);
                    //double y1 = Double.Parse(values[1], culture);
                    //double y2 = Double.Parse(values[2], culture);
                    //double y3 = Double.Parse(values[3], culture);
                    //double y4 = Double.Parse(values[4], culture);

                    //Point p1 = new Point(x, y1);
                    //Point p2 = new Point(x, y2);
                    //Point p3 = new Point(x, y3);
                    //Point p4 = new Point(x, y4);

                    //source1.AppendAsync(Dispatcher, p1);
                    //source2.AppendAsync(Dispatcher, p2);
                    //source3.AppendAsync(Dispatcher, p3);
                    //source4.AppendAsync(Dispatcher, p4);

                    recvStr = "";
                }
                Array.Clear(recvBytes, 0, recvBytes.Length);
            }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
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
                richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "Send to Server: " + sendMessage.Text);
                string sendStr = sendMessage.Text;
                byte[] bs = Encoding.ASCII.GetBytes(sendStr);
                c.Send(bs, bs.Length, 0);
                sendMessage.Clear();
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

        private void download_Click(object sender, RoutedEventArgs e)
        {
            if (year.Text.Length == 0 || mon.Text.Length == 0 || day.Text.Length == 0) MessageBox.Show("Date information is empty!");
            else
            {
                string errorinfo;
                string localpath = @"C:\Users\jiang\Desktop\raspberrypi_client";
                string filename = year.Text + "-" + mon.Text + "-" + day.Text + ".txt";
                FtpUpDown ftp = new FtpUpDown(xip.Text, "pi", "raspberry");
                bool bol = ftp.Download(localpath, filename, out errorinfo);
                if (bol == true)
                    MessageBox.Show("Download success");
                else
                    MessageBox.Show("Download fail：" + errorinfo + "");
            }
        }

        private delegate void WriteDelegate(string str);
        private void ShowText(string text)
        {
            richTextBox.AppendText(text + "\n");
            richTextBox.ScrollToEnd();

        }


        private void close_Click(object sender, RoutedEventArgs e)
        {
            if (Listen != null)
            {
                c.Close();
                if (Listen.ThreadState == ThreadState.Running)
                {
                    Listen.Abort();
                    //Self_send.Abort();
                }
            }
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            Application.Current.Shutdown();
        }
    }

    public class FtpUpDown
    {
        string ftpServerIP;
        string ftpUserID;
        string ftpPassword;
        FtpWebRequest reqFTP;//FTP requset
        private void Connect(String path)//Connect ftp
        {
            // Create FtpWebRequest objects based uri
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(path));
            // Specify the type of data transfer
            reqFTP.UseBinary = true;
            reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
        }
        public FtpUpDown(string ftpServerIP, string ftpUserID, string ftpPassword)
        {
            this.ftpServerIP = ftpServerIP;
            this.ftpUserID = ftpUserID;
            this.ftpPassword = ftpPassword;
        }

        //DownLoad the file
        public bool Download(string localpath, string fileName, out string errorinfo)
        {
            try
            {
                String onlyFileName = Path.GetFileName(fileName);
                string newFileName = localpath + "\\" + onlyFileName;

                if (File.Exists(newFileName))
                {
                    errorinfo = string.Format("Local File {0} already exists, can not be downloaded", newFileName);
                    return false;
                }
                string url = "ftp://" + ftpServerIP + "//home/pi/mxin/TestFile/" + fileName;
                Connect(url); 
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                long cl = response.ContentLength;
                int bufferSize = 2048;
                int readCount;
                byte[] buffer = new byte[bufferSize];
                readCount = ftpStream.Read(buffer, 0, bufferSize);

                FileStream outputStream = new FileStream(newFileName, FileMode.Create);
                while (readCount > 0)
                {
                    outputStream.Write(buffer, 0, readCount);
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                }
                ftpStream.Close();
                outputStream.Close();
                response.Close();
                errorinfo = "";
                return true;
            }
            catch (Exception ex)
            {
                errorinfo = string.Format("Because {0} , can not be downloaded", ex.Message);
                return false;
            }
        }

    }
}
