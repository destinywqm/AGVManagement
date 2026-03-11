using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Help
{
    public class SCU_MQTTClient_Helper
    {
        public static class SCU_MQTTClient_Status
        {
            public const string Connected = "已连接";
            public const string Disconnect = "未连接";
            public const string Initialize = "初始化";
        }
    }
}
