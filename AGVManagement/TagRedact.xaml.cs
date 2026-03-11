using AGVManagement.MapPaint;
using MQTTnet.Client;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;
using MQTTnet.Client.Receiving;
using AGV.BLL;
using System.Data;
using System.Web.UI.WebControls;

namespace AGVManagement
{
    /// <summary>
    /// TagRedact.xaml 的交互逻辑
    /// </summary>
    public partial class TagRedact : Window
    {
        private int TagIndex;
        private Canvas Canvas;
        private System.Windows.Controls.Label label;
        private MapInstrument mapInstrument = new MapInstrument();
        private TagManag tagManag = new TagManag();
        public delegate void Movement(Point point, int TagID);
        public Movement GetMovement; //信标移动委托
        private IMqttClient mqttClient;
        private TagInfoBLL tagInfoBLL;
        private AutomaticSearchAlgorithm searchAlgorithm;
        public static Dictionary<int, int> TagToRotate = new Dictionary<int, int>();
        public static Dictionary<int, int> TagToVSpide = new Dictionary<int, int>();
        public static Dictionary<int, int> TagToASpide = new Dictionary<int, int>();


        public TagRedact(int TagID, Canvas canvas)
        {
            InitializeComponent();
            this.TagIndex = TagID;
            this.Canvas = canvas;
            this.Title = $"信标编辑 - Tag{TagID}";
            tagInfoBLL = new TagInfoBLL();
            TagSelect();
        }

        /// <summary>
        /// 信标信息查询
        /// </summary>
        private void TagSelect()
        {
            label = tagManag.TagSelct(TagIndex);
            /*TagX.Text = (label.Margin.Left/ Painting.siseWin).ToString();   
            TagY.Text = (label.Margin.Top/ Painting.siseWin).ToString();*/

            // 从数据模型而不是UI读取
            Point logicPos = mapInstrument.GetTagLogicPosition(TagIndex);

            TagX.Text = logicPos.X.ToString();
            TagY.Text = logicPos.Y.ToString();  



            /*DataTable TagStation = tagInfoBLL.RataTable(MainWindow.Time.ToString());
            DataRow tagRow = TagStation.AsEnumerable()
    .FirstOrDefault(row => row["TagName"].ToString() == TagIndex.ToString());

            double x = Convert.ToDouble(tagRow["X"]);
            double y = Convert.ToDouble(tagRow["Y"]);

            double transformedX = x * 10 - 19;
            double transformedY = y * 10 - 11.5;

            TagX.Text = transformedX.ToString("F2");  // 保留两位小数
            TagY.Text = transformedY.ToString("F2");*/



            // 假设 TagIndex 是查找的键值
            //if (TagToRotate.TryGetValue(TagIndex, out int rotateValue))
            //{
            //    // 如果字典中存在 TagIndex 键，将其值赋给 TagRotate.Text
            //    TagRotate.Text = rotateValue.ToString();
            //}
            //else
            //{
            //    // 如果字典中不存在 TagIndex 键，默认将 TagRotate.Text 设置为 "0"
            //    TagRotate.Text = "0";
            //}


            //if (TagToVSpide.TryGetValue(TagIndex, out int rotateValue1))
            //{
            //    // 如果字典中存在 TagIndex 键，将其值赋给 TagRotate.Text
            //    v_desired.Text = rotateValue1.ToString();
            //}
            //else
            //{
            //    // 如果字典中不存在 TagIndex 键，默认将 TagRotate.Text 设置为 "0"
            //    v_desired.Text = "6";
            //}

            //if (TagToASpide.TryGetValue(TagIndex, out int rotateValue2))
            //{
            //    // 如果字典中存在 TagIndex 键，将其值赋给 TagRotate.Text
            //    a_desired.Text = rotateValue2.ToString();
            //}
            //else
            //{
            //    // 如果字典中不存在 TagIndex 键，默认将 TagRotate.Text 设置为 "0"
            //    a_desired.Text = "5";
            //}


        }

        //键位更新*3
        private void AddOrUpdateTagRotate(int tagIndex, string tagRotateText)
        {
            int tagRotateValue = int.Parse(tagRotateText);

            if (TagToRotate.ContainsKey(tagIndex))
            {
                TagToRotate[tagIndex] = tagRotateValue; // 更新现有键的值
            }
            else
            {
                TagToRotate.Add(tagIndex, tagRotateValue); // 添加新键值对
            }
        }

        private void AddOrUpdateTagToVSpide(int tagIndex, string tagRotateText)
        {
            int tagRotateValue = int.Parse(tagRotateText);

            if (TagToVSpide.ContainsKey(tagIndex))
            {
                TagToVSpide[tagIndex] = tagRotateValue; // 更新现有键的值
            }
            else
            {
                TagToVSpide.Add(tagIndex, tagRotateValue); // 添加新键值对
            }
        }

        private void AddOrUpdateTagToASpide(int tagIndex, string tagRotateText)
        {
            int tagRotateValue = int.Parse(tagRotateText);

            if (TagToASpide.ContainsKey(tagIndex))
            {
                TagToASpide[tagIndex] = tagRotateValue; // 更新现有键的值
            }
            else
            {
                TagToASpide.Add(tagIndex, tagRotateValue); // 添加新键值对
            }
        }


