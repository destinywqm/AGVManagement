using AGV.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models.Event
{
    public class ConnectorMessageEventArgs : EventArgs
    {
        public string Topic { get; }
        public byte[] Content { get; }
        public BaseMessage Message { get; }

        public long Count { get; }

        public ConnectorMessageEventArgs(CommunicationOriginalMessage oriMsg, BaseMessage cltMsg, long count)
        {
            Topic = oriMsg.Topic;
            Content = oriMsg.Message;
            Message = cltMsg;
            Count = count;
        }
    }
}
