using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;
using System.Windows;
using AGVManagement.Mqtt;

namespace AGVManagement.Helpers
{
    /// <summary>
    /// MQTT 发布辅助类
    /// 将 MainWindow 中重复的"判断连接 → 构建消息 → PublishAsync → 弹框"模式统一封装
    /// </summary>
    public static class MqttPublishHelper
    {
        /// <summary>
        /// 向指定 Topic 发送字符串 Payload，内置连接检查与异常处理
        /// </summary>
        /// <param name="topic">MQTT 主题</param>
        /// <param name="payload">消息内容</param>
        /// <param name="successMsg">成功后弹框提示（null 则不弹）</param>
        public static async Task PublishAsync(string topic, string payload,
            string successMsg = null)
        {
            var client = MqttConnectionManager.LatestClient;
            if (client == null || !client.IsConnected)
            {
                MessageBox.Show("MQTT 客户端未连接，请先建立连接。");
                return;
            }

            try
            {
                var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag(false)
                    .Build();

                await client.PublishAsync(msg);

                if (!string.IsNullOrEmpty(successMsg))
                    MessageBox.Show(successMsg, "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}