using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Controls;

namespace AGVManagement.Mqtt
{
    public static class MqttConnectionManager
    {
        // 全局 MQTT 连接池：Key = 地址，Value = 对应连接
        public static Dictionary<string, MqttClientWrapper> MqttClients { get; } = new Dictionary<string, MqttClientWrapper>();

        public static async Task ConnectAsync(string address, int port, Panel mainPanel, Dispatcher dispatcher, double length, double width)
        {
            if (MqttClients.ContainsKey(address))
            {
                var client = MqttClients[address];
                if (client.IsConnected)
                    return; // 已连接
            }

            var wrapper = new MqttClientWrapper(dispatcher, address, length, width);
            await wrapper.InitializeAsync(address, port, mainPanel);

            MqttClients[address] = wrapper;
        }

        public static async Task ReconnectAsync(string address, int port, Panel mainPanel, Dispatcher dispatcher, double length, double width)
        {
            if (MqttClients.TryGetValue(address, out var client))
            {
                await client.DisconnectAsync();
            }

            await ConnectAsync(address, port, mainPanel, dispatcher, length, width);
        }

        public static void MarkDisconnected(string address)
        {
            if (MqttClients.ContainsKey(address))
            {
                GlobalDisplayData.SetAgvDisconnected(address);
            }
        }



        // 当前用户选中的 AGV 地址（用于主发送/UI切换）
        public static string CurrentAddress { get; private set; }

        // 获取当前选中的客户端（用于发送等）
        public static MqttClientWrapper LatestClient =>
            CurrentAddress != null && MqttClients.TryGetValue(CurrentAddress, out var client) ? client : null;

        // 设置当前选中 AGV（下拉框变更时或连接时调用）
        public static void SetCurrent(string address)
        {
            if (MqttClients.ContainsKey(address))
            {
                CurrentAddress = address;
            }
        }


        public static event Action ClientsChanged;
        // 添加新的连接并设为当前
        public static void AddClient(string address, MqttClientWrapper client)
        {
            MqttClients[address] = client;
            CurrentAddress = address; // 默认设置为当前连接
            ClientsChanged?.Invoke(); // 通知有变更
        }

        // 获取指定连接（已有）
        public static MqttClientWrapper GetClient(string address)
        {
            return MqttClients.TryGetValue(address, out var client) ? client : null;
        }
    }
}
