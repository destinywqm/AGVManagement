using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Event
{
    public class MQTTServerManagerEventArgs : EventArgs
    {
        /// <summary>
        /// 事件说明
        /// </summary>
        public string Str { get; set; }
        /// <summary>
        /// 客户端ID
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string PassWord { get; set; }
        /// <summary>
        /// 心跳间隔
        /// </summary>
        public int TimeSpan { get; set; }
        /// <summary>
        /// 客户端版本
        /// </summary>
        public string ProtocolVersion { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public string Endpoint { get; set; }
        /// <summary>
        /// 主题
        /// </summary>
        public string Topic { get; set; }
        /// <summary>
        /// 消息
        /// </summary>
        public string Payload { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        public int QualityOfServiceLevel { get; set; } = -1;
        public MQTTServerManagerEventArgs()
        {

        }
        public MQTTServerManagerEventArgs(string _str, string _ClientId, string _UserName, string _PassWord, string _Endpoint, string _Topic, string _Payload, int _QualityOfServiceLevel)
        {
            Str = _str;
            ClientId = _ClientId;
            UserName = _UserName;
            PassWord = _PassWord;
            Endpoint = _Endpoint;
            Topic = _Topic;
            Payload = _Payload;
            QualityOfServiceLevel = _QualityOfServiceLevel;
        }
        public MQTTServerManagerEventArgs(string _str, string _ClientId, string _Topic)
        {
            Str = _str;
            ClientId = _ClientId;
            Topic = _Topic;
        }
        public MQTTServerManagerEventArgs(string _str, string _ClientId, string _Topic, int _QualityOfServiceLevel)
        {
            Str = _str;
            ClientId = _ClientId;
            Topic = _Topic;
            QualityOfServiceLevel = _QualityOfServiceLevel;
        }
        public MQTTServerManagerEventArgs(string _str, string _ClientId, string _UserName, string _PassWord, string _Endpoint)
        {
            Str = _str;
            ClientId = _ClientId;
            UserName = _UserName;
            PassWord = _PassWord;
            Endpoint = _Endpoint;
        }
        public MQTTServerManagerEventArgs(string _str, string _ClientId, string _Topic, string _Payload, int _QualityOfServiceLevel)
        {
            Str = _str;
            ClientId = _ClientId;
            Topic = _Topic;
            Payload = _Payload;
            QualityOfServiceLevel = _QualityOfServiceLevel;
        }
        public MQTTServerManagerEventArgs(string _str, string _ClientId, string _UserName, string _PassWord, string _Endpoint, int _TimeSpan, string _ProtocolVersion)
        {
            Str = _str;
            ClientId = _ClientId;
            UserName = _UserName;
            PassWord = _PassWord;
            Endpoint = _Endpoint;
            TimeSpan = _TimeSpan;
            ProtocolVersion = _ProtocolVersion;
        }
        public MQTTServerManagerEventArgs(string _str, string _ClientId)
        {
            Str = _str;
            ClientId = _ClientId;

        }
    }
}
