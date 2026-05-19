using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AGVManagement.Mqtt
{
    public static class MqttConnectionManager
    {
        // ── 连接池 ────────────────────────────────────────────────────────
        public static Dictionary<string, MqttClientWrapper> MqttClients { get; }
            = new Dictionary<string, MqttClientWrapper>();

        // ── 记录每个 AGV 首次连接时的车型尺寸，重连时复用 ────────────────
        private static readonly Dictionary<string, (double Length, double Width)> _carSizes
            = new Dictionary<string, (double, double)>();

        public static double GetStoredLength(string address)
            => _carSizes.TryGetValue(address, out var s) ? s.Length : 1.2;

        public static double GetStoredWidth(string address)
            => _carSizes.TryGetValue(address, out var s) ? s.Width : 0.8;

        // ── 当前选中地址 ──────────────────────────────────────────────────
        public static string CurrentAddress { get; private set; }

        // ── 获取当前客户端 ────────────────────────────────────────────────
        public static MqttClientWrapper LatestClient =>
            CurrentAddress != null && MqttClients.TryGetValue(CurrentAddress, out var c) ? c : null;

        // ── 事件 ──────────────────────────────────────────────────────────

        /// <summary>连接池内容变化（新增 / 移除）时触发，供主窗口刷新下拉框</summary>
        public static event Action ClientsChanged;

        /// <summary>
        /// 所有 AGV 全部断开后触发。
        /// 地图编辑窗口监听此事件以解锁编辑操作。
        /// </summary>
        public static event Action AllClientsDisconnected;

        // ─────────────────────────────────────────────────────────────────
        //  连接 / 重连
        // ─────────────────────────────────────────────────────────────────

        public static async Task ConnectAsync(
            string address, int port, Panel mainPanel,
            Dispatcher dispatcher, double length, double width)
        {
            if (MqttClients.TryGetValue(address, out var existing) && existing.IsConnected)
                return; // 已连接，不重复

            var wrapper = new MqttClientWrapper(dispatcher, address, length, width);
            await wrapper.InitializeAsync(address, port, mainPanel);
            MqttClients[address] = wrapper;
        }

        public static async Task ReconnectAsync(
            string address, int port, Panel mainPanel,
            Dispatcher dispatcher, double length, double width)
        {
            if (MqttClients.TryGetValue(address, out var client))
                await client.DisconnectAsync();

            await ConnectAsync(address, port, mainPanel, dispatcher, length, width);
        }

        // ─────────────────────────────────────────────────────────────────
        //  【新增】断开单个 AGV 连接
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 断开指定地址的 AGV 连接，并在所有 AGV 均断开时触发
        /// <see cref="AllClientsDisconnected"/> 事件，通知地图窗口解锁编辑。
        /// </summary>
        public static async Task DisconnectClientAsync(string address)
        {
            if (!MqttClients.TryGetValue(address, out var client)) return;

            client.DisableAutoReconnect();
            if (client.IsConnected)
                await client.DisconnectAsync();

            // 更新显示状态
            GlobalDisplayData.SetAgvDisconnected(address);

            // 如果这是最后一个连接的客户端，清空 CurrentAddress
            if (CurrentAddress == address)
                CurrentAddress = MqttClients.Keys.FirstOrDefault(k => k != address
                    && MqttClients[k].IsConnected);

            // 通知下拉框刷新
            ClientsChanged?.Invoke();

            // 检查是否已全部断开 → 解锁地图编辑
            bool allDisconnected = MqttClients.Values.All(c => !c.IsConnected);
            if (allDisconnected)
                AllClientsDisconnected?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────
        //  其余原有方法（保持不变）
        // ─────────────────────────────────────────────────────────────────

        public static void MarkDisconnected(string address)
        {
            if (MqttClients.ContainsKey(address))
                GlobalDisplayData.SetAgvDisconnected(address);
        }

        public static void SetCurrent(string address)
        {
            if (MqttClients.ContainsKey(address))
                CurrentAddress = address;
        }

        public static void AddClient(string address, MqttClientWrapper client,
            double length = 1.2, double width = 0.8)
        {
            MqttClients[address] = client;
            _carSizes[address] = (length, width);   // 记录车型尺寸供重连复用
            CurrentAddress = address;
            ClientsChanged?.Invoke();
        }

        public static MqttClientWrapper GetClient(string address)
            => MqttClients.TryGetValue(address, out var c) ? c : null;
    }
}