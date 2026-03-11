using AGVManagement.MapPaint;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using AGV.BLL;
using AGV.Models;
using System.Data;
using AGVManagement.instrument;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.IO;
using MQTTnet;
using AGVManagement.Mqtt;
using MQTTnet.Client;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using AGV.Models.Topic;
using System.Text.Json;
using MahApps.Metro.Controls;
using System.Xml;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Collections.ObjectModel;
using ControlzEx.Standard;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client.Options;
using Renci.SshNet.Common;
using System.Runtime.ConstrainedExecution;
using System.Web.Routing;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Net;
using System.Net.Http;
using System.Collections.Concurrent;
using AGV.DAL;
using MQTTnet.Client.Receiving;
using static AGVManagement.instrument.Planning;
using System.Web.UI;
using PersistentValueDemo;

namespace AGVManagement
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {                
        private IMqttClient mqttClient;
        private Car _car;
        private IMqttClient _mqttClient;
        private TagInfoBLL tagInfoBLL;
        private LineInfoBLL infoBLL;
        private Dispatcher _dispatcher;

        RouteInfoBLL routeInfoBLL = new RouteInfoBLL();

        private Point point;//记录滚动条位置动态调整生成控件位置
        Painting painting = new Painting();
        PointHandle pointHandle = new PointHandle();
        MapInstrument instrument = new MapInstrument();
        MapMessageBLL mapMessage = new MapMessageBLL();
        MqttConnect mqttConnect = new MqttConnect();
        RttInterface rttInterface = new RttInterface();
        double GrnWidth, GrnMpHeight;//默认宽高
        double NewActualHeight, NewActualWidth, NewScale;
        public static long Time;//测试，正常为私有
        private static bool _isAutoReconnectEnabled = true;

        private bool isUpKeyPressed = false;
        private bool isDownKeyPressed = false;
        private bool isLeftKeyPressed = false;
        private bool isRightKeyPressed = false;
        private DispatcherTimer timerS;
        private DispatcherTimer timerM;
        private CarData _data;

        // 用于保存线路快照
        private List<WirePointArray> wireSnapshot = new List<WirePointArray>();

        //public static MqttClientWrapper _mqttClientWrapper;
        //private List<MqttClientWrapper> _mqttClients = new List<MqttClientWrapper>();

        public MainWindow(long Times, string MapNa, string MapNs, double Width, double HeMap, double NewWidth, double NewHeMap,  double ScaleRatio)
        {
            InitializeComponent();
            MapWindowSessionInitializer.ResetMapDrawingState(mainPanel);
            Time = Times;
            LoadMs(Times, MapNa, MapNs, Width, HeMap, NewWidth, NewHeMap,  ScaleRatio);
            //Loaded += MainWindow_Loaded;
            infoBLL = new LineInfoBLL();
            tagInfoBLL = new TagInfoBLL();

            _dispatcher = Dispatcher;

            TaskChecker taskChecker = new TaskChecker();

            //SetupMqttClient();
        }


        // --------- 快照方法 ----------
        // 是否是第一次激活
        private bool _isFirstActivated = true;

        //private void SaveWireSnapshot()
        //{
        //    wireSnapshot.Clear();
        //    foreach (var wp in MapInstrument.wirePointArrays)
        //    {
        //        wireSnapshot.Add(wp.Clone()); // 克隆逻辑线路
        //    }
        //}


        //private void RestoreWireSnapshot()
        //{
        //    MapInstrument.wirePointArrays.Clear();
        //    foreach (var wp in wireSnapshot)
        //    {
        //        MapInstrument.wirePointArrays.Add(wp.Clone());
        //    }
        //}

        ////// --------- 切走页面前调用 ----------
        //protected override void OnDeactivated(EventArgs e)
        //{
        //    base.OnDeactivated(e);

        //    // 只有不是第一次切走才保存
        //    if (!_isFirstActivated)
        //    {
        //        SaveWireSnapshot();
        //    }
        //}

        //protected override void OnActivated(EventArgs e)
        //{
        //    base.OnActivated(e);

        //    if (_isFirstActivated)
        //    {
        //        _isFirstActivated = false;
        //        return; // 第一次打开，不恢复快照
        //    }

        //    // 只有切回来的时候才恢复
        //    RestoreWireSnapshot();
        //}




        // 处理窗口关闭事件
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                await MqttShutdownService.CloseAllConnectionsAsync();
                AgvDataViewResetter.Reset(GlobalData.AgvData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭连接时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMs(long Times, string MapNa, string MapNs, double Width, double HeMap, double NewWidth, double NewHeMap, double ScaleRatio)
        {
            if (!Times.Equals(0))
            {
                   MapInfo(Times, MapNa);
            }
            else
            {
                CanvasMp(Width, HeMap, MapNs, Width, HeMap, NewWidth, NewHeMap,  ScaleRatio);
            }
        }

        // 设置背景图片
        public void SetBackgroundImage(string imagePath)
        {
            try
            {
                // 检查文件是否存在
                if (File.Exists(imagePath))
                {
                    // 创建 ImageBrush 对象
                    ImageBrush mapBackground = new ImageBrush();
                    mapBackground.ImageSource = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));

                    // 将背景应用于名为 "Geenh" 的地图框
                    Geenh.Background = mapBackground;
                }
                else
                {
                    MessageBox.Show("图片路径不存在，请检查！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置背景失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //新的地图界面缩放
        private void SrcCount_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                e.Handled = true;

                double zoomFactor = 0.01;
                double scale = MapScaleTransform.ScaleX;

                if (e.Delta > 0)    
                    scale += zoomFactor;
                else
                    scale -= zoomFactor;

                if (scale < 0.1) scale = 0.1;
                if (scale > 5.0) scale = 5.0;   


                MapScaleTransform.ScaleX = scale;
                MapScaleTransform.ScaleY = scale;
            }
        }

        // 新增：Ctrl + 右键恢复原比例
        private void SrcCount_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                MapScaleTransform.ScaleX = 1.0;
                MapScaleTransform.ScaleY = 1.0;
                e.Handled = true; // 阻止默认右键行为（比如菜单）
            }
        }


        private void MapInfo(long Times, string MapN)
        {
            DataTable da = mapMessage.MapParray(MapN);
            if (da != null)
            {
                foreach (DataRow data in da.Rows)
                {
                    double Width = Convert.ToDouble(data["Width"].ToString());
                    double Height = Convert.ToDouble(data["Height"].ToString());
                    double NewActualWidth = Convert.ToDouble(data["ActualWidth"].ToString());
                    double NewActualHeight = Convert.ToDouble(data["ActualHeight"].ToString());
                    double NewScale = Convert.ToDouble(data["Scale"].ToString());

                    // 更新 MqttClientWrapper 静态字段
                    MqttClientWrapper.ActualWidthNow = NewActualWidth;
                    MqttClientWrapper.ActualHeightNow = NewActualHeight;
                    MqttClientWrapper.ProportionNow = NewScale;


                    GrnWidth = Width * 10;
                    GrnMpHeight = Height * 10;

                    int MPType = Convert.ToInt32(data["Type"].ToString());
                    if (MPType.Equals(0))
                    {
                        TypeMp.SelectedIndex = 1;
                    }
                    else if (MPType.Equals(1))
                    {
                        TypeMp.SelectedIndex = 0;
                    }
                    else
                    {
                        TypeMp.SelectedIndex = 2;
                    }
                    CanvasMp(Width, Height, data["Name"].ToString(), Width, Height, NewActualWidth, NewActualHeight, NewScale);
                    instrument.LoadDataInfo(mainPanel, Times);
                }
            }
            else
            {
                MessageBox.Show("地图丢失");
                
            }
        }
        private void CanvasMp(double Width, double Height,string TX,double WP,double Wh, double NewActualWidthNow, double NewActualHeightNow, double ScaleRatio)
        {
            MP.Text = TX;
            MpWidth.Content = WP + "m";
            MpHeight.Content = Wh + "m";
            GrnWidth = Width * 10;
            GrnMpHeight = Height * 10; 
            mainPanel.Width = Width * 10;
            mainPanel.Height = Height * 10;
            TopX.Width = Width * 10;
            TopY.Height = Height * 10;
            NewActualWidth = NewActualWidthNow;
            NewActualHeight = NewActualHeightNow;
            NewScale = ScaleRatio;
            painting.Coordinate(mainPanel);
            painting.CoordinateX(TopX, TopY);
        }
        SaveMap map = new SaveMap();

        /// <summary>
        /// 保存地图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            int TypeID =0;
            if (TypeMp.SelectedIndex.Equals(0))
            {
                TypeID = 1;
            }
            else if (TypeMp.SelectedIndex.Equals(1))
            {
                TypeID = 0;

            }
            else
            {
                TypeID = 2;
            }
            bool mp = map.SaveAtlas((!Time.Equals(0) ? Time.ToString() : UTC.ConvertDateTimeLong(DateTime.Now).ToString()), !Time.Equals(0) ? false : true, MP.Text, (GrnWidth / 10), (GrnMpHeight / 10),"0", TypeID, NewActualWidth, NewActualHeight, NewScale);
            if (mp)
            {
                MessageBox.Show("保存成功");
            }
            else
            {
                MessageBox.Show("保存失败");
            }
        }

        /// <summary>
        /// 导出地图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Export_Click(object sender, RoutedEventArgs e)
        {

        }


        /// <summary>
        /// 添加信标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tags_Click(object sender, RoutedEventArgs e)
        {
            BgColors(Tags);
            instrument.TagNew(mainPanel, point);
        }


        /// <summary>
        /// 滚动条滚动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SrcCount_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            point.X = e.HorizontalOffset;
            point.Y = e.VerticalOffset;
            SrcX.ScrollToHorizontalOffset(e.HorizontalOffset);//X轴标尺跟随移动
            SrcY.ScrollToVerticalOffset(e.VerticalOffset); //Y轴标尺等随移动
        }

        /// <summary>
        /// 比例尺滑动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliMax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            painting.Mapmagnify(Convert.ToInt32(e.NewValue), TopX, TopY, mainPanel, GrnWidth, GrnMpHeight);//地图比例尺缩放
        }

        /// <summary>
        /// 添加区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Area_Click(object sender, RoutedEventArgs e)
        {
            //BgColors(Area);
            //instrument.MapAreaNew(mainPanel, point);
            //FileHelper fileHelper = new FileHelper();
            //List<Point> coordinatesRtt = rttInterface.ReadCoordinates(@"E:\AGVCode\RTT\scheduled_path\scheduled_path1.txt");

            //// 绘制圆点和连线
            //rttInterface.DrawPointsAndLines(mainPanel, coordinatesRtt);
            try
            {
                // 成功提示弹框
                MessageBox.Show("Mes系统连接已建立！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                // 按钮点击后开始处理任务
                await ProcessTasksSequentially();
            }
            catch (Exception ex)
            {
                // 如果发生异常，弹出错误提示框
                MessageBox.Show($"连接错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //输出点位集合
        private List<Point> ExtractPoints(DataTable routes)
        {
            List<Point> points = new List<Point>();

            for (int i = 0; i < routes.Rows.Count; i++)
            {       
                DataRow row = routes.Rows[i];

                // 添加 StartX 和 StartY 到列表中
                points.Add(new Point(Convert.ToDouble(row["StartX"]), Convert.ToDouble(row["StartY"])));

                // 如果是最后一行，添加 EndX 和 EndY
                if (i == routes.Rows.Count - 1)
                {
                    points.Add(new Point(Convert.ToDouble(row["EndX"]), Convert.ToDouble(row["EndY"])));
                }
            }

            return points;          
        }

        private DataTable TransformDataTable(DataTable inputTable)
        {
            DataTable transformedTable = new DataTable();
            transformedTable.Columns.Add("StartX", typeof(double));
            transformedTable.Columns.Add("StartY", typeof(double));
            transformedTable.Columns.Add("EndX", typeof(double));
            transformedTable.Columns.Add("EndY", typeof(double));

            foreach (DataRow row in inputTable.Rows)
            {
                DataRow newRow = transformedTable.NewRow();

                // 乘以10后加上/减去指定值
                newRow["StartX"] = Convert.ToDouble(row["StartX"]) * 10;
                newRow["StartY"] = Convert.ToDouble(row["StartY"]) * 10;
                newRow["EndX"] = Convert.ToDouble(row["EndX"]) * 10 ;
                newRow["EndY"] = Convert.ToDouble(row["EndY"]) * 10;

                transformedTable.Rows.Add(newRow);
            }

            return transformedTable;
        }

        /// <summary>
        /// 充电测试按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void InsertImg_Click(object sender, RoutedEventArgs e)
        {
            //int result1 = MemoryKeeper.AddAndRemember(1);
            //int result2 = MemoryKeeper.AddAndRemember(2);
            //int result3 = MemoryKeeper.AddAndRemember(3);

            //数据库读取
            DataTable lineStation1 = infoBLL.LinelistArrer(Time.ToString());
            DataTable pointTable = Painting.GenerateTagTableWithNeighbors(lineStation1, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);

            Planning planning = new Planning();
            List<PointData> pointList = ConvertToPointList(pointTable);

            DataTable TagStation = tagInfoBLL.RataTable(Time.ToString());

            var coordinates = GetTagCoordinates(TagStation, "TA2");
            double xA = Convert.ToDouble(coordinates.Value.X);
            double yA = Convert.ToDouble(coordinates.Value.Y);
            Point pointA = new Point(10 * xA, 10 * yA);
            Point pointByANew = painting.TransformCoordinate(pointA, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);

            var coordinates1 = GetTagCoordinates(TagStation, "TA8");
            double xA1 = Convert.ToDouble(coordinates1.Value.X);
            double yA1 = Convert.ToDouble(coordinates1.Value.Y);
            Point pointA1 = new Point(10 * xA1, 10 * yA1);
            Point pointByNewA1 = painting.TransformCoordinate(pointA1, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);

            // 获取当前选中的 AGV 地址
            string currentAgv = MqttConnectionManager.CurrentAddress;

            // 时间戳/ms
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 构造 Tasks 列表
            var tasks = new List<(string agent_name, int start_id, int goal_id, int task_priority, long? timestamp)>
            {
                (currentAgv, 2, 8, 8, nowMs),   // 示例任务
                (currentAgv, 10, 15, 5, nowMs)  // 也可以加多个任务
            };

            // 构造 Clusters（自定义）
            var clusters = new Planning.ClusterWrapper();
            clusters.clusters.Add(new Planning.Cluster
            {
                name = "cluster1",
                area = new List<int> { 4, 5, 6, 13, 14, 15 },
                parking_area = new List<int> { 13, 14, 15 },
                temp_area = new List<int> { 4, 6 },
                entry_area = new Dictionary<string, List<int>> { { "4", new List<int> { 12, 21, 20 } }, { "6", new List<int> { 16, 22, 23 } } },
                exit_area = new Dictionary<string, List<int>> { { "4", new List<int> { 2, 1, 10 } }, { "6", new List<int> { 8, 9, 18 } } },
                light = 0,
                time = 0,
                forward_green_start_time = 0,
                backward_green_start_time = 1000000000000,
                fix_per_cycle = 2,
                direction = new List<int> { 1, 0 }
            });

            try
            {
                //string json = Planning.BuildMapJson(pointTable, tasks, clusters);
                string json = File.ReadAllText(@"E:\ATF\code\test.json");
                string output = CppCBSLib.GetMultiAgentPaths(json);
                Console.WriteLine("DLL 输出结果:");
                //Console.WriteLine(output);
                var result = Painting.ParseMultiAgentPaths(output);
                await Painting.ProcessAndPublishPerAgentAsync(output);
                System.Windows.MessageBox.Show(output, "DLL 输出结果", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("调用 DLL 出错: " + ex.Message, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }


            //try
            //{
            //    //var mqttClientWrapper = new MqttClientWrapper(this.Dispatcher);
            //    //string topic = "AGV/Carrier/Common";
            //    //var message = await mqttClientWrapper.GetMessageFromTopicAsync(topic);
            //    AutoPath autoPathDialog = new AutoPath();
            //    autoPathDialog.ShowDialog();

            //    var dataAuto = autoPathDialog.dataAutoStatu;


            //    var data = JsonSerializer.Deserialize<CarData>(MqttClientWrapper.payloadAll);

            //    Point newPoint1 = new Point(data.X, data.Y);

            //    //Point point1 = new Point(0,0);



            //    Point newPoint2 = painting.ReverseTransformCoordinate(newPoint1, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);
            //    double newX = ((newPoint2.X) / 10) ;
            //    double newY = ((newPoint2.Y) / 10) ;
            //    Point newPoint3 = new Point(newX, newY);
            //    //数据库读取
            //    DataTable lineStation = infoBLL.LinelistArrer(Time.ToString());
            //    DataTable dataTable = new DataTable();
            //    dataTable = Painting.ExpandBidirectionalRoutes(lineStation);

            //    DataTable newLine = Painting.FindRouteNew(dataTable, newPoint3, dataAuto);
            //    newLine = Painting.ProcessDataTable(newLine);
            //    //转化表结构
            //    DataTable newLine1 = TransformDataTable(newLine);

            //    //转化准备输出json
            //    List<Point> points = ExtractPoints(newLine1);

            //    List<Tuple<Point, int, double, double>> stagingTagPointRatate1 = new List<Tuple<Point, int, double, double>>();

            //    //把points放入Tuple
            //    int rotateValue1 = 0;
            //    double rotateValue2 = 0.7;
            //    double rotateValue3 = 0.3;

            //    foreach (Point point in points)
            //    {
            //        stagingTagPointRatate1.Add(new Tuple<Point, int, double, double>(point, rotateValue1, rotateValue2, rotateValue3));
            //    }

            //    List<Tuple<Point, int, double, double>> transformedPointsByRotate = painting.TransformCoordinatesByRotate(stagingTagPointRatate1, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);
            //    List<newPointStraightWithAngle> processedPoints = pointHandle.ProcessPointsByRotate(transformedPointsByRotate, 0.001);

            //    string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            //    string fileName = "AutomaticPoints.json";

            //    string filePath = System.IO.Path.Combine(folderPath, fileName);

            //    newStationData stationData = new newStationData();

            //    stationData.Stations.AddRange(processedPoints);

            //    //输出点位
            //    string jsonString = JsonSerializer.Serialize(stationData, new JsonSerializerOptions { WriteIndented = true });
            //    File.WriteAllText(filePath, jsonString);

            //    var client = MqttConnectionManager.LatestClient;
            //    if (client != null && client.IsConnected)
            //    {
            //        var message = new MqttApplicationMessageBuilder()
            //            .WithTopic("AGV/Carrier/MapLineAuto")
            //            .WithPayload(jsonString)
            //            .WithExactlyOnceQoS()
            //            .WithRetainFlag(false)
            //            .Build();

            //        await client.PublishAsync(message);
            //    }
            //    else
            //    {
            //        MessageBox.Show("MQTT客户端未连接，请先建立连接。");
            //    }

            //    // 成功提示弹框
            //    MessageBox.Show("数据处理成功，文件已生成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
            //catch (Exception ex)
            //{
            //    // 如果发生异常，弹出错误提示框
            //    MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //}

        }










        double value1 = 1;
        double value2 = 1;


        //private List<Point> coordinates;
        /* private async void Timer_Tick(object sender, EventArgs e)
        {
            //获取地图坐标数据并显示
            mqttClient.ApplicationMessageReceived += (s, args) =>
            {
                if (args.ApplicationMessage.Topic == "AGV/Response/MapDrawing")
                {
                    var message = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
                    //MessageBox.Show(receiveMessage, "收到消息");
                    // 如果已经读取过坐标数据，则直接返回
                    // 从字符串中读取坐标数据
                    string[] lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    //MessageBox.Show(message,"更新后的message");
                    

                    // 创建坐标列表
                    List<Point> coordinates = new List<Point>();

                    int angleIndex = 0; // 角度索引

                    foreach (string line in lines)
                    {
                        // 按逗号分割每行数据
                        string[] values = line.Split(',');

                        foreach (string value in values)
                        {
                            // 检查数据是否有效，且为 double 类型
                            if (double.TryParse(value, out double distance))
                            {
                                // 计算每次0.8度偏转的角度，并转换为弧度
                                double angle = angleIndex * 0.8;
                                double radian = angle * Math.PI / 180.0;

                                // 根据距离和角度计算xy坐标点
                                double x = distance * Math.Cos(radian);
                                double y = distance * Math.Sin(radian);

                                // 将坐标点添加到列表中
                                coordinates.Add(new Point(x, y));

                                angleIndex++;
                            }
                        }

                    }

                    var positionMap = new Point();

                    if (args.ApplicationMessage.Topic == "AGV/Carrier/Common")
                    {
                        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
                        //MessageBox.Show(payload);
                        var regex = new Regex(@"AgvId (\d+), LocationX ([-\d.]+), LocationY ([-\d.]+), LocationTheta ([-\d.]+)");
                        var match = regex.Match(payload);

                        if (match.Success)
                        {
                            //MessageBox.Show(payload);
                            double x = double.Parse(match.Groups[2].Value);
                            double y = double.Parse(match.Groups[3].Value);
                            double angle = double.Parse(match.Groups[4].Value);

                            // 构造圆点的位置和半径
                            positionMap = new Point(x * 10, y * 10);
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        painting.DrawDotsNow(mainPanel, coordinates, 1 , positionMap);
                    });
                }



                // 解析消息中的 X 和 Y 的值
                if (args.ApplicationMessage.Topic == "AGV/Carrier/Common")
                {
                    var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
                    //MessageBox.Show(payload);
                    var regex = new Regex(@"AgvId (\d+), LocationX ([-\d.]+), LocationY ([-\d.]+), LocationTheta ([-\d.]+)"); 
                    var match = regex.Match(payload);

                    if (match.Success)
                    {
                        //MessageBox.Show(payload);
                        double x = double.Parse(match.Groups[2].Value);
                        double y = double.Parse(match.Groups[3].Value);
                        double angle = double.Parse(match.Groups[4].Value);

                        // 
                        var position = new Point(x*10, y*10);
                        //double radius = 5;
                        double width = 30;
                        double height = 15;
                        //Brush fillColor = Brushes.Red; // 整体填充颜色为红色
                        //Brush frontColor = Brushes.Blue; // 前方颜色为蓝色
                        //MessageBox.Show(x.ToString());
                        // 重绘画板
                        Dispatcher.Invoke(() =>
                        {
                            painting.DrawRectangle(mainPanel, position, width, height, angle);
                        });
                    }
                }
            };

            if (mqttClient.IsConnected)
           {
                if (isUpKeyPressed)
                {
                    await mqttClient.PublishAsync("agv/cmd_vel", "0.5, 0");
                }
                else if (isDownKeyPressed)
                {
                    await mqttClient.PublishAsync("agv/cmd_vel", "-0.5, 0");
                }
                else if (isLeftKeyPressed)
                {
                    await mqttClient.PublishAsync("agv/cmd_vel", "0, 3");
                }
                else if (isRightKeyPressed)
                {
                    await mqttClient.PublishAsync("agv/cmd_vel", "0, 0");
                }
            }
        }*/

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                isUpKeyPressed = true;
                //Keyboard.Focus(mainPanel);
            }
            else if (e.Key == Key.Down)
            {
                isDownKeyPressed = true;
                //Keyboard.Focus(mainPanel);
            }
            else if (e.Key == Key.Left)
            {
                isLeftKeyPressed = true;
                //Keyboard.Focus(mainPanel);
            }
            else if (e.Key == Key.Right)
            {
                isRightKeyPressed = true;
                //Keyboard.Focus(mainPanel);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                isUpKeyPressed = false;
            }
            else if (e.Key == Key.Down)
            {
                isDownKeyPressed = false;
            }
            else if (e.Key == Key.Left)
            {
                isLeftKeyPressed = false;
            }
            else if (e.Key == Key.Right)
            {
                isRightKeyPressed = false;
            }
        }

        /// <summary>
        /// 绘制直线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Straight_Click(object sender, RoutedEventArgs e)
        {
            BgColors(Straight);
            instrument.Mapstraight();
        }

        /// <summary>
        /// 鼠标点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_mouse_Click(object sender, RoutedEventArgs e)
        {
            BgColors(btn_mouse);
            instrument.mouseStatic();
        }

        private void BgColors(Button button)
        {
            // 定义默认背景色和前景色
            Color defaultBackgroundColor = Color.FromRgb(96, 125, 139);
            Color defaultForegroundColor = Colors.White;

            // 定义高亮背景色和前景色
            Color highlightedBackgroundColor = Color.FromRgb(249, 92, 38);
            Color highlightedForegroundColor = Colors.White;

            // 重置所有按钮为默认颜色
            Bgbtn(DeleteCircuit, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Btn_cren, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(btn_mouse, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Straight, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(InsertImg, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Area, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Tags, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Btn_Text, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Broken, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Btn_bezierCurve, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Btn_RoutePlanning, defaultBackgroundColor, defaultForegroundColor);
            Bgbtn(Btn_LineDistribution, defaultBackgroundColor, defaultForegroundColor);

            // 设置点击的按钮为高亮颜色
            button.Background = new SolidColorBrush(highlightedBackgroundColor);
            button.Foreground = new SolidColorBrush(highlightedForegroundColor);
        }

        private void Bgbtn(Button button, Color backgroundColor, Color foregroundColor)
        {
            button.Background = new SolidColorBrush(backgroundColor);
            button.Foreground = new SolidColorBrush(foregroundColor);
        }

        /// <summary>
        /// 折线线路
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Broken_Click(object sender, RoutedEventArgs e)
        {
            instrument.Brokene();
            BgColors(Broken);
        }

        /// <summary>
        /// 建图测试(原线路选定)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RoutePlanningButtonClick(object sender, RoutedEventArgs e)
        {
            //string folderPath = @"E:\AGVInformation\AGV_C_1104";
            //string fileName = "pointsTest20231218.txt";
            //string filePath = System.IO.Path.Combine(folderPath, fileName);

            //List<Point> Points = painting.GetLinePoints(new Point(505, 420), new Point(555,420));

            //List<Point> PointsTest = Painting.GetArcPoints(new Point(555, 420), new Point(585, 390), new Point(555, 390), 30);
            //Points.AddRange(PointsTest);
            //List<Point> PointsTestNow = painting.TransformCoordinates(Points, 515, 400, 42.8571);
            //painting.SavePointsToFile(PointsTestNow, filePath);

            //instrument.tagLocking = true;
            try 
            {
                var client = MqttConnectionManager.LatestClient;
                if (client != null && client.IsConnected)
                {

                    //建图发送数据（需要时解开注释）
                    var message = new MqttApplicationMessageBuilder()
                    .WithTopic("AGV/Response/Mapping")
                    .WithPayload("Mapping")
                    .WithExactlyOnceQoS()
                    .WithRetainFlag(false)
                    .Build();

                    //定位发送数据（需要时解开注释）
                    /*var message = new MqttApplicationMessageBuilder()
                        .WithTopic("AGV/Response/Orientation")
                        .WithPayload("Orientation")
                        .WithExactlyOnceQoS()
                        .WithRetainFlag(false)
                        .Build();*/

                    await client.PublishAsync(message);
                    // 操作成功，显示提示框
                    MessageBox.Show("建图脚本已下发。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                MessageBox.Show("MQTT客户端未连接，请先建立连接。");
                }
            }
            catch (Exception ex)
            {
                // 捕获异常，显示错误消息
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// 启动任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Btn_TaskTesting_Click(object sender, RoutedEventArgs e)
        {
            try {
                var client = MqttConnectionManager.LatestClient;
                if (client != null && client.IsConnected)
                {
                    //发送信息
                    var message = new MqttApplicationMessageBuilder()
                    .WithTopic("AGV/Response/TaskDistribution")
                    .WithPayload("TaskDistribution")
                    .WithExactlyOnceQoS()
                    .WithRetainFlag(false)
                    .Build();

                await client.PublishAsync(message);
                    // 操作成功，显示提示框
                    MessageBox.Show("任务脚本已下发。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("MQTT客户端未连接，请先建立连接。");
            }
                
            }
            catch (Exception ex)
            {
                // 捕获异常，显示错误消息
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// 任务终止按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Btn_TaskTermination_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmToDel = MessageBox.Show("警告！确认要终止任务吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmToDel == MessageBoxResult.Yes)
            {
                try
                {
                    var client = MqttConnectionManager.LatestClient;
                    if (client != null && client.IsConnected)
                    {
                        //发送信息
                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic("AGV/Response/TaskTermination")
                            .WithPayload("TaskTermination")
                            .WithExactlyOnceQoS()
                            .WithRetainFlag(false)
                            .Build();

                        await client.PublishAsync(message);
                        // 操作成功，显示提示框
                        MessageBox.Show("终止任务脚本已下发。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("MQTT客户端未连接，请先建立连接。");
                    }

                }
                catch (Exception ex)
                {
                    // 捕获异常，显示错误消息
                    MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }


        //点位角度计算
        public static double CalculateAngle(double x1, double y1, double x2, double y2)
        {
            double deltaY = y2 - y1;
            double deltaX = x2 - x1;
            double angleInDegrees = Math.Atan2(deltaY, deltaX);
            //   -π  π
            if (angleInDegrees == Math.PI)
            {
                return -Math.PI;
            }
            else
            {
                return angleInDegrees;
            }
        }

        //点位输出，包含顶升
        public static List<PointStraightWithAngle> ProcessPointsByRotate(List<Tuple<Point, int, int, int>> transformedPointsByRotate, double insertDistance)
        {
            List<PointStraightWithAngle> result = new List<PointStraightWithAngle>();
            int idCounter = 0;

            for (int i = 0; i < transformedPointsByRotate.Count - 1; i++)
            {
                Point p1 = transformedPointsByRotate[i].Item1;
                Point p2 = transformedPointsByRotate[i + 1].Item1;
                int value1 = transformedPointsByRotate[i].Item2;
                int value2 = transformedPointsByRotate[i + 1].Item2;

                int value3 = transformedPointsByRotate[i].Item3;
                int value5 = transformedPointsByRotate[i+1].Item3;

                int value4 = transformedPointsByRotate[i].Item4;
                int value6 = transformedPointsByRotate[i+1].Item4;
                // 原始点
                double angleToNext = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y);
                result.Add(new PointStraightWithAngle(idCounter++, p1.X, p1.Y, angleToNext, value3, value4, value1, true));

                // 插入点
                double totalDistance = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                if (totalDistance > insertDistance)
                {
                    double ratio = insertDistance / totalDistance;
                    double newX = p2.X - ratio * (p2.X - p1.X);
                    double newY = p2.Y - ratio * (p2.Y - p1.Y);
                    angleToNext = CalculateAngle(newX, newY, p2.X, p2.Y);
                    result.Add(new PointStraightWithAngle(idCounter++, newX, newY, angleToNext, value5, value6, 0, false));
                }
            }

            // 添加最后一个原始点
            if (transformedPointsByRotate.Count > 0)
            {
                Point lastPoint = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item1;
                int lastValue = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item2;
                int lastVSpide = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item3;
                int lastASpide = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item4;

                // 如果列表中至少有两个点，则计算最后一个点的角度
                double angleToPrevious = 0;
                if (transformedPointsByRotate.Count > 1)
                {
                    Point secondLastPoint = transformedPointsByRotate[transformedPointsByRotate.Count - 2].Item1;
                    angleToPrevious = CalculateAngle(secondLastPoint.X, secondLastPoint.Y, lastPoint.X, lastPoint.Y);
                }

                result.Add(new PointStraightWithAngle(idCounter++, lastPoint.X, lastPoint.Y, angleToPrevious, lastVSpide, lastASpide, lastValue, true));
            }

            return result;
        }

        //点位输出，包含顶升（新增了个Turn）
        public static List<newPointStraightWithAngle> ProcessPointsByTurnRotate(List<Tuple<Point, int, double, double, double, double>> transformedPointsByRotate, double insertDistance)
        {
            List<newPointStraightWithAngle> result = new List<newPointStraightWithAngle>();
            int idCounter = 0;

            for (int i = 0; i < transformedPointsByRotate.Count - 1; i++)
            {
                Point p1 = transformedPointsByRotate[i].Item1;
                Point p2 = transformedPointsByRotate[i + 1].Item1;
                int value1 = transformedPointsByRotate[i].Item2;
                int value2 = transformedPointsByRotate[i + 1].Item2;

                double value3 = transformedPointsByRotate[i].Item3;
                double value5 = transformedPointsByRotate[i + 1].Item3;

                double value4 = transformedPointsByRotate[i].Item4;
                double value6 = transformedPointsByRotate[i + 1].Item4;

                double value7 = transformedPointsByRotate[i].Item6;
                double value8 = transformedPointsByRotate[i+1].Item6;

                double angleToNext;

                // 判断 Item5 的值
                if (transformedPointsByRotate[i].Item5 == 1)
                {
                    // 使用与下一个点的方向角度
                    angleToNext = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y);

                }
                else
                {
                    // 反向使用当前点到下一个点的角度（弧度）
                    angleToNext = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y);
                    angleToNext = (angleToNext + Math.PI) % (2 * Math.PI);  // 反向计算（弧度）
                    if (angleToNext < -Math.PI)
                        angleToNext += 2 * Math.PI;
                    else if (angleToNext > Math.PI)
                        angleToNext -= 2 * Math.PI;
                }

                // 添加当前点
                result.Add(new newPointStraightWithAngle(idCounter++, p1.X, p1.Y, angleToNext, value3, value4, value1, true, value7));

                // 插入点
                double totalDistance = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                if (totalDistance > insertDistance)
                {
                    double ratio = insertDistance / totalDistance;
                    double newX = p2.X - ratio * (p2.X - p1.X);
                    double newY = p2.Y - ratio * (p2.Y - p1.Y);

                    // 使用前一个 true 点的角度，而不是当前插入点的角度
                    double insertAngle = angleToNext;  // 使用前一个 true 点的角度

                    result.Add(new newPointStraightWithAngle(idCounter++, newX, newY, insertAngle, value5, value6, 0, false, value8));
                }
            }

            // 添加最后一个原始点
            if (transformedPointsByRotate.Count > 0)
            {
                Point lastPoint = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item1;
                int lastValue = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item2;
                double lastVSpide = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item3;
                double lastASpide = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item4;
                double lastTAngle = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item5;
                double lastOavoidance = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item6;

                // 如果列表中至少有两个点，则计算最后一个点的角度
                double angleToPrevious = 0;
                if (transformedPointsByRotate.Count > 1)
                {
                    Point secondLastPoint = transformedPointsByRotate[transformedPointsByRotate.Count - 2].Item1;
                    angleToPrevious = CalculateAngle(secondLastPoint.X, secondLastPoint.Y, lastPoint.X, lastPoint.Y);
                }

                // 最后一个点的角度根据 Item5 的值来决定
                if (lastTAngle == 1)
                {
                    result.Add(new newPointStraightWithAngle(idCounter++, lastPoint.X, lastPoint.Y, angleToPrevious, lastVSpide, lastASpide, lastValue, true, lastOavoidance));
                }
                else
                {
                    // 如果最后一个点的角度不是 1，则反向计算（弧度）
                    lastTAngle = (angleToPrevious + Math.PI) % (2 * Math.PI);
                    if (lastTAngle < -Math.PI)
                        lastTAngle += 2 * Math.PI;
                    else if (lastTAngle > Math.PI)
                        lastTAngle -= 2 * Math.PI;
                    result.Add(new newPointStraightWithAngle(idCounter++, lastPoint.X, lastPoint.Y, lastTAngle, lastVSpide, lastASpide, lastValue, true, lastOavoidance));
                }
            }

            return result;
        }


        //点位输出（不包含顶升）
        public static List<PointStraightWithAngle> ProcessPoints(List<Point> points, double insertDistance)
        {
            List<PointStraightWithAngle> result = new List<PointStraightWithAngle>();
            int idCounter = 0;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];

                // 原始点
                double angleToNext = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y);
                result.Add(new PointStraightWithAngle(idCounter++, p1.X, p1.Y, angleToNext, 6.0, 5.0, 0, true));

                // 插入点
                double totalDistance = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                if (totalDistance > insertDistance)
                {
                    double ratio = insertDistance / totalDistance;
                    double newX = p2.X - ratio * (p2.X - p1.X);
                    double newY = p2.Y - ratio * (p2.Y - p1.Y);
                    angleToNext = CalculateAngle(newX, newY, p2.X, p2.Y);
                    result.Add(new PointStraightWithAngle(idCounter++, newX, newY, angleToNext, 6.0, 5.0, 0, false));
                }
            }

            // 添加最后一个原始点
            if (points.Count > 0)
            {
                Point lastPoint = points[points.Count - 1];
                result.Add(new PointStraightWithAngle(idCounter++, lastPoint.X, lastPoint.Y, 0, 6.0, 5.0, 0, true));
              }

            return result;
        }

        //直线均匀输出点位
        public static List<PointStraightWithAngle> GeneratePointsBetween(List<Point> drawingPoints, double interval)
        {
            List<PointStraightWithAngle> generatedPoints = new List<PointStraightWithAngle>();
            int idCounter = 1;

            for (int i = 0; i < drawingPoints.Count - 1; i++)
            {
                Point start = drawingPoints[i];
                Point end = drawingPoints[i + 1];
                double distance = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
                double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);

                int numberOfPoints = (int)(distance / interval);

                // Add the starting point of each segment with rotate = true
                generatedPoints.Add(new PointStraightWithAngle(idCounter++, start.X, start.Y, angle, 6.0, 5.0, 0, true));

                for (int j = 1; j < numberOfPoints; j++)
                {
                    double ratio = (double)j / numberOfPoints;
                    double x = start.X + ratio * (end.X - start.X);
                    double y = start.Y + ratio * (end.Y - start.Y);

                    PointStraightWithAngle point = new PointStraightWithAngle(idCounter++, x, y, angle, 6.0, 5.0, 0, false);
                    generatedPoints.Add(point);
                }
            }

            // Add the last point of the last segment with rotate = true
            Point lastPoint = drawingPoints[drawingPoints.Count - 1];
            generatedPoints.Add(new PointStraightWithAngle(idCounter++, lastPoint.X, lastPoint.Y, 0, 6.0, 5.0, 0, true));

            return generatedPoints;
        }

        /// <summary>
        /// 定位发送测试(原线路下发)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LineDistributionButtonClick(object sender, RoutedEventArgs e)
        {
            //曲线输出逻辑
            //List<Point> drawingPoints = new List<Point>();
            //List<Point> stagingTagPoint = MapInstrument.stagingTagPoint;
            //instrument.tagLocking = false;
            //for (int i = 0; i < stagingTagPoint.Count - 1; i++)
            //{
            //    Point startPoint = stagingTagPoint[i];
            //    Point endPoint = stagingTagPoint[i + 1];
            //    drawingPoints.AddRange(painting.DrawingBrokenPoint(startPoint, endPoint));
            //    MessageBox.Show(startPoint.ToString());
            //    MessageBox.Show(endPoint.ToString());
            //}
            //List<Point> targetLocation = painting.TransformCoordinates(drawingPoints, 470, 450, 42.105);
            //List<PointDataMapLine> pointDataListByAngle = painting.GeneratePointDataByCurve(targetLocation);

            //string folderPath = @"E:\AGVInformation\AGV_C_1104";
            //string fileName = "TargetPoints.txt";
            //string fileNameByAngle = "TargetPointsByAngle.txt";

            //string filePath = System.IO.Path.Combine(folderPath, fileName);
            //string filePathByAngle = System.IO.Path.Combine(folderPath, fileNameByAngle);

            //painting.SavePointsToFile(targetLocation, filePath);
            //painting.SavePointsToFileByAngle(pointDataListByAngle, filePathByAngle);

            //painting.TransformPointsByAngle(filePath, -2.55, -0.7, 13);
            //MapInstrument.stagingTagId.Clear();
            //MapInstrument.stagingTagPoint.Clear();
            //BgColors(Btn_LineDistribution);

            //从这往下是线路发送逻辑
            //直线输出逻辑
            //List<Point> drawingPoints = new List<Point>();
            //List<Tuple<Point, int, int, int>> stagingTagPointRatate = MapInstrument.stagingTagPointRatate;
            //instrument.tagLocking = false;

            //List<Tuple<Point, int, int, int>> transformedPointsByRotate = painting.TransformCoordinatesByRotate(stagingTagPointRatate, 225, 455, 116);
            //List<PointStraightWithAngle> processedPoints = ProcessPointsByRotate(transformedPointsByRotate, 0.001);

            //string folderPath =  AppDomain.CurrentDomain.BaseDirectory;
            //string fileName = "TargetPoints.json";
            //string fileNameByAngle = "TargetPointsByAngle.txt";

            //string filePath = System.IO.Path.Combine(folderPath, fileName);
            //string filePathByAngle = System.IO.Path.Combine(folderPath, fileNameByAngle);

            //StationData stationData = new StationData();

            //stationData.Stations.AddRange(processedPoints);

            //string jsonString = JsonSerializer.Serialize(stationData, new JsonSerializerOptions { WriteIndented = true });
            //File.WriteAllText(filePath, jsonString);

            //MapInstrument.stagingTagId.Clear();
            //MapInstrument.stagingTagPoint.Clear();
            //MapInstrument.stagingTagPointRatate.Clear();

            try
            {
                var client = MqttConnectionManager.LatestClient;
                if (client != null && client.IsConnected)
                {
                    //定位发送数据（需要时解开注释）
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("AGV/Response/Localization")
                        .WithPayload("Orientation")
                        .WithExactlyOnceQoS()
                        .WithRetainFlag(false)
                        .Build();

                    await client.PublishAsync(message);
                    // 操作成功，显示提示框
                    MessageBox.Show("定位脚本已下发。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("MQTT客户端未连接，请先建立连接。");
                }
                //BgColors(Btn_LineDistribution);
                
            }
            catch (Exception ex)
            {
                // 捕获异常，显示错误消息
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        /// <summary>
        /// 半圆
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_cren_Click(object sender, RoutedEventArgs e)
        {
            instrument.Semicircles();
            BgColors(Btn_cren);
        }   
        /// <summary>
        /// 曲线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_bezierCurve_Click(object sender, RoutedEventArgs e)
        {
            instrument.BezierCurve();
            BgColors(Btn_bezierCurve);
        }

        /// <summary>
        /// 清除线路
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteCircuit_Click(object sender, RoutedEventArgs e)
        {
            instrument.ClearTen();
            BgColors(DeleteCircuit);
        }


        public static (double X, double Y)? GetTagCoordinates(DataTable tagStation, string resTag)
        {
            if (tagStation == null || string.IsNullOrEmpty(resTag))
            {
                return null; // 处理无效输入
            }

            string resTagNew = resTag.Substring(2);

            // 使用 LINQ 查询匹配的 TagName
            var result = tagStation.AsEnumerable()
                .Where(row => row.Field<string>("TagName") == resTagNew)
                .Select(row => (X: row.Field<double>("X"), Y: row.Field<double>("Y")))
                .FirstOrDefault();

            return result != default ? result : default((double, double)?);
        }


        /// <summary>
        /// 重定位测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private Timer timer;

        private async void LoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RepositioningTag repositioningTag = new RepositioningTag();
                bool? result = repositioningTag.ShowDialog();

                if (tagInfoBLL == null)
                {
                    throw new Exception("tagInfoBLL 为空，请检查是否已正确初始化。");
                }

                DataTable TagStation = tagInfoBLL.RataTable(Time.ToString());



                //RepositioningPage repositioningPage = new RepositioningPage();
                //bool? result = repositioningPage.ShowDialog();
                //Point TestPoint = new Point(3, 1);
                if (result == true)
                {
                    var ResTag = repositioningTag.dataResStatu;
                    var ResTagAngel = repositioningTag.dataResAngel;
                    //var dataResX = repositioningPage.dataResStatuX;
                    //var dataResY = repositioningPage.dataResStatuY;
                    //var dataResZ = repositioningPage.dataResStatuZ;
                    var coordinates = GetTagCoordinates(TagStation, ResTag);


                    double x = Convert.ToDouble(coordinates.Value.X);
                    double y = Convert.ToDouble(coordinates.Value.Y);
                    double z = Convert.ToDouble(ResTagAngel);

                    //Point point = new Point(x+19, y+11.5);
                    Point point = new Point(10 * x, 10 * y);
                    Point pointByNew = painting.TransformCoordinate(point, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);

                    var client = MqttConnectionManager.LatestClient;
                    if (client != null && client.IsConnected)
                    {
                        var carData = new CarData
                        {
                            X = pointByNew.X,
                            Y = pointByNew.Y,
                            Z = z
                        };

                        var jsonData = JsonSerializer.Serialize(carData);

                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic("AGV/Response/Repositioning")
                            .WithPayload(jsonData)
                            .WithExactlyOnceQoS()
                            .WithRetainFlag(false)
                            .Build();

                        await client.PublishAsync(message);

                        // 延迟 3 秒后弹出成功提示框
                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(3); // 设置3秒延迟
                        timer.Tick += (s, args) =>
                        {
                            timer.Stop(); // 停止定时器
                            MessageBox.Show("重定位成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        };
                        timer.Start();
                    }
                    else
                    {
                        MessageBox.Show("MQTT客户端未连接，请先建立连接。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                // 异常提示
                MessageBox.Show($"发布失败，发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Timer_Tick1(object sender, EventArgs e)
        {
            // 获取当前系统时间
            var currentTime = DateTime.Now.ToString();

            // 发布当前系统时间到 MQTT 主题
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("AGV/Response/MapDrawingTime")
                .WithPayload(currentTime)
                .WithExactlyOnceQoS()
                .Build();

            mqttClient.PublishAsync(message);
        }


        private void Time_CheckMqttMessage(EventArgs e)
        {
            List<Point> coordinates = painting.GetCoordinatesFromMqttMessage();
            if (coordinates != null)
            {
                //// 取消定时器
                //timer.Dispose();
                //timer = null;

                // 在UI线程上更新UI
                Dispatcher.Invoke(() =>
                {
                    painting.DrawDots(mainPanel, coordinates, 1);
                });
            }
        }













        //private async void SetupMqttClient()
        //{
        //    var factory = new MqttFactory();
        //    _mqttClient = factory.CreateMqttClient();

        //    _mqttClient.UseConnectedHandler(async e =>
        //    {
        //        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("AGV/Carrier/Common").Build());
        //    });

        //    _mqttClient.UseApplicationMessageReceivedHandler(e =>
        //    {
        //        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
        //        var _data = JsonSerializer.Deserialize<CarData>(payload);

        //        //缩放
        //        _data.X *= 116;
        //        _data.Y *= -116;
        //        _data.Z = -_data.Z;

        //        UpdateCarPosition(_data);
        //    });
        //}

        //private void UpdateCarPosition(CarData data)
        //{
        //    Dispatcher.Invoke(() =>
        //    {
        //        if (_car == null)
        //        {
        //            CreateCar(new Point(244, 466.5), 144, 96); // Example origin at (240, 460), width 50, height 30
        //        }
                    
        //        _car.UpdatePosition(data.X, data.Y, data.Z);
        //    });
        //}

        //private void CreateCar(Point origin, double width, double height)
        //{
        //    _car = new Car(origin, width, height);
        //    mainPanel.Children.Add(_car.Shape);
        //}

        private Timer heartbeatTimer;
        private string lastAddress;
        private int lastPort;

        /// <summary>
        /// 连接AGV
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Btn_Text_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mqtt = new mqtt();
                if (mqtt.ShowDialog() == true)
                {
                    // 保存首次连接的 IP 和端口
                    string address = mqtt.Address;
                    int port = mqtt.Port;
                    double length = mqtt.SelectedLength;
                    double width = mqtt.SelectedWidth;

                    // 判断是否已连接
                    if (MqttConnectionManager.MqttClients.ContainsKey(address)
                        && MqttConnectionManager.MqttClients[address].IsConnected)
                    {
                        MessageBox.Show($"AGV 已连接：{address}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        return;
                    }
                     
                    var wrapper = new MqttClientWrapper(this.Dispatcher, address, length, width);
                    await wrapper.InitializeAsync(address, port, mainPanel);

                    // 添加连接并设为当前连接
                    MqttConnectionManager.AddClient(address, wrapper);
                    MqttConnectionManager.SetCurrent(address);

                    GlobalData.UpdateAgvInfo("AGV", address);
                    GlobalDisplayData.UpdateDisplayInfo(address, "AGV", address);
                    _isAutoReconnectEnabled = true;

                     // 初始化并启动心跳检测
                    //StartHeartbeatCheck();
                }
            }
            catch (Exception ex)
            {
                // 异常提示
                MessageBox.Show($"连接失败，发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async void ConnectToMqttBroker(string address, int port)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(address, port)
                .WithCleanSession() 
                .Build();

            await _mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        private void StartHeartbeatCheck()
        {
            heartbeatTimer = new Timer(2000); // 每隔2秒检测一次
            heartbeatTimer.Elapsed += async (sender, e) => await CheckConnectionStatus();
            heartbeatTimer.AutoReset = true;
            heartbeatTimer.Start();
        }

        private async Task CheckConnectionStatus()
        {
            var client = MqttConnectionManager.LatestClient;
            if (client != null && client.IsConnected)
            {
                if (_isAutoReconnectEnabled)
                {
                    // 连接断开，尝试重连
                    try
                    {
                        await client.InitializeAsync(lastAddress, lastPort, mainPanel);
                    }
                    catch (Exception ex)
                    {
                        // 提示重连失败
                        //MessageBox.Show($"重连失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }



        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<List<TaskModel>> FetchTasksFromApiAsync()
        {
            try
            {
                string apiUrl = "http://192.168.137.99:8002/api/agvschedule"; // API 地址
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    List<TaskModel> tasks = JsonSerializer.Deserialize<List<TaskModel>>(jsonString);
                    return tasks.OrderBy(t => t.TaskId).ToList(); // 按 TaskId 排序
                }
                else
                {
                    MessageBox.Show("获取任务失败: " + response.StatusCode);
                    return new List<TaskModel>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("API 请求异常: " + ex.Message);
                return new List<TaskModel>();
            }
        }

        private static ConcurrentQueue<TaskModel> _taskQueue = new ConcurrentQueue<TaskModel>();

        private async Task UpdateTaskQueue()
        {
            var newTasks = await FetchTasksFromApiAsync();
            foreach (var task in newTasks)
            {
                _taskQueue.Enqueue(task);
            }
        }

        private async Task ProcessTask(TaskModel task)
        {
            try
            {
                string dataMes = task.Origin;
                string dataEndMes = task.Destination;

                DataTable routeStation = routeInfoBLL.RoutelistArrer(Time.ToString());
                DataTable TagStation = tagInfoBLL.RataTable(Time.ToString());

                //搜寻当前点位置
                var data = JsonSerializer.Deserialize<CarData>(MqttClientWrapper.payloadAll);
                Point newPoint1 = new Point(data.X, data.Y);

                //Point newPoint1 = new Point(0, 0);
                Point newPoint2 = painting.ReverseTransformCoordinate(newPoint1, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);
                double newX = ((newPoint2.X) / 10);
                double newY = ((newPoint2.Y) / 10);
                Point newPoint3 = new Point(newX, newY);//确定当前点位置

                int mesDataEnd = pointHandle.FindRowByTagsNameNew(routeStation, dataMes, dataEndMes, newPoint3, TagStation);

                //如果返回 -2，直接跳出，视为任务已完成
                if (mesDataEnd == -2)
                {
                    GlobalStatus.Status = "已完成";
                    return; // 跳出整个任务处理
                }


                List<Tuple<Point, int, double, double, double, double>> mesEndPoints =
                    pointHandle.FindMesEnd(routeStation, Time.ToString(), mesDataEnd);

                List<newPointStraightWithAngle> processedPoints = ProcessPointsByTurnRotate(mesEndPoints, 0.001);
                newStationData stationData = new newStationData();
                stationData.Stations.AddRange(processedPoints);

                string jsonString = JsonSerializer.Serialize(stationData, new JsonSerializerOptions { WriteIndented = true });

                bool taskCompleted = false;
                DateTime startTime = DateTime.Now;

                // 只在订阅前判断连接
                var client = MqttConnectionManager.LatestClient;
                {
                    if (client != null && client.IsConnected)
                    {
                        // 订阅完成话题
                        await client.SubscribeAsync("AGV/Response/TaskDone");

                        // 注册接收消息事件（仅注册一次的话可放在连接成功后注册）
                        client.Client.UseApplicationMessageReceivedHandler(e =>
                        {
                            string topic = e.ApplicationMessage.Topic;
                            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                            if (topic == "AGV/Response/TaskDone" && payload == "已完成")
                            {
                                taskCompleted = true;
                            }
                        });
                    }
                    else
                    {
                        Console.WriteLine("MQTT 未连接，无法订阅任务完成消息！");
                        return;
                    }

                    while (!taskCompleted)
                    {
                        if (client.IsConnected)
                        {
                            var mapLineMessage = new MqttApplicationMessageBuilder()
                                .WithTopic("AGV/Carrier/MapLineMes")
                                .WithPayload(jsonString)
                                .WithExactlyOnceQoS()
                                .WithRetainFlag(false)
                                .Build();

                            var distributionMessage = new MqttApplicationMessageBuilder()
                                .WithTopic("AGV/Response/TaskDistribution")
                                .WithPayload("TaskDistribution")
                                .WithExactlyOnceQoS()
                                .WithRetainFlag(false)
                                .Build();

                            await client.PublishAsync(mapLineMessage);
                            await client.PublishAsync(distributionMessage);
                        }

                        if ((DateTime.Now - startTime).TotalSeconds >= 30)
                        {
                            Console.WriteLine("任务下发超时（30 秒），停止发送");
                            break;
                        }

                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"任务处理过程中出现异常：{ex.Message}");
                // 可加日志记录 ex.StackTrace
            }

        }

        public async Task ProcessTasksSequentially()
        {
            try
            {
                while (true)
                {
                    // Step 1: 查询数据库获取任务，按顺序获取任务，限制返回一条未完成的任务
                    DataTable tasksDataTable = AGV.DAL.MySqlHelper.ExecuteDataTableEnableNull("SELECT taskId, origin, destination, startTime FROM tasks ORDER BY startTime ASC");

                    if (tasksDataTable.Rows.Count > 0)
                    {
                        // Step 2: 提取当前任务的信息
                        DataRow taskRow = tasksDataTable.Rows[0];
                        TaskModel task = new TaskModel
                        {
                            TaskId = Convert.ToInt64(taskRow["TaskId"]),
                            Origin = taskRow["Origin"].ToString(),
                            Destination = taskRow["Destination"].ToString(),
                        };

                        // Step 3: 执行任务
                        await ProcessTask(task);

                        // Step 4: 等待任务执行完成
                        bool isCompleted = await CheckTaskCompletionStatus(task.TaskId);

                        if (isCompleted)
                        {
                            // Step 5
                            DeleteTaskFromDatabase(task.TaskId);

                            // Step 6: 反馈
                            // 如果目标不是 TA8，再发送反馈
                            if (task.Destination != "TA8")
                            {
                                await SendTaskCompletionReport(task.TaskId, "已完成");
                            }
                        }
                    }

                    // 每 5 秒检查一次
                    await Task.Delay(5000);
                }
            }
            catch (Exception ex)
            {
                // 如果发生异常，弹出错误提示框
                MessageBox.Show($"MES连接错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 辅助

        private async Task<bool> CheckTaskCompletionStatus(long taskId)
        {
            // 模拟任务检查，等待任务状态变为 "已完成"
            // 这里你可以根据实际情况定制检查逻辑，比如轮询 GlobalStatus.Status 等

            while (GlobalStatus.Status != "已完成")
            {
                await Task.Delay(2000);  // 每隔 1 秒钟检查一次
            }

            return true;  // 当 Status 变为 "已完成" 时，返回 true
        }



        private void DeleteTaskFromDatabase(long taskId)
        {
            // 删除任务记录
            AGV.DAL.MySqlHelper.ExecuteDataTableDelete($"DELETE FROM tasks WHERE TaskId = {taskId}");
        }

        private async Task SendTaskCompletionReport(long taskId, string status)
        {
            // 向上位系统发送任务完成的状态
            var requestBody = new
            {
                orderId = taskId.ToString(),
                status = status
            };

            string jsonString = JsonSerializer.Serialize(requestBody);

            //请求
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync("http://192.168.1.100:8084/integration/agv/agvStatus", content);

                    bool isSuccess = response.IsSuccessStatusCode;
                    string errorMessage = isSuccess ? "" : await response.Content.ReadAsStringAsync();

                    // 异步插入日志，不阻塞主线程
                    _ = Task.Run(() => AGV.DAL.MySqlHelper.InsertTaskCompletionLog(taskId, status, isSuccess, errorMessage));
                }
                catch (Exception ex)
                {
                    // 异步插入日志
                    _ = Task.Run(() => AGV.DAL.MySqlHelper.InsertTaskCompletionLog(taskId, status, false, ex.Message));
                }
            }
        }








        //MES测试
        private async void OnInputLineButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                
                LineInputDialog inputDialog = new LineInputDialog();
                inputDialog.ShowDialog();

                var dataMes = inputDialog.dataMesStartStatu;
                var dataEndMes = inputDialog.dataMesEndStatu;
                            
                var data = JsonSerializer.Deserialize<CarData>(MqttClientWrapper.payloadAll);
                Point newPoint1 = new Point(data.X, data.Y);

                //Point newPoint1 = new Point(0, 0);
                Point newPoint2 = painting.ReverseTransformCoordinate(newPoint1, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);
                double newX = ((newPoint2.X) / 10); 
                double newY = ((newPoint2.Y) / 10);
                Point newPoint3 = new Point(newX, newY);

                // 数据库读取
                //DataTable lineStation = infoBLL.LinelistArrer(Time.ToString());
                DataTable lineStation = infoBLL.LinelistArrer(Time.ToString());
                DataTable routeStation = routeInfoBLL.RoutelistArrer(Time.ToString());


                DataTable dataTable = new DataTable();
                dataTable = Painting.ExpandBidirectionalRoutes(lineStation);

                DataTable newLine = Painting.FindRouteNew(dataTable, newPoint3, dataMes);

                newLine = Painting.ProcessDataTable(newLine);

                // 转化表结构 
                DataTable newLine1 = TransformDataTable(newLine);

                //拿取后半段mes路线
                int mesDataEnd = pointHandle.FindRowByTags(routeStation, dataMes, dataEndMes);

                List<Tuple<Point, int, double, double, double, double>> mesEndPoints = pointHandle.FindMesEnd(routeStation, Time.ToString(), mesDataEnd);

                // 转化准备输出 JSON
                List<Point> points = ExtractPoints(newLine1);

                List<Tuple<Point, int, double, double>> stagingTagPointRatate1 = new List<Tuple<Point, int, double, double>>();

                // 把 points 放入 Tuple
                int rotateValue1 = 0;
                double rotateValue2 = 0.7;//线速度
                double rotateValue3 = 0.3;

                foreach (Point point in points)
                {
                    stagingTagPointRatate1.Add(new Tuple<Point, int, double, double>(point, rotateValue1, rotateValue2, rotateValue3));
                }

                List<Tuple<Point, int, double, double>> transformedPointsByRotate = painting.TransformCoordinatesByRotate(stagingTagPointRatate1, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);

                transformedPointsByRotate = painting.RemoveRedundantRoutes(transformedPointsByRotate);

                List<Tuple<Point, int, double, double, double, double>> mesCombinationPoint = pointHandle.CombineLists(transformedPointsByRotate, mesEndPoints);
                //mesCombinationPoint = RemoveRepetitivePoints(mesCombinationPoint);
                //List<PointStraightWithAngle> processedPoints = ProcessPointsByRotate(transformedPointsByRotate, 0.001);

                List<newPointStraightWithAngle> processedPoints = ProcessPointsByTurnRotate(mesCombinationPoint, 0.001);

                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string fileName = "MesPoints.json";
                string filePath = System.IO.Path.Combine(folderPath, fileName);

                newStationData stationData = new newStationData();
                stationData.Stations.AddRange(processedPoints);

                // 输出点位
                string jsonString = JsonSerializer.Serialize(stationData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, jsonString);

                var client = MqttConnectionManager.LatestClient;
                if (client != null && client.IsConnected)
                {

                    //发送信息
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("AGV/Carrier/MapLineMes")
                        .WithPayload(jsonString)
                        .WithExactlyOnceQoS()
                        .WithRetainFlag(false)
                        .Build();

                    await client.PublishAsync(message);

                }
                else
                {
                    MessageBox.Show("MQTT客户端未连接，请先建立连接。");
                }


                // 操作成功，显示提示框
                MessageBox.Show("点位数据已成功保存到文件。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // 捕获异常，显示错误消息
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public  List<Tuple<Point, int, double, double, double, double>> RemoveRepetitivePoints(List<Tuple<Point, int, double, double, double, double>> mesCombinationPoint)
        {
            List<Tuple<Point, int, double, double, double, double>> result = new List<Tuple<Point, int, double, double, double, double>>();

            for (int i = 0; i < mesCombinationPoint.Count; i++)
            {
                // 检查是否为第一行和第三行
                if (i + 2 < mesCombinationPoint.Count && mesCombinationPoint[i].Item1.Equals(mesCombinationPoint[i + 2].Item1))
                {
                    // 如果第一个和第三个 Point 相同，跳过第一行和第二行
                    i++;  // 跳过第二行
                    continue;
                }

                // 保留当前行
                result.Add(mesCombinationPoint[i]);
            }

            return result;
        }



        /// <summary>
        /// 曲线点位计算函数
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public List<Point> GetPointsOnCurve(Point startPoint, Point endPoint)
        {
            double controlDistance = Math.Abs(endPoint.X - startPoint.X) / 2;
                
            // 计算控制点的坐标
            Point controlPoint = new Point(startPoint.X + controlDistance, startPoint.Y);

            List<Point> points = new List<Point>();

            // 计算步数，根据起始点和终止点的横坐标差值确定
            int steps = (int)Math.Ceiling(Math.Abs(endPoint.X - startPoint.X) / 0.0428571);

            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                    
                // 参数方程计算
                double x = (1 - t) * (1 - t) * startPoint.X + 2 * (1 - t) * t * controlPoint.X + t * t * endPoint.X;
                double y = (1 - t) * (1 - t) * startPoint.Y + 2 * (1 - t) * t * startPoint.Y + t * t * endPoint.Y;
                        
                points.Add(new Point(x, y));
            }

            return points;
        }   

        public List<Point> GetSemiCirclePoints(Point startPoint, Point endPoint, double radius)
        {
            List<Point> points = new List<Point>();

            // 计算起始点和终止点之间的距离
            double distance = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));

            // 计算弧线的半径
            double arcRadius = distance / 2 + Math.Pow(radius, 2) / (2 * distance);

            // 计算弧线的圆心坐标
            double centerX = (startPoint.X + endPoint.X) / 2 + (startPoint.Y - endPoint.Y) * Math.Sqrt(Math.Pow(radius, 2) / (4 * Math.Pow(distance, 2) - Math.Pow(startPoint.Y - endPoint.Y, 2))) * (startPoint.Y < endPoint.Y ? 1 : -1);
            double centerY = (startPoint.Y + endPoint.Y) / 2 + (startPoint.X - endPoint.X) * Math.Sqrt(Math.Pow(radius, 2) / (4 * Math.Pow(distance, 2) - Math.Pow(startPoint.Y - endPoint.Y, 2))) * (startPoint.X > endPoint.X ? 1 : -1);

            // 计算起始角度和终止角度
            double startAngle = Math.Atan2(startPoint.Y - centerY, startPoint.X - centerX);
            double endAngle = Math.Atan2(endPoint.Y - centerY, endPoint.X - centerX);

            // 计算半圆上的所有点位
            double angleDiff = endAngle - startAngle;
            double angleStep = Math.PI / 180;
            int steps = (int)Math.Ceiling(angleDiff / angleStep);
            for (int i = 0; i <= steps; i++)
            {
                double angle = startAngle + i * angleDiff / steps;
                double x = centerX + arcRadius * Math.Cos(angle);
                double y = centerY + arcRadius * Math.Sin(angle);
                points.Add(new Point(x, y));
            }

            // 返回半圆上的所有点位
            return points;
        }
        //private List<Point> CalculateLinePoints(double startX, double startY, double endX, double endY)
        //{
        //    List<Point> points = new List<Point>();
        //    double step = 0.1;
        //    while (startX <= endX)
        //    {
        //        double y = startY + (endY - startY) * (startX - startX) / (endX - startX);

        //        // 使用 Math.Round 来保留两位小数
        //        y = Math.Round(y, 2);
        //        points.Add(new Point(startX, y));
        //        startX += step;
        //    }
        //    return points;
        //}

        private List<PointDataMapLine> CalculateLinePoints(double startX, double startY, double endX, double endY)
        {
            List<PointDataMapLine> points = new List<PointDataMapLine>();
            double step = 0.1;

            while (startX <= endX)
            {
                double y = startY + (endY - startY) * (startX - startX) / (endX - startX);
                y = Math.Round(y, 2);
                PointDataMapLine point = new PointDataMapLine(startX, y, 0); // 默认角度为0
                points.Add(point);
                startX += step;
                
            }

            return points;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            const int arrowSize = 30;


            
            System.Windows.Shapes.Path upArrow = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M5,10 L15,10 L10,5 Z"),
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                Width = arrowSize,
                Height = arrowSize
            };

            System.Windows.Shapes.Path downArrow = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M5,20 L15,20 L10,25 Z"),
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                Width = arrowSize,
                Height = arrowSize
            };

            System.Windows.Shapes.Path leftArrow = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M10,5 L10,15 L5,10 Z"),
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                Width = arrowSize,
                Height = arrowSize
            };

            System.Windows.Shapes.Path rightArrow = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M20,5 L20,15 L25,10 Z"),
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                Width = arrowSize,
                Height = arrowSize
            };

           
            mainPanel.Children.Add(upArrow);
            mainPanel.Children.Add(downArrow);
            mainPanel.Children.Add(leftArrow);
            mainPanel.Children.Add(rightArrow);

            
            mainPanel.KeyDown += (sender1, e1) =>
            {
                switch (e1.Key)
                {
                    case Key.Up:
                        upArrow.Visibility = Visibility.Visible;
                        e.Handled = true;
                        break;
                    case Key.Down:
                        downArrow.Visibility = Visibility.Visible;
                        e.Handled = true;
                        break;
                    case Key.Left:
                        leftArrow.Visibility = Visibility.Visible;
                        e.Handled = true;
                        break;
                    case Key.Right:
                        rightArrow.Visibility = Visibility.Visible;
                        e.Handled = true;
                        break;
                }
            };

            
            mainPanel.KeyUp += (sender1, e1) =>
            {
                switch (e1.Key)
                {
                    case Key.Up:
                        upArrow.Visibility = Visibility.Collapsed;
                        e.Handled = true;
                        break;
                    case Key.Down:
                        downArrow.Visibility = Visibility.Collapsed;
                        e.Handled = true;
                        break;
                    case Key.Left:
                        leftArrow.Visibility = Visibility.Collapsed;
                        e.Handled = true;
                        break;
                    case Key.Right:
                        rightArrow.Visibility = Visibility.Collapsed;
                        e.Handled = true;
                        break;
                }
            };

            
            double margin = 5; // 右下角边距
            Canvas.SetRight(upArrow, margin);
            Canvas.SetBottom(upArrow, margin);
            Canvas.SetRight(downArrow, margin);
            Canvas.SetBottom(downArrow, margin);
            Canvas.SetRight(leftArrow, margin + arrowSize);
            Canvas.SetBottom(leftArrow, margin + arrowSize);
            Canvas.SetRight(rightArrow, margin - arrowSize);
            Canvas.SetBottom(rightArrow, margin + arrowSize);


        }

            
        public class GridCell
        {
            public Brush FillColor { get; set; }

            public GridCell(Brush color)
            {
                FillColor = color;
            }
        }

        public class GridViewModel
        {
            public ObservableCollection<ObservableCollection<GridCell>> Rows { get; set; }

            public GridViewModel(int rowCount, int columnCount)
            {
                Rows = new ObservableCollection<ObservableCollection<GridCell>>();

                for (int i = 0; i < rowCount; i++)
                {
                    var row = new ObservableCollection<GridCell>();
                    for (int j = 0; j < columnCount; j++)
                    {
                        if (i == rowCount - 1)
                            row.Add(new GridCell(Brushes.Black)); // Last row black
                        else if (i == rowCount - 2)
                            row.Add(new GridCell(Brushes.White)); // Penultimate row white
                        else
                            row.Add(new GridCell((i + j) % 2 == 0 ? Brushes.White : Brushes.Black));
                    }
                    Rows.Add(row);
                }
            }
        }

        public class PointStraightWithAngle
        {
            public int ID { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double angle { get; set; }
            public double v_desired { get; set; }

            public double a_desired { get; set; }
            public int lift { get; set; }
            public bool rotate { get; set; }


            public PointStraightWithAngle(int id, double x, double y, double angleToNext, double v, double a, int liftStation, bool rotateNow)
            {
                ID = id;
                X = x;
                Y = y;
                lift = liftStation;
                angle = angleToNext;
                v_desired = v; 
                a_desired = a;
                rotate = rotateNow;
            }
        }

        public class newPointStraightWithAngle
        {
            public int ID { get; set; }
            public double X { get; set; }/// <summary>
                                         /// 根据连线 DataTable 生成站点点表（包含邻接点）
                                         /// </summary>
            public static DataTable GeneratePointTable(DataTable routeTable)
            {
                // 创建结果 DataTable
                DataTable pointTable = new DataTable("Points");
                pointTable.Columns.Add("TagName", typeof(string));
                pointTable.Columns.Add("X", typeof(double));
                pointTable.Columns.Add("Y", typeof(double));
                pointTable.Columns.Add("neighborIds", typeof(string)); // 用逗号分隔多个邻居

                // 用字典保存每个站点的信息
                var pointDict = new Dictionary<string, (double X, double Y, HashSet<string> Neighbors)>();

                foreach (DataRow row in routeTable.Rows)
                {
                    string tag1 = row["Tag1"].ToString();
                    string tag2 = row["Tag2"].ToString();

                    double startX = Convert.ToDouble(row["StartX"]);
                    double startY = Convert.ToDouble(row["StartY"]);
                    double endX = Convert.ToDouble(row["EndX"]);
                    double endY = Convert.ToDouble(row["EndY"]);

                    // 添加起点
                    if (!pointDict.ContainsKey(tag1))
                        pointDict[tag1] = (startX, startY, new HashSet<string>());
                    pointDict[tag1].Neighbors.Add(tag2);

                    // 添加终点
                    if (!pointDict.ContainsKey(tag2))
                        pointDict[tag2] = (endX, endY, new HashSet<string>());
                    pointDict[tag2].Neighbors.Add(tag1);
                }

                // 按 Tag 排序（从小到大）
                foreach (var kv in pointDict.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    string tag = kv.Key;
                    double x = kv.Value.X;
                    double y = kv.Value.Y;
                    string neighborList = string.Join(",", kv.Value.Neighbors.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

                    pointTable.Rows.Add(tag, x, y, neighborList);
                }

                return pointTable;
            }
            public double Y { get; set; }
            public double angle { get; set; }
            public double v_desired { get; set; }

            public double a_desired { get; set; }
            public int lift { get; set; }
            public bool rotate { get; set; }
            public double obs_avoidance { get; set; }


            public newPointStraightWithAngle(int id, double x, double y, double angleToNext, double v, double a, int liftStation, bool rotateNow, double obsAvoidance)
            {
                ID = id;
                X = x;
                Y = y;
                lift = liftStation;
                angle = angleToNext;
                v_desired = v;
                a_desired = a;
                rotate = rotateNow;
                obs_avoidance = obsAvoidance;
            }
        }


        public class StationData
        {
            [JsonPropertyName("station")]
            public List<PointStraightWithAngle> Stations { get; set; }

            public StationData()
            {
                Stations = new List<PointStraightWithAngle>();
            }
        }

        public class newStationData
        {
            [JsonPropertyName("station")]
            public List<newPointStraightWithAngle> Stations { get; set; }

            public newStationData()
            {
                Stations = new List<newPointStraightWithAngle>();
            }
        }

        public class CarData
        {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        }

        public class TaskModel
        {
            public long TaskId { get; set; }
            public string Origin { get; set; }
            public string Destination { get; set; }
        }


        public class Car
        {
            public Rectangle Shape { get; private set; }
            private Point _center;
            private double _width;
            private double _height;
            private string _address; // 每辆车对应的AGV地址

            public Car(string address, Point center, double width, double height)
            {
                _address = address;
                _center = center;
                _width = width;
                _height = height;
                CreateShape();
            }

            private void CreateShape()
            {
                Shape = new Rectangle
                {
                    Width = _width,
                    Height = _height,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };


                // Define two linear gradient brushes
                var frontBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0.5),
                    EndPoint = new Point(1, 0.5)
                };
                frontBrush.GradientStops.Add(new GradientStop(Colors.Red, 0));
                frontBrush.GradientStops.Add(new GradientStop(Colors.Red, 0.7));
                frontBrush.GradientStops.Add(new GradientStop(Colors.Blue, 0.7));
                frontBrush.GradientStops.Add(new GradientStop(Colors.Blue, 1));

                Shape.Fill = frontBrush;

                // Position the car shape based on center point
                Canvas.SetLeft(Shape, _center.X - (_width / 2));
                Canvas.SetTop(Shape, _center.Y - (_height / 2));
            }

            public void UpdatePosition(double x, double y, double angle)
            {
                // Update position of the main rectangle
                Canvas.SetLeft(Shape, _center.X + x - (_width / 2));
                Canvas.SetTop(Shape, _center.Y + y - (_height / 2));

                double statusX = _center.X + x ;
                double statusY = _center.Y + y ;
                GlobalData.UpdateAgvInfo("车辆坐标X", statusX.ToString("F2"));
                GlobalData.UpdateAgvInfo("车辆坐标Y", statusY.ToString("F2"));

                // 更新显示，用自己的AGV地址
                GlobalDisplayData.UpdateDisplayInfo(_address, "车辆坐标X", statusX.ToString("F2"));
                GlobalDisplayData.UpdateDisplayInfo(_address, "车辆坐标Y", statusY.ToString("F2"));
                // Rotate the entire car including the front color gradient
                Shape.RenderTransform = new RotateTransform(angle, _width / 2, _height / 2);
            }
        }

    }
}
