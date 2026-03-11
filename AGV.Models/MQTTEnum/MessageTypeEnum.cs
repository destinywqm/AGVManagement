using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.MQTTEnum
{
    public enum MessageTypeEnum
    {
        None = 0x00,
        /// <summary>
        /// 请求
        /// </summary>
        Request = 0x01,
        /// <summary>
        /// 响应
        /// </summary>
        Response = 0x02,
        /// <summary>
        /// 报告
        /// </summary>
        Report = 0x03,
    }
}
