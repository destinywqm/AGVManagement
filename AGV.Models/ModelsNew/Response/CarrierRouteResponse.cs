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
    public class CarrierRouteResponse : BaseMessage
    {
        /// <summary>
        /// 小车系统编号
        /// </summary>
        public int Number { get; set; } = 0;
        public ResultEnum Results { get; set; }


        public long TaskId { get; set; } = 0;

        /// <summary>
        /// 带参数的调度下方路线请求构造函数
        /// </summary>
        /// 车号
        /// <param name="number"></param>
        public CarrierRouteResponse()
        {
            MessageType = MessageTypeEnum.Response;
            //  Topic = string.Format("{0}/N{1:0000}", MyTopic.CarrierSessionTopic, number);
            MessClass = MessClassEnum.CarrierRouteToDispatchResponse;
        }

    }
}
