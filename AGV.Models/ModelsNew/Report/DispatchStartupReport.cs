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
    /// 调度开机报告
    /// </summary>
    public class DispatchStartupReport : BaseMessage
    {
        public int Number { get; set; } 

        public DispatchStartupReport(int number)
        { 
            MessageType = MessageTypeEnum.Report;
            Topic = string.Format("{0}/N{1:0000}", MyTopic.DispatchStartingTopic, number);
            MessClass = MessClassEnum.DispatchStartupReport;
            Retained = false;
        }
    }
}
