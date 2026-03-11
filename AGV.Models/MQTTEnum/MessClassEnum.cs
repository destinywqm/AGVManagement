using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.MQTTEnum
{
    public enum MessClassEnum
    {
        None = 0x00,

        //请求
        /// <summary>
        /// 小车心跳请求
        /// </summary>
        CarrierHeartbeatToDispatchRequest = 0x61,
        /// <summary>
        /// 小车线段请求
        /// </summary>
        CarrierReplyApplyLineToDispatchRequest = 0x62,
        /// <summary>
        /// 小车线段完成请求
        /// </summary>
        CarrierReplyUpLineCompleteToDispatchRequest = 0x63,


        /// <summary>
        /// 调度下发任务请求
        /// </summary>
        DispatchTaskToCarrierRequest = 0x52,
        /// <summary>
        /// 调度下发路径请求
        /// </summary>
        DispatchRouteToCarrierRequest = 0x53,
        /// <summary>
        /// 调度删除线段请求
        /// </summary>
        DispatchDeleteRouteToCarrierRequest = 0x54,
        /// <summary>
        /// 调度暂停任务请求
        /// </summary>
        DispatchSuspendTaskRequest = 0x55,
        /// <summary>
        /// 调度继续任务请求
        /// </summary>
        DispatchContinueTaskRequest = 0x56,
        /// <summary>
        /// 调度取消任务请求
        /// </summary>
        DispatchCancelTaskRequest = 0x57,
        /// <summary>
        /// 调度清除任务请求
        /// </summary>
        DispatchClearTaskRequest = 0x58,



        //响应  
        /// <summary>
        /// 调度响应小车心跳  
        /// </summary>
        DispatchHeartbeatToCarrierResponse = 0x61,
        /// <summary>
        /// 调度响应小车线段申请
        /// </summary>
        DispatchReplyApplyLineToCarrierResponse = 0x62,
        /// <summary>
        /// 调度响应小车线段完成申请
        /// </summary>
        DispatchReplyUpLineCompleteToCarrierResponse = 0x63,

        /// <summary>
        /// 小车响应调度任务
        /// </summary>
        CarrierTaskToDispatchResponse = 0x52,
        /// <summary>
        /// 响应调度下发路径
        /// </summary>
        CarrierRouteToDispatchResponse = 0x53,
        /// <summary>
        /// 响应调度删除线段
        /// </summary>
        CarrierDeleteRouteToDispatchResponse = 0x54,
        /// <summary>
        /// 调度暂停任务请求
        /// </summary>
        CarrierSuspendTaskToDispatchResponse = 0x55,
        /// <summary>
        /// 调度继续任务请求
        /// </summary>
        CarrierContinueTaskToDispatchResponse = 0x56,
        /// <summary>
        /// 调度取消任务请求
        /// </summary>
        CarrierCancelTaskToDispatchResponse = 0x57,
        /// <summary>
        /// 调度清除任务请求
        /// </summary>
        CarrierClearTaskToDispatchResponse = 0x58,
        

        //报告
        DispatchLastwillReport = 0xF1,//调度关机
        DispatchStartupReport = 0xF3,//调度开机


        CarrierStartupReport = 0xB1,//小车开机
        CarrierShutdownReport = 0xB2,//小车关机

        CarrierNormalstatusReport = 0xC1,//车辆状态报告
        CarrierTaskstatusReport = 0xC2,//车辆任务报告
    }
}
