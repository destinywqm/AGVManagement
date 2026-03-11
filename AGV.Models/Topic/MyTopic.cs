using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Topic
{
    public class MyTopic
    {
        //订阅主题
        //车子
        /// <summary>
        /// 小车系统主题-遗言 AGV/Carrier/LastWill
        /// </summary>
        public static readonly string CarrierLastWillTopic = "AGV/Carrier/LastWill";
        /// <summary>
        /// 小车系统主题-通用 AGV/Carrier/Common
        /// </summary>
        public static readonly string CarrierCommonTopic = "AGV/Carrier/Common";
        /// <summary>
        /// 小车系统主题-会话 AGV/Carrier/Session
        /// </summary>
        public static readonly string CarrierSessionTopic = "AGV/Carrier/Session";
        /// <summary>
        /// 小车系统主题-一般状态 AGV/Carrier/NormalStatus
        /// </summary>
        public static readonly string CarrierNormalStatusTopic = "AGV/Carrier/NormalStatus";
        /// <summary>
        /// 小车系统主题-任务状态 AGV/Carrier/TaskStatus
        /// </summary>
        public static readonly string CarrierTaskStatusTopic = "AGV/Carrier/TaskStatus";


        //-----------------------------------------------------------------------------------------------------------
        //发布主题
        //车子 算法  地图编辑器
        /// <summary>
        /// 调度系统主题-遗言
        /// </summary>
        public static readonly string DispatchLastwillTopic = "SCU/Dispatch/LastWill";
        /// <summary>
        /// 调度系统主题 -开机
        /// </summary>
        public static readonly string DispatchStartingTopic = "SCU/Dispatch/Starting";


        //车子
        /// <summary>
        /// 调度系统发给车主题-通用 SCU/Dispatch/ToCarrier/Common
        /// </summary>
        public static readonly string DispatchToCarrierCommonTopic = "SCU/Dispatch/ToCarrier/Common";
        /// <summary>
        /// 调度系统发给车主题-会话 SCU/Dispatch/ToCarrier/Session
        /// </summary>
        public static readonly string DispatchToCarrierSessionTopic = "SCU/Dispatch/ToCarrier/Session";


        /// <summary>
        /// 调度系统发送给监控的主题
        /// </summary>
        public static readonly string DispatchToMonitorSessionTopic = "SCU/Dispatch/ToMonitor/Session";
    }
}
