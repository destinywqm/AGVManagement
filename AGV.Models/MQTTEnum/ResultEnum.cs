using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.MQTTEnum
{
           public enum ResultEnum
        {
            /// <summary>
            /// 失败 0
            /// </summary>
            失败 = 0,
            /// <summary>
            /// 成功 1
            /// </summary>
            成功 = 1,
            /// <summary>
            /// 其它车锁定 2
            /// </summary>
            其它车锁定 = 2,
            /// <summary>
            /// 申请点不是当前地图点 3
            /// </summary>
            申请点不是当前地图点 = 3,
            /// <summary>
            /// 第三方交管请求失败 4
            /// </summary>
            第三方交管请求失败 = 4,
            /// <summary>
            /// 与小车当前执行任务不符合 5
            /// </summary>
            与小车当前执行任务不符合 = 5,
            /// <summary>
            /// 调度中当前车子没有任务绑定 6
            /// </summary>
            调度中当前车子没有任务绑定 = 6,
            /// <summary>
            /// 调度逻辑异常 7
            /// </summary>
            调度逻辑异常 = 7,
            /// <summary>
            /// 任务可能被删除 8
            /// </summary>
            任务可能被删除 = 8,
            /// <summary>
            ///  超出申请数量 9
            /// </summary>
            超出申请数量 = 9,
            /// <summary>
            /// 前方有车 10
            /// </summary>
            前方有车 = 10,
            /// <summary>
            /// 添加交管失败 11
            /// </summary>
            添加交管失败 = 11,


            /// <summary>
            /// 车辆接收任务异常
            /// </summary>
            接收异常 = 21,
            /// <summary>
            /// 车辆当前已有任务
            /// </summary>
            车辆当前已有任务 = 22,
            /// <summary>
            /// 任务重复
            /// </summary>
            任务重复 = 23,
            /// <summary>
            /// 无任务
            /// </summary>
            无任务 = 24,
            /// <summary>
            /// 线段已下发
            /// </summary>
            线段已下发 = 25,
        }
    
}
