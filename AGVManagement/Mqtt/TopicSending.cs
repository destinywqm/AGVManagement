//using MQTTnet.Client;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Input;
//using MQTTnet;
//namespace AGVManagement.Mqtt
//{
//    public class TopicSending
//    {
//        private const string Topic = "AGV/Request/KeyboardCommands"; // MQTT 话题
//        private const int DelayMilliseconds = 100; // 发送延迟时间（毫秒）

//        private readonly MqttClient _mqttClient;
//        private bool _isSnedding = false; // 是否正在发送消息的标志
//        private Timer _timer = null; // 定时器

//        public TopicSending(MqttClient mqttClient)
//        {
//            _mqttClient = mqttClient;
//            // 订阅指定话题的消息
//            _mqttClient.Subscribe(new[] { Topic }, new[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
//            // 指定消息到达事件的处理函数
//            _mqttClient.MqttMsgPublishReceived += MqttMsgPublishReceived;
//        }

//        private void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
//        {
//            // 收到消息时的回调函数
//            Console.WriteLine($"Received message: {System.Text.Encoding.UTF8.GetString(e.Message)}");
//        }

//        public void HandleKeyboardInput(Key key, KeyState state)
//        {
//            if (state == KeyState.Down)
//            {
//                // 按下某个键时，根据键位发送对应消息
//                switch (key)
//                {
//                    case Key.Up:
//                        StartSending("up, 0.5");
//                        break;
//                    case Key.Down:
//                        StartSending("down, 0.5");
//                        break;
//                    case Key.Left:
//                        StartSending("left, 0.8");
//                        break;
//                    case Key.Right:
//                        StartSending("right, 0.8");
//                        break;
//                }
//            }
//            else if (state == KeyState.Up)
//            {
//                // 松开某个键时，停止发送消息
//                StopSending();
//            }
//        }

//        private void StartSending(string message)
//        {
//            if (!_isSnedding)
//            {
//                // 如果当前未在发送消息，则开启定时器开始发送消息
//                _isSnedding = true;
//                _timer = new Timer(state => {
//                    SendTopicMessage(message);
//                }, null, 0, DelayMilliseconds);
//            }
//        }

//        private void StopSending()
//        {
//            if (_isSnedding)
//            {
//                // 如果当前正在发送消息，则停止定时器，停止发送消息
//                _isSnedding = false;
//                _timer.Dispose();
//            }
//        }

//        private void SendTopicMessage(string message)
//        {
//            if (_mqttClient.IsConnected)
//            {
//                // 发布消息到指定话题
//                _mqttClient.Publish(Topic, System.Text.Encoding.UTF8.GetBytes(message),
//                    MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
//            }
//        }

//    }
//}
