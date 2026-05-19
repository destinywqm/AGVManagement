using AGV.Models.MQTTEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Models
{
    public class BaseMessage
    {
        /// <summary>
        /// 主题
        /// </summary>
        public string Topic = "";

        /// <summary>
        /// 消息大类
        /// </summary>
        public MessageTypeEnum MessageType { get; protected set; } = MessageTypeEnum.None;

        /// <summary>
        /// 消息小类
        /// </summary>
        public MessClassEnum MessClass { get; protected set; } = MessClassEnum.None;

        /// <summary>
        /// 是否保留
        /// </summary>
        public bool Retained { get; protected set; } = false;

        /// <summary>
        /// 会话编号
        /// </summary>
        public int Session { get; set; } = 0;

        /// <summary>
        /// 时间
        /// </summary>
        public DateTime DateTime { get; } = DateTime.Now;
    }
}
