using AGVManagement.Mqtt;
using System.Threading.Tasks;

namespace AGVManagement
{
    internal static class MqttShutdownService
    {
        public static async Task CloseAllConnectionsAsync()
        {
            foreach (var client in MqttConnectionManager.MqttClients.Values)
            {
                client.DisableAutoReconnect();
            }

            foreach (var client in MqttConnectionManager.MqttClients.Values)
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync();
                }
            }

            MqttConnectionManager.MqttClients.Clear();
        }
    }
}
