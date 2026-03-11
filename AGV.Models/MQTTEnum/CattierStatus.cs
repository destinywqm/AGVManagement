using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.MQTTEnum
{
    public enum CattierStatus
    {
        [System.ComponentModel.Description("未知")]
        未知 = 0,
        /// <summary>
        /// 前往取货点
        /// </summary>
        [System.ComponentModel.Description("前往取货点")]
        前往取货点 = 1,
        /// <summary>
        /// 取货点取货
        /// </summary>
        [System.ComponentModel.Description("取货点取货")]
        取货点取货 = 2,
        /// <summary>
        /// 前往送货点
        /// </summary>
        [System.ComponentModel.Description("前往送货点")]
        前往送货点 = 3,
        /// <summary>
        /// 送货点放货
        /// </summary>
        [System.ComponentModel.Description("送货点放货")]
        送货点放货 = 4,
        /// <summary>
        /// 任务完成
        /// </summary>
        [System.ComponentModel.Description("任务完成")]
        任务完成 = 5,
        /// <summary>
        /// 空闲
        /// </summary>
        [System.ComponentModel.Description("空闲")]
        空闲 = 6,
        /// <summary>
        /// 故障
        /// </summary>
        [System.ComponentModel.Description("故障")]
        故障 = 7,
        /// <summary>
        /// 充电中
        /// </summary>
        [System.ComponentModel.Description("充电中")]
        充电中 = 8,
        /// <summary>
        /// 手动
        /// </summary>
        [System.ComponentModel.Description("手动")]
        手动 = 9,
        /// <summary>
        /// 离线
        /// </summary>
        [System.ComponentModel.Description("离线")]
        离线 = 10,
        /// <summary>
        /// 初始化
        /// </summary>
        [System.ComponentModel.Description("初始化")]
        初始化 = 11,
    }
}
