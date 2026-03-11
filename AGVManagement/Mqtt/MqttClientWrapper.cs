using MQTTnet.Client.Options;
using MQTTnet.Client;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AGVManagement.MainWindow;
using System.Windows.Threading;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows;
using System.Net;
using AGVManagement.MapPaint;
using System.Net.Http;
using System.Windows.Markup;

namespace AGVManagement.Mqtt
{
    public class MqttClientWrapper
    {
        public IMqttClient _mqttClient;
        public IMqttClient Client => _mqttClient;

        private Car _car;
        private Dispatcher _dispatcher;
        private string _address;
        private readonly double _length;
        private readonly double _width;
        public static string payloadAll;
        private Panel _mainPanel;
        //public static double ActualWidthNow = 22.756*20*2.5;
        //public static double ActualHeightNow = 493*2.5-18.618*20*2.5;
        //public static double ProportionNow = 20*2.5;
        public static double ActualWidthNow ;
        public static double ActualHeightNow ;
        public static double ProportionNow ;
        SendCarDataToApi sendCarDataToApi = new SendCarDataToApi();
        private bool _isAutoReconnectEnabled = true;
        private Panel _mainPanelCut; // 当前显示小车的面板

        public MqttClientWrapper(Dispatcher dispatcher, string address, double length, double width)
        {
            _dispatcher = dispatcher;
            _address = address;
            _length = length;
            _width = width;
        }

        // 添加 IsConnected 属性
        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        private async Task SendCarDataToApi(CarData data)
        {
            var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://localhost:<port>/api/CarData/update", content);
            response.EnsureSuccessStatusCode();
        }

        
        public async Task InitializeAsync(string address, int port, Panel mainPanel)
        {
            _mainPanel = mainPanel;

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.UseConnectedHandler(async e =>
            {
                // 更新网络状态为“已连接”
                GlobalData.UpdateAgvInfo("网络状态", "已连接");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    GlobalDisplayData.SetAgvConnected(_address);
                });
                //GlobalDisplayData.UpdateDisplayInfo("网络状态", "已连接");

                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("AGV/Response/SOC").Build());
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("AGV/Carrier/Common").Build());
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("AGV/Response/AssignmentState").Build());
            });



            // 已断开事件
            _mqttClient.UseDisconnectedHandler(e =>
            {
                // 只有在实际连接过后再触发“未连接”状态
                if (e.ClientWasConnected)
                {
                    // 更新网络状态为“未连接”
                    GlobalData.UpdateAgvInfo("网络状态", "未连接");
                    //GlobalDisplayData.UpdateDisplayInfo("网络状态", "未连接");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        GlobalDisplayData.SetAgvDisconnected(_address);
                    });
                    // 在断开连接时移除小车
                    _dispatcher.Invoke(() =>
                    {
                        if (_car != null)
                        {
                            _mainPanel.Children.Remove(_car.Shape);
                            _car = null; // 清空小车对象
                        }

                        // 强制刷新 UI
                        _mainPanel.UpdateLayout();
                    });
                }

                // 可选：显示消息框提示断开连接
                //MessageBox.Show($"与地址 {_address} 的连接已断开。", "连接断开", MessageBoxButton.OK, MessageBoxImage.Warning);
            });


            _mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                try
                {
                    if (topic == "AGV/Response/SOC")
                    {
                        MqttMessageProcessingService.HandleSocMessage(_address, payload);
                    }

                    if (topic == "AGV/Response/AssignmentState")
                    {
                        await MqttMessageProcessingService.HandleAssignmentStateMessageAsync(_address, payload, PublishTaskDoneAckAsync);
                    }
                    if (topic == "AGV/Carrier/Common")
                    {
                        var data = JsonSerializer.Deserialize<CarData>(payload);

                        // 缩
                        data.X *= ProportionNow;
                        data.Y *= -ProportionNow;
                        data.Z = -data.Z;
                        
                        //SendCarDataToApi(data);

                        // 只有在收到消息后才显示小车
                        if (_car == null)
                        {
                            _dispatcher.Invoke(() =>
                            {
                                // 移除旧的小车，确保界面干净
                                if (_car != null)
                                {
                                    _mainPanel.Children.Remove(_car.Shape);
                                    _car = null;
                                }

                                _car = new Car(_address, new Point(ActualWidthNow, ActualHeightNow), _length * ProportionNow, _width * ProportionNow);
                                _mainPanel.Children.Add(_car.Shape);
                            });
                        }
                        payloadAll = payload;
                        UpdateCarPosition(data);
                    }
                }
                catch (JsonException)       
                {
                    // 如果反序列化失败，忽略这个错误，不进行处理
                }
            });



            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(address, port)
                .WithCleanSession()
                .Build();

            await _mqttClient.ConnectAsync(options, CancellationToken.None);

            // 创建小车并添加到面板
            //_dispatcher.Invoke(() =>
            //{
            //    _car = new Car(new Point(ActualWidthNow, ActualHeightNow), 144, 96);
            //    mainPanel.Children.Add(_car.Shape);
            //});
        }

        private async Task PublishTaskDoneAckAsync()
        {
            var client = MqttConnectionManager.LatestClient;
            if (client != null && client.IsConnected)
            {
                var message = MqttMessageProcessingService.BuildTaskDoneMessage();
                await client.PublishAsync(message);
            }
        }

        //发布消息
        public async Task PublishAsync(MqttApplicationMessage message)
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                await _mqttClient.PublishAsync(message);
            }
            else
            {
                throw new InvalidOperationException("MQTT客户端未连接。");
            }
        }

        public void DisableAutoReconnect()
        {
            _isAutoReconnectEnabled = false;
        }

        public async Task DisconnectAsync()
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
                _mqttClient.Dispose(); // 确保清理 MQTT 客户端
                _mqttClient = null;
            }
        }

        //订阅主题
        public async Task SubscribeAsync(string topic)
        {
            try
            {
                if (_mqttClient != null && _mqttClient.IsConnected)
            {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
            }
            else
            {
                throw new InvalidOperationException("MQTT客户端未连接。");
            }
            }   
            catch (Exception ex)
            {
                MessageBox.Show($"发送数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCarPosition(CarData data)
        {
            _dispatcher.InvokeAsync(() =>
            {
                if (_car != null) // 仅当车辆存在时更新
                {
                    _car.UpdatePosition(data.X, data.Y, data.Z);
                }
            });
        }

        public async Task<string> GetMessageFromTopicAsync(string topic)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("MQTT client is not initialized or connected.");
            
            var tcs = new TaskCompletionSource<string>();

            // 设置消息处理程序
            _mqttClient.UseApplicationMessageReceivedHandler(e =>
            {   
                if (e.ApplicationMessage?.Topic == topic)   
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    tcs.SetResult(payload);
                }
            });

            // 订阅主题
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());

            // 等待消息到达
            return await tcs.Task;
        }


    }


}
