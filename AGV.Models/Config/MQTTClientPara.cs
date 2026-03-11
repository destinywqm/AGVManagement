using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Config
{
    public class MQTTClientPara
    {
        public static string BrokerHostName { get; set; } = "127.0.0.1";
        public static int BrokerPort { get; set; } = 7777;


        public static string UserName { get; } = "YS";//"zj-r";
        public static string Password { get; } = "1234qwer";//"68689168";
        public static string ClientName { get; } = "Dispatch";
        public static string LastWillTopic { get; } = "SCU/Dispatch/LastWill";

        public static int DispatchVersion = 1;
    }
}
