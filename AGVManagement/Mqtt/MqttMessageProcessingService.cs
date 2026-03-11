using MQTTnet;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AGVManagement.Mqtt
{
    internal static class MqttMessageProcessingService
    {
        public static void HandleSocMessage(string address, string payload)
        {
            var match = Regex.Match(payload, @"\d+");
            if (match.Success)
            {
                string voltage = match.Value;
                GlobalData.UpdateAgvInfo("电压", $"{voltage}%");
                GlobalDisplayData.UpdateDisplayInfo(address, "电压", $"{voltage}%");
                return;
            }

            GlobalData.UpdateAgvInfo("电压", "未知");
            GlobalDisplayData.UpdateDisplayInfo(address, "电压", "未知");
        }

        public static async Task HandleAssignmentStateMessageAsync(string address, string payload, Func<Task> publishTaskDoneAsync)
        {
            var match = Regex.Match(payload, @"\d+");
            if (!match.Success)
            {
                GlobalStatus.Status = "未知";
                GlobalData.UpdateAgvInfo("运行状态", "未知");
                GlobalDisplayData.UpdateDisplayInfo(address, "运行状态", "未知");
                return;
            }

            int stateValue = int.Parse(match.Value);
            string status = stateValue.ToString();

            if (stateValue == 97)
            {
                status = "进行中";
            }
            else if (stateValue == 32)
            {
                status = "无任务";
            }
            else if (stateValue == 122)
            {
                status = "已完成";
                if (publishTaskDoneAsync != null)
                {
                    await publishTaskDoneAsync();
                }
            }

            GlobalStatus.Status = status;
            GlobalData.UpdateAgvInfo("运行状态", status);
            GlobalDisplayData.UpdateDisplayInfo(address, "运行状态", status);
        }

        public static MqttApplicationMessage BuildTaskDoneMessage()
        {
            return new MqttApplicationMessageBuilder()
                .WithTopic("AGV/Response/TaskDone")
                .WithPayload("已接收")
                .WithExactlyOnceQoS()
                .WithRetainFlag(false)
                .Build();
        }
    }
}
