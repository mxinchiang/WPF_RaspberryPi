using System;
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
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace raspberrypi_client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket c;
        private Thread Listen;
        int num = 0;
        ObservableDataSource<Point> source1 = null;
        ObservableDataSource<Point> source2 = null;
        ObservableDataSource<Point> source3 = null;
        ObservableDataSource<Point> source4 = null;
        ObservableDataSource<Point> source5 = null;

        private LineGraph graphT = new LineGraph();
        private LineGraph graphH = new LineGraph();
        private LineGraph graphD = new LineGraph();
        private LineGraph graphP = new LineGraph();
        private LineGraph graphACC = new LineGraph();

        Thread water;
        Thread acc;
        bool first_w = true;
        bool first_a = true;

        DispatcherTimer timer = new DispatcherTimer();

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

            source5 = new ObservableDataSource<Point>();
            source5.SetXYMapping(p => p);

            // Add all three graphs. Colors are not specified and chosen random
            graphT = plotterT.AddLineGraph(source1, Colors.Red, 2, "TEMP");
            graphH = plotterH.AddLineGraph(source2, Colors.Green, 2, "HUM");
            graphD = plotterD.AddLineGraph(source3, Colors.Blue, 2, "DUST");
            graphP = plotterP.AddLineGraph(source4, Colors.DarkSeaGreen, 2, "PRESS");
            graphACC = plotterACC.AddLineGraph(source5, Colors.CadetBlue, 2, "ACC");

            //timer.Tick += new EventHandler(timer_Tick);//Alarm Signal lamp flicker
            //timer.Interval = new TimeSpan(1000);
        }

        private delegate void ShowAlarm(string text);
        private delegate void WriteDelegate(string str);

        private void showalarm(string text)
        {
            if (text.Contains("water_red"))
            {
                label_water.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (text.Contains("water_green"))
            {
                label_water.Foreground = new SolidColorBrush(Colors.Green);
            }
            if (text.Contains("acc_red"))
            {
                label_acc.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (text.Contains("acc_green"))
            {
                label_acc.Foreground = new SolidColorBrush(Colors.Green);
            }
            //Storyboard story = (Storyboard)this.FindResource("Storyboard");
            //BeginStoryboard(story);
        }

        private void ShowText(string text)
        {
            richTextBox.UndoLimit = 50;
            richTextBox.AppendText(text + "\n");
            richTextBox.ScrollToEnd();
        }

        public void SetLabel(string recvStr)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            string[] values = recvStr.Split(' ');
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                T_l.Dispatcher.Invoke(new Action(() =>
                {
                    T_l.Content = values[1];
                    H_l.Content = values[2];
                    D_l.Content = values[3];
                    P_l.Content = values[4];
                    ACC_l.Content = Math.Round(Math.Sqrt(double.Parse(values[5], culture) * double.Parse(values[5], culture) +
                                  double.Parse(values[6], culture) * double.Parse(values[6], culture) +
                                  double.Parse(values[7], culture) * double.Parse(values[7], culture)), 2).ToString();
                    east.Content = values[8];
                    north.Content = values[9];//Math.Round(((double.Parse(values[6]) - 50) / 10),2).ToString();
                }));
            }
            else
            {

            }
        }

        private void warming_water()
        {
            while (true)
            {
                //waitCallCarHandler.WaitOne();
                label_water.Dispatcher.Invoke(new ShowAlarm(showalarm), "water_red");
                Thread.Sleep(500);
                label_water.Dispatcher.Invoke(new ShowAlarm(showalarm), "water_green");
                Thread.Sleep(500);
            }
        }

        private void warming_acc()
        {
            while (true)
            {
                label_acc.Dispatcher.Invoke(new ShowAlarm(showalarm), "acc_red");
                Thread.Sleep(500);
                label_acc.Dispatcher.Invoke(new ShowAlarm(showalarm), "acc_green");
                Thread.Sleep(500);
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (timer.IsEnabled == true)
            {
                label_acc.Dispatcher.Invoke(new ShowAlarm(showalarm), "acc_red");
                Thread.Sleep(500);
                label_acc.Dispatcher.Invoke(new ShowAlarm(showalarm), "acc_green");
                Thread.Sleep(500);
            }
        }

        private void InitClient()
        {
            //try
            //{
                int port = Convert.ToInt32(xport.Text);
                string host = xip.Text;
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = null;
                ipe = new IPEndPoint(ip, port);
                c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//Create a Socket
                c.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "Connect to Server...");
                c.Connect(ipe);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
            Listen = new Thread(new ThreadStart(this.StartListen));
            Listen.IsBackground = true;
            Listen.Start();
        }

        private void DrawingLines(string recvStr)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string[] values = recvStr.Split(' ');
            values[0] = (num++).ToString();

            double x = double.Parse(values[0], culture);
            double y1 = double.Parse(values[1], culture);
            double y2 = double.Parse(values[2], culture);
            double y3 = double.Parse(values[3], culture);
            double y4 = double.Parse(values[4], culture);
            double y5 = Math.Round(Math.Sqrt(double.Parse(values[5], culture) * double.Parse(values[5], culture) +
                                  double.Parse(values[6], culture) * double.Parse(values[6], culture) +
                                  double.Parse(values[7], culture) * double.Parse(values[7], culture)),2);//Math.Round((double.Parse(values[6], culture)-50)/10,2);

            Point p1 = new Point(x, y1);
            Point p2 = new Point(x, y2);
            Point p3 = new Point(x, y3);
            Point p4 = new Point(x, y4);
            Point p5 = new Point(x, y5);

            source1.AppendAsync(Dispatcher, p1);
            source2.AppendAsync(Dispatcher, p2);
            source3.AppendAsync(Dispatcher, p3);
            source4.AppendAsync(Dispatcher, p4);
            source5.AppendAsync(Dispatcher, p5);
        }

        private void OnNewMessageReceived(string msg)
        {
            msg = msg.Replace("\0", "");
            if (msg.Contains("Alarm water"))
            {
                richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "Recv from Server：" + msg + "\n");
                //label_water.Dispatcher.Invoke(new ShowAlarm(showalarm), msg);
                if (first_w == true)
                {
                    first_w = false;
                    water = new Thread(warming_water);
                    water.Start();
                }
            }
            else if (msg.Contains("Alarm acc"))
            {
                richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "Recv from Server：" + msg + "\n");
                //label_acc.Dispatcher.Invoke(new ShowAlarm(showalarm), msg);
                timer.IsEnabled = true;
                if (first_a == true)
                {
                    first_a = false;
                    acc = new Thread(warming_acc);
                    acc.Start();
                }
            }
            else if (msg.Length >= 15)
            {
                string[] values = msg.Split(' ');
                double y1 = double.Parse(values[1]);        //temp
                double y2 = double.Parse(values[2]);        //humi
                double y3 = double.Parse(values[3]);        //dust
                double y4 = double.Parse(values[4]);        //press
                double y5 = double.Parse(values[5]);        //acc_x
                double y6 = double.Parse(values[6]);        //acc_y
                double y7 = double.Parse(values[7]);        //acc_z
                double y8 = double.Parse(values[8]);        //gps_e
                double y9 = double.Parse(values[9]);        //gps_n
                richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), msg);//temperature, humidity, dust, pressure, acceleration
                DrawingLines(msg);
                SetLabel(msg);
            }
        }

        private void StartListen()
        {
            //try
            //{
            StringBuilder sb = new StringBuilder();             //这个是用来保存：接收到了的，但是还没有结束的消息蒋明欣
            string terminateString = "\n";
            byte[] recvBytes = new byte[2048];//64
            int bytes = 0;
            num = 0;
            while (true)
            {
                bytes = c.Receive(recvBytes, recvBytes.Length, 0);//从服务器端接受返回信息
                string rawMsg = Encoding.Default.GetString(recvBytes, 0, recvBytes.Length);
                //richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "Recv from Server：" + rawMsg + "\n");
                int rnFixLength = terminateString.Length;         //这个是指消息结束符的长度，此处为\n
                for (int i = 0; i < rawMsg.Length;)               //遍历接收到的整个buffer文本
                {
                    if (i <= rawMsg.Length - rnFixLength)
                    {
                        if (rawMsg.Substring(i, rnFixLength) != terminateString)//非消息结束符，则加入sb
                        {
                            sb.Append(rawMsg[i]);
                            i++;
                        }
                        else
                        {
                            string a = sb.ToString();
                            //richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "Recv from Server：" + sb.ToString() + "\n");
                            OnNewMessageReceived(sb.ToString());//找到了消息结束符，触发消息接收完成事件
                            sb.Clear();
                            i += rnFixLength;
                        }
                    }
                    else
                    {
                        sb.Append(rawMsg[i]);
                        i++;
                    }
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
                plotterT.Children.Remove(graphT);
                plotterH.Children.Remove(graphH);
                plotterD.Children.Remove(graphD);
                plotterP.Children.Remove(graphP);
                plotterACC.Children.Remove(graphACC);

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

                source5 = new ObservableDataSource<Point>();
                source5.SetXYMapping(p => p);

                // Add all three graphs. Colors are not specified and chosen random
                graphT = plotterT.AddLineGraph(source1, Colors.Red, 2, "TEMP");
                graphH = plotterH.AddLineGraph(source2, Colors.Green, 2, "HUM");
                graphD = plotterD.AddLineGraph(source3, Colors.Blue, 2, "DUST");
                graphP = plotterP.AddLineGraph(source4, Colors.DarkSeaGreen, 2, "PRESS");
                graphACC = plotterACC.AddLineGraph(source5, Colors.CadetBlue, 2, "ACC");

                InitClient();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void send_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(comboBox.Text);
            try
            {
                string sendStr = "change" + comboBox.Text + " " + sendMessage.Text + "\0";
                byte[] bs = Encoding.ASCII.GetBytes(sendStr);
                c.Send(bs, bs.Length, 0);
                richTextBox.Dispatcher.Invoke(new WriteDelegate(ShowText), "<--------- " + comboBox.Text + " interval is changed to " + sendMessage.Text + "s");
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

                System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
                dlg.Description = "选择要保存日志文件的路径";
                System.Windows.Interop.HwndSource source = PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;
                System.Windows.Forms.IWin32Window win = new OldWindow(source.Handle);
                System.Windows.Forms.DialogResult result = dlg.ShowDialog(win);
                string localpath = dlg.SelectedPath;

                string filename = year.Text + "-" + mon.Text + "-" + day.Text + ".csv";
                FtpUpDown ftp = new FtpUpDown(xip.Text, "pi", "raspberry");
                bool bol = ftp.Download(localpath, filename, out errorinfo);
                if (bol == true)
                    MessageBox.Show("Download success");
                else
                    MessageBox.Show("Download fail：" + errorinfo + "");
            }
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

        private void reset_Click(object sender, RoutedEventArgs e)
        {
            first_w = true;
            first_a = true;
            try
            {
                acc.Abort();
                water.Abort();
            }
            catch { }
            label_water.Foreground = new SolidColorBrush(Colors.Green);
            label_acc.Foreground = new SolidColorBrush(Colors.Green);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
                string url = "ftp://" + ftpServerIP + "//home/pi/LogFiles/" + fileName;
                Connect(url);
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                long cl = response.ContentLength;
                int bufferSize = 4096;
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

    public class OldWindow : System.Windows.Forms.IWin32Window
    {
        IntPtr _handle;
        public OldWindow(IntPtr handle)
        {
            _handle = handle;
        }
#region IWin32Window Members
        IntPtr System.Windows.Forms.IWin32Window.Handle
        {
            get { return _handle; }
        }
#endregion
    }
}