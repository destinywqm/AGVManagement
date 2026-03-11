using AGV.Models.MQTTEnum;
using AGV.Models.Topic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Models.Report
{
    /// <summary>
    /// 小车任务状态报告
    /// </summary>
    public class CarrierTaskStatusReport : BaseMessage
    {
        /// <summary>
        /// 小车系统编号
        /// </summary>
        public int Number { get; set; } 
        /// <summary>
        /// 任务编号
        /// </summary>
        public long TaskID { get; set; }
        public int TaskEnd { get; set; }
         
        /// <summary>
        /// 任务类型
        /// </summary>
        public AgvTaskType TaskType { get; set; }
        /// <summary>
        /// 任务状态
        /// </summary>
        public SCU_AGVInfo_Temp_TaskStatus TaskStatus { get; set; }
        /// <summary>
        /// 待行驶路线长度
        /// </summary>
        public int RouteLength { get; set; }
        /// <summary>
        /// 待行驶路线编号
        /// </summary>
        public Int64[] RouteNumbers { get; set; } 
        /// <summary>
        /// 待行驶路线长度
        /// </summary>
        public int ALLRouteLength { get; set; }
        /// <summary>
        /// 待行驶路线编号
        /// </summary>
        public Int64[] ALLRouteNumbers { get; set; } 

        public CarrierTaskStatusReport(int number)
        {
            MessageType = MessageTypeEnum.Report;
            Topic = string.Format("{0}/N{1:0000}", MyTopic.CarrierTaskStatusTopic, number);
            MessClass = MessClassEnum.CarrierTaskstatusReport;
        }
    }
}
