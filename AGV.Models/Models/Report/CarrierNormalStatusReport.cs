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
    /// 小车一般状态报告
    /// </summary>
    public class CarrierNormalStatusReport : BaseMessage
    {
        /// <summary>
        /// 小车系统编号
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 小车所处的全局坐标X
        /// </summary>
        public double LocationX { get; set; }
        /// <summary>
        /// 小车所处的全局坐标Y
        /// </summary>
        public double LocationY { get; set; }


        /// <summary>
        /// 小车所处的地图号
        /// </summary>
        public int MapID { get; set; }

        /// <summary>
        /// 小车当前所在点
        /// </summary>
        public int CurrentNode { get; set; }   
        /// <summary>
        /// 电池电量百分比*100
        /// </summary>
        public int Battery { get; set; }
        /// <summary>
        /// 角度
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// 实时速度(mm/s)
        /// </summary>
        public int Speed { get; set; }
        /// <summary>
        /// 是否负载
        /// </summary>
        public BearloadEnum Bearload { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public CattierStatus ControlMode { get; set; }
        /// <summary>
        /// 异常状总数
        /// </summary>
        public int ErrorNum { get; set; }
        /// <summary>
        /// 异常集合
        /// </summary>
        public int[] ErrorLst { get; set; }

        public CarrierNormalStatusReport(int number)
        {
            MessageType = MessageTypeEnum.Report;
            Topic = string.Format("{0}/N{1:0000}", MyTopic.CarrierNormalStatusTopic, number);
            MessClass = MessClassEnum.CarrierNormalstatusReport;
        }
    }
   
}
