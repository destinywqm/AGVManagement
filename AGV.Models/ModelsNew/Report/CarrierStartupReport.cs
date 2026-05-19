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
    /// 小车开机报告
    /// </summary>
    public class CarrierStartupReport : BaseMessage
    {
        public int Number { get; set; } 

        public CarrierStartupReport()
        {
            MessageType = MessageTypeEnum.Report;
            Topic = string.Format("{0}", MyTopic.CarrierCommonTopic);
            MessClass = MessClassEnum.CarrierStartupReport;
        }
    }
}
