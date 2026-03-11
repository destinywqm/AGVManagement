using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Event
{
    public class MQTTClientServiceEventArgs : EventArgs
    {
        public string Topic
        {
            get;
            internal set;
        }

        /// <summary>
        /// 消息
        /// </summary>
        public byte[] Message
        {
            get;
            internal set;
        }

        /// <summary>
        /// 是否重复发送
        /// </summary>
        public bool DupFlag
        {
            get;
            set;
        }

        /// <summary>
        /// 等级
        /// </summary>
        public byte QosLevel
        {
            get;
            internal set;
        }

        /// <summary>
        /// 是否保留消息
        /// </summary>
        public bool Retain
        {
            get;
            internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="dupFlag"></param>
        /// <param name="qosLevel"></param>
        /// <param name="retain"></param>
        public MQTTClientServiceEventArgs(string topic,
            byte[] message,
            bool dupFlag,
            byte qosLevel,
            bool retain)
        {
            Topic = topic;
            Message = message;
            DupFlag = dupFlag;
            QosLevel = qosLevel;
            Retain = retain;
        }
    }
}
