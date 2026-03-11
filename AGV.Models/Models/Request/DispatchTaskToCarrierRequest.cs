using AGV.Models.MQTTEnum;
using AGV.Models.Topic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Models.Request
{
    /// <summary>
    /// request
    /// 调度向小车发送任务
    /// </summary>
    public class DispatchTaskToCarrierRequest : BaseMessage
    {
        /// <summary>
        /// 小车系统编号
        /// </summary>
        public int Number { get; set; } = 0;
        /// <summary>
        /// 任务类型
        /// </summary>
        public AgvTaskType TaskType { get; set; } = AgvTaskType.行走;
        /// <summary>
        /// 任务编号
        /// </summary>
        public long TaskID { get; set; } = 0;

        /// <summary>
        /// 结束点
        /// </summary>
        public int ToPoint { get; set; } = 0;
        /// <summary>
        /// 结束地图
        /// </summary>
        public int ToMapCode { get; set; } = 0;

        /// <summary>
        /// 结束层级
        /// </summary>
        public int ToLevel { get; set; } = 0;

        /// <summary>
        ///  货架类型
        /// </summary>
        public int Shelves { get; set; } = 0;

        /// <summary>
        ///  是否新任务 0是新任务 1是上个任务的后续任务
        /// </summary>
        public int  TaskNew { get; set; } = 0;

        public DispatchTaskToCarrierRequest(int number)
        { 
            this.Number = number;
            MessageType = MessageTypeEnum.Request;
            Topic = string.Format("{0}/N{1:0000}", MyTopic.DispatchToCarrierSessionTopic, number);
            MessClass = MessClassEnum.DispatchTaskToCarrierRequest;
        }
    }
}
