using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.Models
{
    public class CommunicationOriginalMessage
    {
        public string Topic { get; }
        public byte[] Message { get; }

        public CommunicationOriginalMessage(string topic, byte[] message)
        {
            Topic = topic;
            Message = message;
        }
    }
}
