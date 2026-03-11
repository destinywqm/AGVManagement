using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.MQTTEnum
{
    public enum AgvTaskType
    {
        无 = 0,
        /// <summary>
        /// 行走 1
        /// </summary>
        行走 = 4,
        /// <summary>
        /// 取货 2
        /// </summary>
        取货 = 1,
        /// <summary>
        /// 放货 3
        /// </summary>
        放货 = 2,
        /// <summary>
        /// 充电 4
        /// </summary>
        充电 = 6,
    }
    public enum SCU_AGVInfo_Temp_TaskStatus
    {
        /// <summary>
        /// 无任务
        /// </summary>
        [System.ComponentModel.Description("无任务")]
        无任务 = 0,//未知，或没有任务 
        /// <summary>
        /// 新任务
        /// </summary>
        [System.ComponentModel.Description("新任务")]
        新任务 = 1,//未知，或没有任务 
        /// <summary>
        /// 配送中
        /// </summary>
        [System.ComponentModel.Description("配送中")]
        配送中 = 2,//配送中
        /// <summary>
        /// 操作中
        /// </summary>
        [System.ComponentModel.Description("操作中")]
        操作中 = 3,//操作中
        /// <summary>
        /// 已完成
        /// </summary>
        [System.ComponentModel.Description("已完成")]
        已完成 = 4,//已完成
        /// <summary>
        /// 异常
        /// </summary>
        [System.ComponentModel.Description("异常")]
        异常 = 5,//异常
        /// <summary>
        /// 暂停
        /// </summary>
        [System.ComponentModel.Description("暂停")]
        暂停 = 6,
    }
}
