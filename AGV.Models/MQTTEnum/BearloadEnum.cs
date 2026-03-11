using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.MQTTEnum
{
    public enum BearloadEnum
    {
        /// <summary>
        /// 无货
        /// </summary>
        [System.ComponentModel.Description("无货")]
        无货 = 0x00,//没有负载
        /// <summary>
        /// 有货
        /// </summary>
        [System.ComponentModel.Description("有货")]
        有货 = 0x01,//带载
    }
}
