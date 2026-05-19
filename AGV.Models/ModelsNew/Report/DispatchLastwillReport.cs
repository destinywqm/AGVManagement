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
    /// 调度遗言报告
    /// </summary>
    public class DispatchLastwillReport : BaseMessage
    {
        /// <summary>
        /// 调度系统编号
        /// </summary>
        public int Number { get; set; } = 0; 

        public DispatchLastwillReport(int number)
        { 
            MessageType = MessageTypeEnum.Report; 
            Topic = string.Format("{0}/N{1:0000}", MyTopic.DispatchLastwillTopic, number);
            MessClass = MessClassEnum.DispatchLastwillReport;
        }
    }
}
