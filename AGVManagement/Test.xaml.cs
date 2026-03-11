using AGVManagement.MapPaint;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace AGVManagement
{
    /// <summary>
    /// Test.xaml 的交互逻辑
    /// </summary>
    public partial class Test : Window
    {
        private IMqttClient mqttClient;
        private bool isUpKeyPressed = false;
        private bool isDownKeyPressed = false;
        private bool isLeftKeyPressed = false;
        private bool isRightKeyPressed = false;
        private DispatcherTimer timer;

        public Test()
        {
            InitializeComponent();
        }

        private void btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            // 初始化 MQTT 客户端
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            // 设置 MQTT 连接选项
            var options = new  MqttClientOptionsBuilder()
            .WithTcpServer("192.168.137.66", 1883) // MQTT 服务器地址和端口号
            .Build();

            // 连接到 MQTT 代理
            mqttClient.ConnectAsync(options);

            // 显示连接成功消息
            MessageBox.Show("MQTT 连接成功！");

            string filePath = @"E:\AGVCode\RTT\init_path\init_path1.txt";
            List<Point> points = Painting.ReadPointsFromFile(filePath);
            string json = Painting.GeneratePointDataJson(points);
            mqttClient.PublishAsync("AGV/Carrier/MapLine", json).Wait();


            //// 开始定时器
            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromMilliseconds(100);
            //timer.Tick += Timer_Tick;
            //timer.Start();



            //// 监听键盘按键状态
            //Keyboard.AddKeyDownHandler(this, OnKeyDown);
            //Keyboard.AddKeyUpHandler(this, OnKeyUp);
        }

        //double value1 = 1;
        //double value2 = 1;


        //private async void Timer_Tick(object sender, EventArgs e)
        //{
        //    if (mqttClient.IsConnected)
        //    {
        //        if (isUpKeyPressed)
        //        {
        //            await mqttClient.PublishAsync("turtle/cmd_vel", "0.5, 0");
        //        }
        //        else if (isDownKeyPressed)
        //        {
        //            await mqttClient.PublishAsync("turtle/cmd_vel", "-0.5, 0");
        //        }
        //        else if (isLeftKeyPressed)
        //        {
        //            await mqttClient.PublishAsync("turtle/cmd_vel", "0, 3");
        //        }
        //        else if (isRightKeyPressed)
        //        {
        //            await mqttClient.PublishAsync("turtle/cmd_vel", "0, 0");
        //        }
        //    }
        //}

        //private void OnKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Up)
        //    {
        //        isUpKeyPressed = true;
        //    }
        //    else if (e.Key == Key.Down)
        //    {
        //        isDownKeyPressed = true;
        //    }
        //    else if (e.Key == Key.Left)
        //    {
        //        isLeftKeyPressed = true;
        //    }
        //    else if (e.Key == Key.Right)
        //    {
        //        isRightKeyPressed = true;
        //    }
        //}

        //private void OnKeyUp(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Up)
        //    {
        //        isUpKeyPressed = false;
        //    }
        //    else if (e.Key == Key.Down)
        //    {
        //        isDownKeyPressed = false;
        //    }
        //    else if (e.Key == Key.Left)
        //    {
        //        isLeftKeyPressed = false;
        //    }
        //    else if (e.Key == Key.Right)
        //    {
        //        isRightKeyPressed = false;
        //    }
        //}

    }
}
