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
    /// 调度继续任务请求
    /// </summary>
    public class DispatchContinueTaskToCarrierRequest : BaseMessage
    {
        /// <summary>
        /// 小车系统编号
        /// </summary>
        public int Number { get; set; } = 0;
        public long TaskId { get; set; } = 0;


        /// <summary>
        /// 带参数的调度下发删除路线请求构造函数
        /// </summary>
        /// 车号
        /// <param name="number"></param>
        public DispatchContinueTaskToCarrierRequest(int number)
        {
            MessageType = MessageTypeEnum.Request;
            Topic = string.Format("{0}/N{1:0000}", MyTopic.DispatchToCarrierSessionTopic, number);
            MessClass = MessClassEnum.DispatchContinueTaskRequest;
        }
    }
}
