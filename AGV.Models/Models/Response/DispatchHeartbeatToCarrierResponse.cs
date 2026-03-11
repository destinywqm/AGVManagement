using AGV.Models.MQTTEnum;
using AGV.Models.Topic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Models.Response
{
    /// <summary>
    /// 调度回复车子心跳请求
    /// </summary>
    public class DispatchHeartbeatToCarrierResponse : BaseMessage
    {
        /// <summary>
        /// 小车系统编号
        /// </summary>
        public int Number { get; set; } = 0; 
        /// <summary>
        /// 带参数的调度请求心跳
        /// </summary>
        /// 输入车号
        /// <param name="number"></param>
        public DispatchHeartbeatToCarrierResponse(int number)
        {
            Number = number;
            MessageType = MessageTypeEnum.Response;
            Topic = string.Format("{0}/N{1:0000}", MyTopic.DispatchToCarrierSessionTopic, number);
            MessClass = MessClassEnum.DispatchHeartbeatToCarrierResponse;
        }

    }
}