        /// <summary>
        /// 信标编辑提交
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tag_Submit_Click(object sender, RoutedEventArgs e)
        {
            // 检查 TagX 和 TagY 是否为空
            if (string.IsNullOrWhiteSpace(TagX.Text) || string.IsNullOrWhiteSpace(TagY.Text))
            {
                MessageBox.Show("X 和 Y 坐标不能为空，请输入有效的值。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // 终止后续操作
            }

            foreach (WirePointArray item in MapInstrument.wirePointArrays)//查询所有关联线路
            {
                if (item.GetPoint.TagID.Equals(TagIndex) || item.GetWirePoint.TagID.Equals(TagIndex))//暂时移除拖动时关联线路
                {
                    Canvas.Children.Remove(item.GetPath);
                    if (item.Paths != null)
                    {
                        foreach (var ite in item.Paths)
                        {
                            Canvas.Children.Remove(ite);
                        }
                    }
                }
            }
            double logicX = Convert.ToDouble(TagX.Text.Trim()) / Painting.siseWin;
            double logicY = Convert.ToDouble(TagY.Text.Trim()) / Painting.siseWin;
            mapInstrument.UpdateTagLogicPosition(Convert.ToInt32(TagIndex), new Point(logicX, logicY));
            label.Margin = new Thickness(Convert.ToDouble(TagX.Text.Trim()) * Painting.siseWin, Convert.ToDouble(TagY.Text.Trim()) * Painting.siseWin, 0, 0);
            GetMovement(new Point() { X = logicX + 19, Y = logicY + 11.5 }, TagIndex);
            //AddOrUpdateTagRotate(TagIndex, TagRotate.Text);
            //AddOrUpdateTagToVSpide(TagIndex, v_desired.Text);
            //AddOrUpdateTagToASpide(TagIndex, a_desired.Text);
            this.Close();
        }

        /// <summary>
        /// 信标删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TagDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmToDel = MessageBox.Show("警告！确认要删除信标吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmToDel == MessageBoxResult.Yes)
            {
                tagManag.TagDelete(TagIndex, Canvas);
                this.Close();
            }

        }

        /// <summary>
        /// 自动导航
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TagAutoSearch_Click(object sender, RoutedEventArgs e)
        {
            double TagAutoX = Convert.ToDouble(TagX.Text.Trim());
            double TagAutoY = Convert.ToDouble(TagY.Text.Trim());

            //var factory = new MqttFactory();
            //mqttClient = factory.CreateMqttClient();

            //// 设置 MQTT 连接选项
            //var options = new MqttClientOptionsBuilder()
            //    .WithTcpServer("192.168.137.66", 1883) //  MQTT 服务器地址
            //    .Build();

            //// 连接到 MQTT 代理
            //mqttClient.ConnectAsync(options);


            //// 显示连接成功消息
            //MessageBox.Show("MQTT 连接成功！");
            
            ////mainPanel.Focus();

            ////订阅主题
            //mqttClient.SubscribeAsync("AGV/Carrier/Common").Wait();
            //mqttClient.SubscribeAsync("AGV/Response/MapDrawing").Wait();


            //生成路径
            //var location = ProcessLocationMessage();
            searchAlgorithm.Rrt(10, 10, (int)Math.Round(TagAutoX), (int)Math.Round(TagAutoY));

            //发送消息
            string filePath = @"E:\AGVCode\RTT\init_path\init_path1.txt";
            List<Point> points = Painting.ReadPointsFromFile(filePath);
            Painting.DrawPointsOnCanvas(Canvas, points);

            string json = Painting.GeneratePointDataJson(points);
            //mqttClient.PublishAsync("AGV/Carrier/MapLine", json).Wait();

        }

        /// <summary>
        /// 从AGV/Carrier/Common这个话题中获取xy坐标
        /// </summary>
        /// <returns></returns>
        public (double x, double y) ProcessLocationMessage()
        {
            double AutoX = 0;
            double AutoY = 0;
            mqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args =>
            {
                if (args.ApplicationMessage.Topic == "AGV/Carrier/Common")
                {
                    var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
                    //MessageBox.Show(payload);
                    var regex = new Regex(@"AgvId (\d+), LocationX ([-\d.]+), LocationY ([-\d.]+), LocationTheta ([-\d.]+)");
                    var match = regex.Match(payload);

                    if (match.Success)
                    {
                        //MessageBox.Show(payload);
                        double autoX = double.Parse(match.Groups[2].Value);
                        double autoY = double.Parse(match.Groups[3].Value);

                        // 在这里处理接收到的自动X和Y值
                        Console.WriteLine($"AutoX: {autoX}, AutoY: {autoY}");
                    }
                }
            });
            return (AutoX, AutoY);
        }

        private void TagTest_Click(object sender, RoutedEventArgs e)
        {
            string filePath = @"E:\AGVCode\RTT\init_path\init_path1.txt";
            List<Point> points = Painting.ReadPointsFromFile(filePath);
            Painting.DrawPointsOnCanvas(Canvas, points);
            this.Close();
        }

        private void UpdateLabelContent(object sender, TextChangedEventArgs e)
        {

        }

        private void TagX_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
