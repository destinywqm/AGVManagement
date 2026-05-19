using AGV.Models.MQTTEnum;
using AGV.Models.Topic;
//using SCU.AGV.Entity.MQTT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Models.Response
{
  
    /// <summary>
    /// 小车响应任务
    /// </summary>
    public class DispatchReplyApplyLineToCarrierResponse : BaseMessage
    {
        /// <summary>
        /// 小车系统编号
        /// </summary>
        public int Number { get; set; } = 0;
        /// <summary>
        /// 任务编号
        /// </summary>
        public long TaskID { get; set; } = 0;
        /// <summary>
        /// 任务编号
        /// </summary>
        public int LineNo { get; set; } = 0;
        /// <summary>
        /// 响应结果
        /// </summary>
        public ResultEnum Results { get; set; }
        /// <summary>
        /// 响应构造函数
        /// </summary>
        public DispatchReplyApplyLineToCarrierResponse(int number)
        {
            MessageType = MessageTypeEnum.Response;
            Topic = string.Format("{0}/N{1:0000}", MyTopic.DispatchToCarrierSessionTopic, number);
            MessClass = MessClassEnum.DispatchReplyApplyLineToCarrierResponse;
        }
    }
}
