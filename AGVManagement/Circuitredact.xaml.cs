using AGV.BLL;
using AGVManagement.instrument;
using AGVManagement.MapPaint;
using AGVManagement.Mqtt;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static AGVManagement.MainWindow;

namespace AGVManagement
{
    /// <summary>
    /// Circuitredact.xaml 的交互逻辑
    /// </summary>
    public partial class Circuitredact : Window
    {
        private MapManag manag = new MapManag();
        private TagCompile tag = new TagCompile();
        private OperateDBBLL operate = new OperateDBBLL();
        private double mpWidth, mpHeight;
        private long Times;
        private DataTable dtRoute = new DataTable();
        private MapMessageBLL messageBLL = new MapMessageBLL();
        private int edid = 0;


        private List<MqttClientWrapper> _mqttClients = new List<MqttClientWrapper>();
        public Circuitredact()
        {
            InitializeComponent();
            MapLoad();

            // 创建 Converter 实例
            var converter = new DefaultToChineseConverter();

            // 找到角速度列
            var changeProgramColumn = EditlineData.Columns
                .OfType<DataGridTextColumn>()
                .FirstOrDefault(c => c.Header?.ToString() == "角速度");

            //var changePortColumn = 
            if (changeProgramColumn != null)
            {
                // 用 Converter 替换 Binding
                changeProgramColumn.Binding = new Binding("ChangeProgram")
                {
                    Converter = converter
                };
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                e.Handled = true;

                // 当前缩放值
                double currentScale = MapScaleTransform.ScaleX;

                // 缩放因子
                double zoomFactor = e.Delta > 0 ? 1.05 : 0.95;
                double newScale = currentScale * zoomFactor;

                // 限制缩放范围
                if (newScale < 0.2 || newScale > 5)
                    return;

                // 获取 Canvas 中心作为缩放中心
                double centerX = MapIN.ActualWidth / 2;
                double centerY = MapIN.ActualHeight / 2;

                MapScaleTransform.CenterX = centerX;
                MapScaleTransform.CenterY = centerY;

                // 应用缩放
                MapScaleTransform.ScaleX = newScale;
                MapScaleTransform.ScaleY = newScale;
            }
        }


        /// <summary>
        /// 载入地图信息
        /// </summary>
        private void MapLoad()
        {
            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();
            Painting.siseWin = 1;
            SliMax.Value = 0;
            SubmitPro.IsEnabled = false;
            DelPro.IsEnabled = false;
            MapMessageBLL messageBLL = new MapMessageBLL();
            DataTable da = messageBLL.GetMapData(null);
            if (da == null)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = "请选择";
                maplist.Items.Add(item);
                SubmitPro.IsEnabled = false;
                DelPro.IsEnabled = false;
            }
            else
            {
                foreach (DataRow data in da.Rows)
                {
                    ComboBoxItem ite = new ComboBoxItem();
                    ite.Content = data["Name"].ToString();
                    ite.Tag = data["Width"].ToString() + "," + data["Height"].ToString() + "," + data["CreateTime"].ToString();
                    maplist.Items.Add(ite);
                }
            }
            maplist.SelectedIndex = 0;
        }

        /// <summary>
        /// 地图选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Maplist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBoxItem)maplist.SelectedItem).Tag == null)
            {
                return;
            }
            SliMax.Value = 0;
            SubmitPro.IsEnabled = true;
            DelPro.IsEnabled = true;
            EditlineData.ItemsSource = new DataTable().DefaultView;
            lineRo.Items.Clear();
            MapMessageBLL messageBLL = new MapMessageBLL();
            string ls = ((ComboBoxItem)maplist.SelectedItem).Tag.ToString();
            string[] arr = ls.Split(',');
            dtRoute = messageBLL.BLLMapRoute(arr[2]);
            if (dtRoute.Rows.Count == 0)    
            {
                ComboBoxItem item = new ComboBoxItem { Content = "请选择" };
                lineRo.Items.Add(item);
                SubmitPro.IsEnabled = false;
                DelPro.IsEnabled = false;
                //decimal.Add
            }
            else
            {
                ComboBoxItem item = new ComboBoxItem { Content = "请选择" };
                lineRo.Items.Add(item);
                foreach (DataRow data in dtRoute.Rows)
                {
                    ComboBoxItem ite = new ComboBoxItem();
                    ite.Content = data["Name"].ToString();
                    ite.Tag = data["Program"].ToString();
                    lineRo.Items.Add(ite);

                }
            }
            lineRo.SelectedIndex = 0;

