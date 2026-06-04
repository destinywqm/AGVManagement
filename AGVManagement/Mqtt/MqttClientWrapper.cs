using AGVManagement.MapPaint;
using AGVManagement.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AGVManagement.Mqtt
{
    /// <summary>
    /// 单个 AGV 的 MQTT 连接封装：连接、订阅、消息处理、小车 UI 更新
    /// </summary>
    public class MqttClientWrapper
    {
        // ── 公开字段 / 属性 ───────────────────────────────────────────────
        public IMqttClient Client => _mqttClient;
        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        public static string payloadAll;

        public static double ActualWidthNow;
        public static double ActualHeightNow;
        public static double ProportionNow;

        // ── 私有字段 ──────────────────────────────────────────────────────
        public IMqttClient _mqttClient;
        private AgvCar _car;
        private Panel _mainPanel;

        private readonly Dispatcher _dispatcher;
        private readonly string _address;
        private readonly double _length;
        private readonly double _width;
        private bool _autoReconnect = true;

        private static readonly HttpClient _httpClient = new HttpClient();

        // ── 构造 ──────────────────────────────────────────────────────────
        public MqttClientWrapper(Dispatcher dispatcher, string address, double length, double width)
        {
            _dispatcher = dispatcher;
            _address = address;
            _length = length;
            _width = width;
        }

        // ─────────────────────────────────────────────────────────────────
        //  初始化 & 连接
        // ─────────────────────────────────────────────────────────────────

        public async Task InitializeAsync(string address, int port, Panel mainPanel)
        {
            _mainPanel = mainPanel;

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // ── 连接成功 ──
            _mqttClient.UseConnectedHandler(async e =>
            {
                GlobalData.UpdateAgvInfo("网络状态", "已连接");
                Application.Current.Dispatcher.Invoke(() =>
                    GlobalDisplayData.SetAgvConnected(_address));

                await _mqttClient.SubscribeAsync(
                    new MqttTopicFilterBuilder().WithTopic("AGV/Response/SOC").Build());
                await _mqttClient.SubscribeAsync(
                    new MqttTopicFilterBuilder().WithTopic("AGV/Carrier/Common").Build());
                await _mqttClient.SubscribeAsync(
                    new MqttTopicFilterBuilder().WithTopic("AGV/Response/AssignmentState").Build());
            });

            // ── 断开连接 ──
            _mqttClient.UseDisconnectedHandler(e =>
            {
                if (!e.ClientWasConnected) return;

                GlobalData.UpdateAgvInfo("网络状态", "未连接");
                Application.Current.Dispatcher.Invoke(() =>
                    GlobalDisplayData.SetAgvDisconnected(_address));

                _dispatcher.Invoke(() =>
                {
                    if (_car != null)
                    {
                        _mainPanel.Children.Remove(_car.Shape);
                        _car = null;
                    }
                    _mainPanel.UpdateLayout();
                });
            });

            // ── 收到消息 ──
            _mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                string topic = e.ApplicationMessage.Topic;
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                try
                {
                    switch (topic)
                    {
                        case "AGV/Response/SOC":
                            HandleSoc(payload);
                            break;

                        case "AGV/Response/AssignmentState":
                            await HandleAssignmentStateAsync(payload);
                            break;

                        case "AGV/Carrier/Common":
                            await HandleCarCommonAsync(payload);
                            break;
                    }
                }
                catch (JsonException) { /* 忽略非 JSON 消息 */ }
            });

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(address, port)
                .WithCleanSession()
                .Build();

            await _mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        // ─────────────────────────────────────────────────────────────────
        //  消息处理（私有，按 Topic 拆分）
        // ─────────────────────────────────────────────────────────────────

        private void HandleSoc(string payload)
        {
            var match = Regex.Match(payload, @"\d+");
            string voltage = match.Success ? $"{match.Value}%" : "未知";
            GlobalData.UpdateAgvInfo("电压", voltage);
            GlobalDisplayData.UpdateDisplayInfo(_address, "电压", voltage);
        }

        private async Task HandleAssignmentStateAsync(string payload)
        {
            var match = Regex.Match(payload, @"\d+");
            string status;

            if (match.Success)
            {
                int val = int.Parse(match.Value);
                switch (val)
                {
                    case 97: status = "进行中"; break;
                    case 32: status = "无任务"; break;
                    case 122:
                        status = "已完成";
                        var client = MqttConnectionManager.LatestClient;
                        if (client != null && client.IsConnected)
                        {
                            var msg = new MqttApplicationMessageBuilder()
                                .WithTopic("AGV/Response/TaskDone")
                                .WithPayload("已接收")
                                .WithExactlyOnceQoS().WithRetainFlag(false).Build();
                            await client.PublishAsync(msg);
                        }
                        break;
                    default: status = val.ToString(); break;
                }
            }
            else
            {
                status = "未知";
            }

            GlobalStatus.Status = status;
            GlobalData.UpdateAgvInfo("运行状态", status);
            GlobalDisplayData.UpdateDisplayInfo(MqttConnectionManager.CurrentAddress, "运行状态", status);
        }

        private async Task HandleCarCommonAsync(string payload)
        {
            var data = JsonSerializer.Deserialize<CarData>(payload);

            double rawX = data.X;
            double rawY = data.Y;

            data.X *= ProportionNow;
            data.Y *= -ProportionNow;
            data.Z = -data.Z;

            // 首次收到消息时创建小车 UI
            if (_car == null)
            {
                _dispatcher.Invoke(() =>
                {
                    _car = new AgvCar(
                        _address,
                        new Point(ActualWidthNow, ActualHeightNow),
                        _length * ProportionNow,
                        _width * ProportionNow);
                    Panel.SetZIndex(_car.Shape, int.MaxValue);
                    _mainPanel.Children.Add(_car.Shape);
                });
            }

            payloadAll = payload;
            UpdateCarPosition(data, rawX, rawY);

            await Task.CompletedTask;
        }

        // ─────────────────────────────────────────────────────────────────
        //  公开操作
        // ─────────────────────────────────────────────────────────────────

        public async Task PublishAsync(MqttApplicationMessage message)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("MQTT 客户端未连接。");
            await _mqttClient.PublishAsync(message);
        }

        public async Task SubscribeAsync(string topic)
        {
            try
            {
                if (_mqttClient == null || !_mqttClient.IsConnected)
                    throw new InvalidOperationException("MQTT 客户端未连接。");
                await _mqttClient.SubscribeAsync(
                    new MqttTopicFilterBuilder().WithTopic(topic).Build());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"订阅失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void DisableAutoReconnect() => _autoReconnect = false;

        public async Task DisconnectAsync()
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
                _mqttClient.Dispose();
                _mqttClient = null;
            }
        }

        public async Task<string> GetMessageFromTopicAsync(string topic)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("MQTT 客户端未初始化或未连接。");

            var tcs = new TaskCompletionSource<string>();
            _mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                if (e.ApplicationMessage?.Topic == topic)
                    tcs.TrySetResult(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            });

            await _mqttClient.SubscribeAsync(
                new MqttTopicFilterBuilder().WithTopic(topic).Build());
            return await tcs.Task;
        }

        // ─────────────────────────────────────────────────────────────────
        //  私有工具
        // ─────────────────────────────────────────────────────────────────

        private void UpdateCarPosition(CarData data, double rawX, double rawY)
        {
            _dispatcher.InvokeAsync(() => _car?.UpdatePosition(data.X, data.Y, data.Z, rawX, rawY));
        }
    }
}