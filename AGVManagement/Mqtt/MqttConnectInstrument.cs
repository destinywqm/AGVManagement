using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVManagement.Mqtt
{
    public class MqttConnectInstrument
    {
        public void client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            // 获取主题
            string topic = e.ApplicationMessage.Topic;

            // 判断主题是否是"AGV/Carrier/NormalStatus"
            if (topic == "AGV/Carrier/NormalStatus")
            {
                // 获取消息内容
                string message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                // 解析消息内容
                string[] messageParts = message.Split(',');

                // 存储解析后的参数
                string number = "";
                double locationX = 0;
                double locationY = 0;
                int mapID = 0;
                int currentNode = 0;
                int battery = 0;
                int angle = 0;
                int speed = 0;
                int bearload = 0;
                string controlMode = "";

                foreach (string part in messageParts)
                {
                    string[] keyValue = part.Split(' ');
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();

                        switch (key)
                        {
                            case "Number":
                                number = value;
                                break;
                            case "LocationX":
                                double.TryParse(value, out locationX);
                                break;
                            case "LocationY":
                                double.TryParse(value, out locationY);
                                break;
                            case "MapID":
                                int.TryParse(value, out mapID);
                                break;
                            case "CurrentNode":
                                int.TryParse(value, out currentNode);
                                break;
                            case "Battery":
                                int.TryParse(value, out battery);
                                break;
                            case "Angle":
                                int.TryParse(value, out angle);
                                break;
                            case "Speed":
                                int.TryParse(value, out speed);
                                break;
                            case "Bearload":
                                int.TryParse(value, out bearload);
                                break;
                            case "ControlMode":
                                controlMode = value;
                                break;
                        }
                    }
                }


            }
            // 其他逻辑处理
        }

        public string HandleMapDrawingMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic == "AGV/Response/MapDrawing")
            {
                // 解析消息负载
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                // 在这里处理消息负载，可以根据需求编写逻辑操作

                // 返回收到的消息即可
                return payload;
            }

            return null;
        }
    }
}