            if (!maplist.Text.Equals("请选择") && ((ComboBoxItem)maplist.SelectedItem).Tag.ToString().Split(',').Count().Equals(3))
            {
                //线路
                MapInstrument.keyValuePairs.Clear();
                MapInstrument.valuePairs.Clear();
                MapInstrument.wirePointArrays.Clear();
                MapInstrument.GetKeyValues.Clear();
                Painting.siseWin = 1;
                int six = Convert.ToInt32(SliMax.Value.ToString("G3"));
                Painting.siseWin = six.Equals(0) ? 1 : six;
                MapIN.Children.Clear();
                string lss = ((ComboBoxItem)maplist.SelectedItem).Tag.ToString();
                string[] aarr = lss.Split(',');
                mpWidth = Convert.ToDouble(aarr[0]) * manag.Sise;
                mpHeight = Convert.ToDouble(aarr[1]) * manag.Sise;
                //manag

                
                MapIN.Width = mpWidth;
                MapIN.Height = mpHeight;
                Times = long.Parse(aarr[2]);
                manag.Times = Times;
                manag.GetData = EditlineData;
                manag.SelectMap(long.Parse(aarr[2]), MapIN,true);
            }
        }

        /// <summary>
        /// 地图缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliMax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Painting painting = new Painting();
            painting.mainPan = MapIN;
            int sis = Convert.ToInt32(e.NewValue);
            MapIN.Children.Clear();
            if (sis.Equals(0))
            {
                MapIN.Width = mpWidth * 1;
                MapIN.Height = mpHeight * 1;
                painting.Zoom(1);
                Painting.siseWin = 1;
            }
            else
            {
                MapIN.Width = mpWidth * sis;
                MapIN.Height = mpHeight * sis;
                painting.Zoom(sis);
                Painting.siseWin = sis;
            }
        }

        /// <summary>
        /// line
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Line_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                LineRest();
                if (lineRo.SelectedItem != null && !(lineRo.SelectedItem as ComboBoxItem).Content.ToString().Equals("请选择") && lineRo.Items.Count - 1 > 0)
                {
                    SubmitPro.IsEnabled = true;

                    DelPro.IsEnabled = true;
                    ProgramNO.IsEnabled = false;
                    ProgramNO.Text = ((ComboBoxItem)lineRo.SelectedItem).Tag.ToString();
                    edid = 1;
                    ProgramName.Text = ((ComboBoxItem)lineRo.SelectedItem).Content.ToString();
                    TagLine(lineRo.SelectedIndex - 1, (lineRo.SelectedItem as ComboBoxItem).Content.ToString());
                }
                else
                {
                    manag.tagType = false;
                    SubmitPro.IsEnabled = false;
                    DelPro.IsEnabled = false;
                    ProgramName.Text = "";
                    EditlineData.ItemsSource = new DataTable().DefaultView;
                    ProgramNO.Text = "0";
                }
            }
            catch
            { ProgramNO.Text = "0"; }
        }

        /// <summary>
        /// 线路还原
        /// </summary>
        public void LineRest()
        {
            
            MapInstrument map = new MapInstrument();
            map.TagFormer();//所有Tag还原为原色
            foreach (var item in MapInstrument.wirePointArrays)
            {
                if (item.GetPath != null)
                {
                    item.GetPath.Stroke = Brushes.Black;
                    item.GetPath.StrokeThickness = 1;
                }
                List<Path> paths = item.Paths;
                if (paths != null)
                {
                    foreach (Path it in paths)
                    {
                        it.Stroke = Brushes.Black;
                        it.StrokeThickness = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 显示线路信息
        /// </summary>
        /// <param name="index"></param>
        /// <param name="LeName"></param>
        public void TagLine(int index, string LeName)
        {
           
            string strTag = dtRoute.Rows[index]["Tag"].ToString();
            string[] Tagar = strTag.Split(',');

            string strSpeed = dtRoute.Rows[index]["Speed"].ToString();
            string[] Speedar = strSpeed.Split(',');

            string stPbs = dtRoute.Rows[index]["Pbs"].ToString();
            string[] Pbsar = stPbs.Split(',');

            string strTurn = dtRoute.Rows[index]["Turn"].ToString();
            string[] Turnar = strTurn.Split(',');

            string strDirection = dtRoute.Rows[index]["Direction"].ToString();
            string[] Directionar = strDirection.Split(',');

            string strHook = dtRoute.Rows[index]["Hook"].ToString();
            string[] Hookar = strHook.Split(',');

            string strStop = dtRoute.Rows[index]["Stop"].ToString();
            string[] Stopar = strStop.Split(',');

            string strChangeProgram = dtRoute.Rows[index]["ChangeProgram"].ToString();
            string[] ChangeProgramar = strChangeProgram.Split(',');

            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("Tag"));
            dt.Columns.Add(new DataColumn("Speed"));
            dt.Columns.Add(new DataColumn("Pbs"));
            dt.Columns.Add(new DataColumn("Turn"));
            dt.Columns.Add(new DataColumn("Direction"));
            dt.Columns.Add(new DataColumn("Hook"));
            dt.Columns.Add(new DataColumn("Stop"));
            dt.Columns.Add(new DataColumn("ChangeProgram"));
            for (int i = 0; i < Tagar.Length; i++)
            {
                dt.Rows.Add(new object[] { Tagar[i], TagCompile.agvSpeed[Convert.ToInt32(Speedar[i])], TagCompile.agvPbs[Convert.ToInt32(Pbsar[i])], TagCompile.agvTurn[Convert.ToInt32(Turnar[i])], TagCompile.agvDire[Convert.ToInt32(Directionar[i])], TagCompile.agvHook[Convert.ToInt32(Hookar[i])], Stopar[i], ChangeProgramar[i] });
            }
            if (Tagar.Count() > 0)
            {
                GetScroll.ScrollToHorizontalOffset(MapInstrument.valuePairs[Convert.ToInt32(Tagar[0])].Margin.Left - 600);//滚动条X轴跟随移动
                GetScroll.ScrollToVerticalOffset(MapInstrument.valuePairs[Convert.ToInt32(Tagar[0])].Margin.Top - 600); ///滚动条Y轴等随移动
            }
            EditlineData.ItemsSource = dt.DefaultView;
            EditlineData.AutoGenerateColumns = false;
            manag.table = dt;
            manag.tagType = true;
            manag.TagCic();
            manag.lineMap.TagClick(Times, Convert.ToInt32(dt.Rows[dt.Rows.Count - 1]["Tag"]), EditlineData, dt,false);
        }

        /// <summary>
        /// 表格点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditlineData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (EditlineData.SelectedItems.Count > 0)
                {
                    List<object> arr = ((DataRowView)EditlineData.SelectedValue).Row.ItemArray.ToList();
                    manag.LineMapShow(arr, (EditlineData.SelectedIndex == 0 ? true : false), (EditlineData.SelectedIndex == 0 ? Convert.ToInt32(((DataRowView)EditlineData.SelectedItem)["Tag"]) : Convert.ToInt32(((DataRowView)EditlineData.Items[EditlineData.SelectedIndex - 1])["Tag"])), EditlineData.SelectedIndex);

                    // 打开编辑框后清除选中行，避免再次点空白时重复触发
                    //EditlineData.SelectedIndex = -1;
                }
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelPro_Click(object sender, RoutedEventArgs e)
        {
            if (!edid.Equals(0))
            {
                MessageBoxResult confirmToDel = MessageBox.Show("确认要删除线路吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmToDel == MessageBoxResult.Yes)
                {
                    if (messageBLL.DelRouteMapWithName(Times, ProgramName.Text.Trim())/*messageBLL.DelRouteMap(Times, Convert.ToInt32(ProgramNO.Text.Trim()))*/)
                    {
                        MessageBox.Show("删除成功");
                        AddPro_Click(null, null);
                        Maplist_SelectionChanged(null, null);
                    }
                    else
                    {
                        MessageBox.Show("删除失败");
                    }
                }
            }
            else
            {
                AddPro_Click(null, null);
            }
           
        }

        /// <summary>
        /// 新建
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddPro_Click(object sender, RoutedEventArgs e)
        {
            LineRest();
            SubmitPro.IsEnabled = true;
            ProgramNO.IsEnabled = true;
            DelPro.IsEnabled = true;
            ProgramName.Text = "";
            ProgramNO.Text = "0";
            edid = 0;
            DataTable dr = new DataTable();
            EditlineData.ItemsSource = dr.DefaultView;
            dr.Columns.Add(new DataColumn("Tag"));
            dr.Columns.Add(new DataColumn("Speed"));
            dr.Columns.Add(new DataColumn("Pbs"));
            dr.Columns.Add(new DataColumn("Turn"));
            dr.Columns.Add(new DataColumn("Direction"));
            dr.Columns.Add(new DataColumn("Hook"));
            dr.Columns.Add(new DataColumn("Stop"));
            dr.Columns.Add(new DataColumn("ChangeProgram"));
            manag.table = dr;
            manag.tagType = true;
            manag.TagCic();
        }

        /// <summary>
        /// 匹配是否为数字
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool IsFloat(string str)
        {
            string regextext = @"^(-?\d+)(\.\d+)?$";
            Regex regex = new Regex(regextext, RegexOptions.None);
            return regex.IsMatch(str.Trim());
        }

        private void ProgramNO_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ProgramName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }


        TagInfoBLL tagInfo = new TagInfoBLL();


        public DataTable ProcessDataNow(DataTable originalDt)
        {
            // 创建一个副本以避免修改原始DataTable
            DataTable newDt = originalDt.Copy();

            // 假设第二列是X，第三列是Y
            foreach (DataRow row in newDt.Rows)
            {
                // 对第二列(X)的数据乘以10再减去19
                row[1] = Convert.ToDouble(row[1]) * 10;

                // 对第三列(Y)的数据乘以10再减去11.5
                row[2] = Convert.ToDouble(row[2]) * 10;
            }

            return newDt;
        }



        //从线路处下发任务
        private async void Distribution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lineRo.SelectedIndex <= 0)
                {
                    MessageBox.Show("请选择有效的线路！");
                    return;
                }

                int int1 = lineRo.SelectedIndex - 1;

                MapMessageBLL messageBLL = new MapMessageBLL();
                string ls = ((ComboBoxItem)maplist.SelectedItem)?.Tag?.ToString();

                if (string.IsNullOrEmpty(ls))
                {
                    MessageBox.Show("未选择地图，请先选择地图！");
                    return;
                }

                string[] arr = ls.Split(',');
                DataTable itemTag = tagInfo.RataTable(arr[2]);

                if (dtRoute == null || dtRoute.Rows.Count <= int1)
                {
                    MessageBox.Show("无效的线路数据，请检查配置！");
                    return;
                }

                string newTag = dtRoute.Rows[int1]["Tag"]?.ToString();
                string[] newTagar = newTag?.Split(',');
                if (newTagar == null || newTagar.Length == 0)
                {
                    MessageBox.Show("目标点位为空，请检查线路配置！");
                    return;
                }

                string newSpeed = dtRoute.Rows[int1]["Speed"]?.ToString();
                string[] newSpeedar = newSpeed?.Split(',');

                string newHook = dtRoute.Rows[int1]["Hook"]?.ToString();
                string[] newHookar = newHook?.Split(',');

                string strChangeProgram = dtRoute.Rows[int1]["ChangeProgram"]?.ToString();
                string[] ChangeProgramar = strChangeProgram?.Split(',');

                string newTurn = dtRoute.Rows[int1]["Turn"]?.ToString();
                string[] newTurnar = newTurn?.Split(',');

                string obsAvoidance = dtRoute.Rows[int1]["Direction"]?.ToString();
                string[] obsAvoidancear = obsAvoidance?.Split(',');

                if (itemTag == null || itemTag.Rows.Count == 0)
                {
                    MessageBox.Show("点位数据为空，请检查地图和配置！");
                    return;
                }

                DataTable newDt = new DataTable();
                newDt.Columns.Add(new DataColumn("Tag"));
                newDt.Columns.Add(new DataColumn("X"));
                newDt.Columns.Add(new DataColumn("Y"));
                newDt.Columns.Add(new DataColumn("Speed"));
                newDt.Columns.Add(new DataColumn("Hook"));
                newDt.Columns.Add(new DataColumn("AngleSpeed"));
                newDt.Columns.Add(new DataColumn("Turn"));
                newDt.Columns.Add(new DataColumn("ObsAvoidance"));

                for (int i = 0; i < newTagar.Length; i++)
                {
                    var tagRow = itemTag.Select($"TagName = '{newTagar[i]}'");
                    if (tagRow.Length == 0)
                    {
                        MessageBox.Show($"无法找到点位 {newTagar[i]} 的数据！");
                        return;
                    }

                    newDt.Rows.Add(new object[]
                    {
                     newTagar[i],
                     Convert.ToDouble(tagRow[0]["X"]),
                     Convert.ToDouble(tagRow[0]["Y"]),
                     TagCompile.agvSpeed[Convert.ToInt32(newSpeedar[i])],
                     TagCompile.agvHook[Convert.ToInt32(newHookar[i])],
                     ChangeProgramar[i],
                     TagCompile.agvTurn[Convert.ToInt32(newTurnar[i])],
                     TagCompile.agvDire[Convert.ToInt32(obsAvoidancear[i])]
                    });
                }

                DataTable newDt2 = ProcessData(newDt);
                newDt2 = ProcessDataNow(newDt2);

                List<Tuple<Point, int, double, double, double, double>> newStagingTagPointRatate = PopulateListFromDataTable(newDt2);

                Painting painting = new Painting();
                List<Tuple<Point, int, double, double, double, double>> transformedPointsByRotate =
                    painting.TransformCoordinatesByTurnRotate(newStagingTagPointRatate, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);

                List<newPointStraightWithAngle> processedPoints = ProcessPointsByTurnRotate(transformedPointsByRotate, 0.001);

                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string fileName = "TargetPoints.json";
                string filePath = System.IO.Path.Combine(folderPath, fileName);

                newStationData stationData = new newStationData();
                stationData.Stations.AddRange(processedPoints);

                string jsonString = JsonSerializer.Serialize(stationData, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(filePath, jsonString);

                var client = MqttConnectionManager.LatestClient;
                if (client == null)
                {
                    MessageBox.Show("当前未选择有效的 MQTT 连接（LatestClient 为 null）。");
                    return;
                }
                if (!client.IsConnected)
                {
                    MessageBox.Show("MQTT客户端未连接，请先建立连接。");
                    return;
                }

                if (client != null && client.IsConnected)
                {
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("AGV/Carrier/MapLine")
                        .WithPayload(jsonString)
                        .WithExactlyOnceQoS()
                        .WithRetainFlag(false)
                        .Build();

                    bool sentSuccess = false;
                    int maxRetryTime = 10 * 1000; // 10秒
                    int elapsedTime = 0;
                    int retryInterval = 1000; // 1秒

                    while (!sentSuccess && elapsedTime < maxRetryTime)
                    {
                        try
                        {
                            await client.PublishAsync(message);
                            sentSuccess = true; // 发送成功
                            MessageBox.Show("路线已下发！");
                        }
                        catch (Exception ex)
                        {
                            elapsedTime += retryInterval;
                            if (elapsedTime >= maxRetryTime)
                            {
                                MessageBox.Show($"发送失败，已重试 10 秒：{ex.Message}");
                            }
                            else
                            {
                                await Task.Delay(retryInterval);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("MQTT客户端未连接，请先建立连接。");
                }

                LineRest();
                if (lineRo.SelectedItem != null && !(lineRo.SelectedItem as ComboBoxItem).Content.ToString().Equals("请选择") && lineRo.Items.Count - 1 > 0)
                {
                    TagLine(lineRo.SelectedIndex - 1, (lineRo.SelectedItem as ComboBoxItem).Content.ToString());
                }
                else
                {
                    MessageBox.Show("无线路，请重新选择");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}");
            }
        }



        public  DataTable ProcessData(DataTable inputTable)
        {
            // 定义映射关系
            var agvSpeedMapping = new Dictionary<string, double>
        {
            { "0.5", 0.5 },
            { "-0.5", -0.5 },
            { "0.6", 0.6 },
            { "-0.6", -0.6 },
            { "0.8", 0.8 },
            { "-0.8", -0.8 },
            { "1.0", 1.0 },
            { "-1.0", -1.0 },
            { "1.2", 1.2 },
            { "-1.2", -1.2 },
            { "缺省", 0.7 } // -1 代表无变化
        };

            var agvHookMapping = new Dictionary<string, int>
        {
            { "移动前下降", 12 },
            { "移动前升起", 11 },
            { "移动后下降", 22 },
            { "移动后升起", 21 },
            { "缺省", 0 }
        };

            var agvTurn = new Dictionary<string, double>
        {
            { "默认", 1 },
            { "正向", 1 },
            { "反向", 0 }
        };
            var agvObsAvoidancear = new Dictionary<string, double>
            {
                {"避障", 0 },
                {"不避障", 1},
                {"缺省", 0 }
            };


            // 创建新 DataTable
            DataTable newDt = new DataTable();
            newDt.Columns.Add(new DataColumn("Tag", typeof(int)));
            newDt.Columns.Add(new DataColumn("X", typeof(double)));
            newDt.Columns.Add(new DataColumn("Y", typeof(double)));
            newDt.Columns.Add(new DataColumn("Speed", typeof(double)));
            newDt.Columns.Add(new DataColumn("Hook", typeof(int)));
            newDt.Columns.Add(new DataColumn("AngleSpeed", typeof(double)));
            newDt.Columns.Add(new DataColumn("Turn", typeof(double)));
            newDt.Columns.Add(new DataColumn("ObsAvoidance", typeof(double)));

            // 填充新 DataTable
            foreach (DataRow row in inputTable.Rows)
            {
                int tag = Convert.ToInt32(row["Tag"]);
                double x = Convert.ToDouble(row["X"]);
                double y = Convert.ToDouble(row["Y"]);
                double speed = agvSpeedMapping[row["Speed"].ToString()];
                int hook = agvHookMapping[row["Hook"].ToString()];
                //double angleSpeed = Convert.ToDouble(row["AngleSpeed"]);
                double angleSpeed = 0; 

                string angleInput = row["AngleSpeed"].ToString().Trim();

                if (angleInput == "default")
                {
                    angleSpeed = 0.25;
                }
                else if (!double.TryParse(angleInput, out angleSpeed))
                {
                    throw new ArgumentException($"无效的角速度输入值：'{angleInput}'，应为数值或 'default'");
                }
                double turn = agvTurn[row["Turn"].ToString()];
                double obsAvoidance = agvObsAvoidancear[row["ObsAvoidance"].ToString()];

                newDt.Rows.Add(new object[] { tag, x, y, speed, hook, angleSpeed, turn, obsAvoidance });
            }
                
            return newDt;
        }

        public List<Tuple<Point, int, double, double, double, double>> PopulateListFromDataTable(DataTable dataTable)
        {
            var newStagingTagPointRatate = new List<Tuple<Point, int, double, double, double, double>>();

            foreach (DataRow row in dataTable.Rows)
            {
                double x = Convert.ToDouble(row["X"]);
                double y = Convert.ToDouble(row["Y"]);
                int hook = Convert.ToInt32(row["Hook"]);
                double speed = Convert.ToDouble(row["Speed"]);
                double angleSpeed = Convert.ToDouble(row["AngleSpeed"]);
                double turn = Convert.ToDouble(row["Turn"]);
                double obsAvoidance = Convert.ToDouble(row["ObsAvoidance"]);

                Point point = new Point(x, y);
                var tuple = new Tuple<Point, int, double, double, double, double>(point, hook, speed, angleSpeed, turn, obsAvoidance);
                newStagingTagPointRatate.Add(tuple);
            }

            return newStagingTagPointRatate;
        }






        /// <summary>
        /// 提交
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubmitPro_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ProgramNO.Text) || string.IsNullOrEmpty(Convert.ToString(ProgramName.Text)))
            {
                MessageBox.Show("请输入线路名称及线路号");
                return;
            }
            if (!IsFloat(ProgramNO.Text.Trim()))
            {
                MessageBox.Show("线路号只能为数字");
                return;
            }
            else if (EditlineData.Items.Count == 0)
            {
                MessageBox.Show("未编辑线路");
                return;
            }
            StringBuilder sbTag = new StringBuilder();
            StringBuilder sbSpeed = new StringBuilder();
            StringBuilder sbStop = new StringBuilder();
            StringBuilder sbTurn = new StringBuilder();
            StringBuilder sbDirection = new StringBuilder();
            StringBuilder sbPbs = new StringBuilder();
            StringBuilder sbHook = new StringBuilder();
            StringBuilder sbProgram = new StringBuilder();

            for (int i = 0; i < EditlineData.Items.Count; i++)
            {
                sbTag.Append(((DataRowView)EditlineData.Items[i])[0]);
                sbTag.Append(",");
                sbSpeed.Append(tag.agvSpeedIndex(((DataRowView)EditlineData.Items[i])[1].ToString()));
                sbSpeed.Append(",");
                sbStop.Append(((DataRowView)EditlineData.Items[i])[6]);
                sbStop.Append(",");
                sbTurn.Append(tag.agvTurnIndex(((DataRowView)EditlineData.Items[i])[3].ToString()));
                sbTurn.Append(",");
                sbDirection.Append(tag.agvDireIndex(((DataRowView)EditlineData.Items[i])[4].ToString()));
                sbDirection.Append(",");
                sbPbs.Append(tag.agvPbsIndex(((DataRowView)EditlineData.Items[i])[2].ToString()));
                sbPbs.Append(",");
                sbHook.Append(tag.agvHookIndex(((DataRowView)EditlineData.Items[i])[5].ToString()));
                sbHook.Append(",");
                sbProgram.Append(((DataRowView)EditlineData.Items[i])[7]);
                sbProgram.Append(",");
            }

            sbTag.Remove(sbTag.Length - 1, 1);
            sbSpeed.Remove(sbSpeed.Length - 1, 1);
            sbStop.Remove(sbStop.Length - 1, 1);
            sbTurn.Remove(sbTurn.Length - 1, 1);
            sbDirection.Remove(sbDirection.Length - 1, 1);
            sbPbs.Remove(sbPbs.Length - 1, 1);
            sbHook.Remove(sbHook.Length - 1, 1);
            sbProgram.Remove(sbProgram.Length - 1, 1);

            string tagStr = sbTag.ToString();
            string speedStr = sbSpeed.ToString();
            string stopStr = sbStop.ToString();
            string turnStr = sbTurn.ToString();
            string direStr = sbDirection.ToString();
            string pbsStr = sbPbs.ToString();
            string hookStr = sbHook.ToString();
            string programStr = sbProgram.ToString();

            string agvStr = "";//地图上不用注册agv，为保证程序正常运行保留字段。
            if (edid.Equals(0))
            {
                if (messageBLL.Program(ProgramNO.Text.Trim(), Times))
                {
                    MessageBox.Show("线路号已存在，请重新输入线路号");
                    return;
                }
                else
                {
                    if (messageBLL.InsertRouteMap(ProgramNO.Text.Trim(), ProgramName.Text.Trim(), UTC.ConvertDateTimeLong(DateTime.Now), Times, tagStr, speedStr, stopStr, turnStr, direStr, pbsStr, hookStr, agvStr, programStr))
                    {
                        MessageBox.Show("保存成功");
                        Maplist_SelectionChanged(null, null);
                    }
                    else
                    {
                        MessageBox.Show("保存失败");
                    }
                }
            }
            else if (edid.Equals(1))
            {
                if (messageBLL.UpdateRouteMap(Times, Convert.ToInt32(ProgramNO.Text.Trim()), ProgramName.Text.Trim(), tagStr, speedStr, stopStr, turnStr, direStr, pbsStr, hookStr, agvStr, programStr))
                {
                    MessageBox.Show("保存成功");
                    Maplist_SelectionChanged(null, null);
                }
                else
                {
                    MessageBox.Show("保存失败");
                }
            }
        }
    }
}
