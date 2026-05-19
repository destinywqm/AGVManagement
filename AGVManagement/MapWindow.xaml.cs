// ============================================================
//  MainWindow.xaml.cs（重构后）
//  职责：地图编辑窗口 UI 事件响应
//  业务逻辑已分离至：
//    Services/TaskService.cs   —— MES 任务调度
//    Helpers/PathHelper.cs     —— 路径点计算
//    Helpers/MqttPublishHelper —— MQTT 发布封装
//    Models/AgvModels.cs       —— 数据模型
// ============================================================
using AGV.BLL;
using AGV.DAL;
using AGVManagement.Helpers;
using AGVManagement.instrument;
using AGVManagement.MapPaint;
using AGVManagement.Models;
using AGVManagement.Mqtt;
using AGVManagement.Services;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AGVManagement
{
    public partial class MainWindow : Window
    {
        // ── 字段 ──────────────────────────────────────────────────────────
        public static long Time;

        private readonly LineInfoBLL _lineBLL = new LineInfoBLL();
        private readonly RouteInfoBLL _routeBLL = new RouteInfoBLL();
        private readonly TagInfoBLL _tagBLL = new TagInfoBLL();
        private readonly MapMessageBLL _mapMessage = new MapMessageBLL();
        private readonly Painting _painting = new Painting();
        private readonly PointHandle _pointHandle = new PointHandle();
        private readonly MapInstrument _instrument = new MapInstrument();
        private readonly TaskService _taskService;

        private double _mapWidthPx, _mapHeightPx;
        private double _actualWidth, _actualHeight, _scale;
        private System.Windows.Point _scrollOffset;
        private static bool _autoReconnect = true;

        // ── 编辑锁定标志 ──────────────────────────────────────────────────
        /// <summary>
        /// true = 当前有 AGV 已连接并显示，编辑操作被锁定。
        /// 只有所有 AGV 全部断开后才会变回 false。
        /// </summary>
        private bool _editLocked = false;

        // ── 构造 ──────────────────────────────────────────────────────────
        public MainWindow(long mapTime, string mapName, string mapNs,
            double width, double height,
            double newWidth, double newHeight, double scaleRatio)
        {
            InitializeComponent();

            Painting.siseWin = 1;
            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();
            mainPanel.Children.Clear();

            Time = mapTime;
            LoadMap(mapTime, mapName, mapNs, width, height, newWidth, newHeight, scaleRatio);

            _taskService = new TaskService(
                _lineBLL, _routeBLL, _tagBLL, _painting, _pointHandle, Dispatcher);

            // ── 监听「全部断开」事件，在 UI 线程上解锁编辑 ──────────────
            MqttConnectionManager.AllClientsDisconnected += () =>
                Dispatcher.Invoke(UnlockEditing);
        }

        // ─────────────────────────────────────────────────────────────────
        //  编辑锁定 / 解锁
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 锁定所有编辑操作按钮与画布交互。
        /// 在 AMR 连接成功且车辆 UI 首次出现后调用。
        /// </summary>
        public void LockEditing()
        {
            if (_editLocked) return;
            _editLocked = true;

            // 禁用工具栏编辑按钮（保留 连接AMR、任务控制 等运行时按钮）
            Button[] editButtons =
            {
                btn_mouse, DeleteCircuit, Tags,
                Straight, Broken, Btn_cren, Btn_bezierCurve
            };
            foreach (var btn in editButtons)
                btn.IsEnabled = false;

            // 禁用保存、导出（防止误保存正在运行的地图）
            Save.IsEnabled = false;
            Export.IsEnabled = false;

            // 禁用画布上的鼠标交互（信标拖动、路线绘制等）
            mainPanel.IsHitTestVisible = false;

            // 重置绘图工具状态（防止已激活的工具继续响应）
            _instrument.mouseStatic();
        }

        /// <summary>
        /// 解锁所有编辑操作。在所有 AGV 全部断开后调用。
        /// </summary>
        public void UnlockEditing()
        {
            if (!_editLocked) return;
            _editLocked = false;

            Button[] editButtons =
            {
                btn_mouse, DeleteCircuit, Tags,
                Straight, Broken, Btn_cren, Btn_bezierCurve
            };
            foreach (var btn in editButtons)
                btn.IsEnabled = true;

            Save.IsEnabled = true;
            Export.IsEnabled = true;
            mainPanel.IsHitTestVisible = true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  地图加载
        // ─────────────────────────────────────────────────────────────────

        public void SetBackgroundImage(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                MessageBox.Show("图片路径不存在！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Geenh.Background = new ImageBrush(
                new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(imagePath, UriKind.RelativeOrAbsolute)));
        }

        private void LoadMap(long mapTime, string mapName, string mapNs,
            double w, double h, double nw, double nh, double scale)
        {
            if (!mapTime.Equals(0))
                LoadExistingMapInfo(mapTime, mapName);
            else
                InitCanvas(w, h, mapNs, w, h, nw, nh, scale);
        }

        private void LoadExistingMapInfo(long mapTime, string mapName)
        {
            DataTable da = _mapMessage.MapParray(mapName);
            if (da == null) { MessageBox.Show("地图丢失"); return; }

            foreach (DataRow row in da.Rows)
            {
                double w = Convert.ToDouble(row["Width"]);
                double h = Convert.ToDouble(row["Height"]);
                double nw = Convert.ToDouble(row["ActualWidth"]);
                double nh = Convert.ToDouble(row["ActualHeight"]);
                double sc = Convert.ToDouble(row["Scale"]);

                MqttClientWrapper.ActualWidthNow = nw;
                MqttClientWrapper.ActualHeightNow = nh;
                MqttClientWrapper.ProportionNow = sc;

                int type = Convert.ToInt32(row["Type"]);
                TypeMp.SelectedIndex = type == 1 ? 0 : type == 0 ? 1 : 2;

                InitCanvas(w, h, row["Name"].ToString(), w, h, nw, nh, sc);
                _instrument.LoadDataInfo(mainPanel, mapTime);
                break;
            }
        }

        private void InitCanvas(double w, double h, string name,
            double mpW, double mpH, double nw, double nh, double scale)
        {
            MP.Text = name;
            MpWidth.Content = mpW + "m";
            MpHeight.Content = mpH + "m";

            _mapWidthPx = w * 10;
            _mapHeightPx = h * 10;
            _actualWidth = nw;
            _actualHeight = nh;
            _scale = scale;

            mainPanel.Width = _mapWidthPx;
            mainPanel.Height = _mapHeightPx;
            TopX.Width = _mapWidthPx;
            TopY.Height = _mapHeightPx;

            _painting.Coordinate(mainPanel);
            _painting.CoordinateX(TopX, TopY);
        }

        // ─────────────────────────────────────────────────────────────────
        //  窗口关闭
        // ─────────────────────────────────────────────────────────────────

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach (var c in MqttConnectionManager.MqttClients.Values)
                    c.DisableAutoReconnect();
                foreach (var c in MqttConnectionManager.MqttClients.Values)
                    if (c.IsConnected) await c.DisconnectAsync();

                MqttConnectionManager.MqttClients.Clear();

                lock (GlobalData.AgvData)
                {
                    GlobalData.AgvData.Clear();
                    foreach (var row in new[]
                    {
                        ("AGV",""),("网络状态","未连接"),("运行状态",""),
                        ("车辆坐标X",""),("车辆坐标Y",""),("运行准备",""),
                        ("驱动下降",""),("脱轨",""),("扫描区域",""),
                        ("电压",""),("报警",""),("报警信息","")
                    })
                        GlobalData.AgvData.Rows.Add(row.Item1, row.Item2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭连接时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  地图缩放（滚轮 + 右键恢复）
        // ─────────────────────────────────────────────────────────────────

        private void SrcCount_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) return;
            e.Handled = true;

            double delta = e.Delta > 0 ? 0.01 : -0.01;
            double scale = MapScaleTransform.ScaleX + delta;
            if (scale < 0.1) scale = 0.1;
            if (scale > 5.0) scale = 5.0;
            MapScaleTransform.ScaleX = MapScaleTransform.ScaleY = scale;
        }

        private void SrcCount_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                MapScaleTransform.ScaleX = MapScaleTransform.ScaleY = 1.0;
                e.Handled = true;
            }
        }

        private void SrcCount_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _scrollOffset.X = e.HorizontalOffset;
            _scrollOffset.Y = e.VerticalOffset;
            SrcX.ScrollToHorizontalOffset(e.HorizontalOffset);
            SrcY.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private void SliMax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => _painting.Mapmagnify(
                Convert.ToInt32(e.NewValue), TopX, TopY, mainPanel, _mapWidthPx, _mapHeightPx);

        // ─────────────────────────────────────────────────────────────────
        //  保存 / 导出
        // ─────────────────────────────────────────────────────────────────

        private readonly SaveMap _saveMap = new SaveMap();

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            int typeId = TypeMp.SelectedIndex == 0 ? 1 : TypeMp.SelectedIndex == 1 ? 0 : 2;
            bool ok = _saveMap.SaveAtlas(
                (!Time.Equals(0) ? Time.ToString() : UTC.ConvertDateTimeLong(DateTime.Now).ToString()),
                !Time.Equals(0) ? false : true,
                MP.Text, _mapWidthPx / 10, _mapHeightPx / 10,
                "0", typeId, _actualWidth, _actualHeight, _scale);

            MessageBox.Show(ok ? "保存成功" : "保存失败");
        }

        private void Export_Click(object sender, RoutedEventArgs e) { /* 预留 */ }

        // ─────────────────────────────────────────────────────────────────
        //  工具栏按钮（绘图模式切换）
        //  锁定期间点击无效（按钮本身 IsEnabled=false，事件不会触发）
        // ─────────────────────────────────────────────────────────────────

        private void Tags_Click(object s, RoutedEventArgs e) { ActivateButton(Tags); _instrument.TagNew(mainPanel, _scrollOffset); }
        private void Straight_Click(object s, RoutedEventArgs e) { ActivateButton(Straight); _instrument.Mapstraight(); }
        private void btn_mouse_Click(object s, RoutedEventArgs e) { ActivateButton(btn_mouse); _instrument.mouseStatic(); }
        private void Broken_Click(object s, RoutedEventArgs e) { ActivateButton(Broken); _instrument.Brokene(); }
        private void Btn_cren_Click(object s, RoutedEventArgs e) { ActivateButton(Btn_cren); _instrument.Semicircles(); }
        private void Btn_bezierCurve_Click(object s, RoutedEventArgs e) { ActivateButton(Btn_bezierCurve); _instrument.BezierCurve(); }
        private void DeleteCircuit_Click(object s, RoutedEventArgs e) { ActivateButton(DeleteCircuit); _instrument.ClearTen(); }

        // ─────────────────────────────────────────────────────────────────
        //  MQTT 指令按钮（运行期间保持可用）
        // ─────────────────────────────────────────────────────────────────

        private async void RoutePlanningButtonClick(object s, RoutedEventArgs e)
            => await MqttPublishHelper.PublishAsync(
                "AGV/Response/Mapping", "Mapping", "建图脚本已下发。");

        private async void LineDistributionButtonClick(object s, RoutedEventArgs e)
            => await MqttPublishHelper.PublishAsync(
                "AGV/Response/Localization", "Orientation", "定位脚本已下发。");

        private async void Btn_TaskTesting_Click(object s, RoutedEventArgs e)
            => await MqttPublishHelper.PublishAsync(
                "AGV/Response/TaskDistribution", "TaskDistribution", "任务脚本已下发。");

        private async void Btn_TaskTermination_Click(object s, RoutedEventArgs e)
        {
            if (MessageBox.Show("警告！确认要终止任务吗？", "提示",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                await MqttPublishHelper.PublishAsync(
                    "AGV/Response/TaskTermination", "TaskTermination", "终止任务脚本已下发。");
        }

        private async void Area_Click(object s, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Mes 系统连接已建立！", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await _taskService.ProcessTasksSequentiallyAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  连接 AMR（MQTT）
        //  InitializeAsync 成功（TCP握手完成）后立即锁定编辑区；
        //  连接失败则保持解锁。
        // ─────────────────────────────────────────────────────────────────

        private async void Btn_Text_Click(object s, RoutedEventArgs e)
        {
            try
            {
                var dlg = new mqtt();
                if (dlg.ShowDialog() != true) return;

                string address = dlg.Address;
                int port = dlg.Port;

                if (MqttConnectionManager.MqttClients.ContainsKey(address)
                    && MqttConnectionManager.MqttClients[address].IsConnected)
                {
                    MessageBox.Show($"AGV 已连接：{address}");
                    return;
                }

                var wrapper = new MqttClientWrapper(Dispatcher, address, dlg.SelectedLength, dlg.SelectedWidth);

                // ── 连接成功后立即锁定，失败则 catch 里不会执行到此处 ──
                await wrapper.InitializeAsync(address, port, mainPanel);
                LockEditing();

                MqttConnectionManager.AddClient(address, wrapper, dlg.SelectedLength, dlg.SelectedWidth);
                MqttConnectionManager.SetCurrent(address);
                GlobalData.UpdateAgvInfo("AGV", address);
                GlobalDisplayData.UpdateDisplayInfo(address, "AGV", address);
                _autoReconnect = true;
            }
            catch (Exception ex)
            {
                // 连接失败：确保编辑区保持可用（若此次是第一次连接则无需解锁）
                // 若已有其他 AGV 在线则维持锁定状态，由 AllClientsDisconnected 统一解锁
                MessageBox.Show($"连接失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  重定位
        // ─────────────────────────────────────────────────────────────────

        private async void LoadFromFile_Click(object s, RoutedEventArgs e)
        {
            try
            {
                DataTable tagStation = _tagBLL.RataTable(Time.ToString());
                var dlg = new RepositioningTag(tagStation);
                if (dlg.ShowDialog() != true) return;

                var coords = GetTagCoordinates(tagStation, dlg.dataResStatu);
                if (coords == null) return;

                double x = coords.Value.X, y = coords.Value.Y, z = Convert.ToDouble(dlg.dataResAngel);
                var pt = _painting.TransformCoordinate(
                    new System.Windows.Point(x * 10, y * 10),
                    MqttClientWrapper.ActualWidthNow,
                    MqttClientWrapper.ActualHeightNow,
                    MqttClientWrapper.ProportionNow);

                var client = MqttConnectionManager.LatestClient;
                if (client == null || !client.IsConnected)
                {
                    MessageBox.Show("MQTT 客户端未连接，请先建立连接。", "警告",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var carData = new CarData { X = pt.X, Y = pt.Y, Z = z };
                var json = JsonSerializer.Serialize(carData);
                var msg = new MqttApplicationMessageBuilder()
                    .WithTopic("AGV/Response/Repositioning")
                    .WithPayload(json).WithExactlyOnceQoS().WithRetainFlag(false).Build();

                await client.PublishAsync(msg);

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (_, __) => { timer.Stop(); MessageBox.Show("重定位成功。", "成功"); };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发布失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  MES 路线下发
        // ─────────────────────────────────────────────────────────────────

        private async void OnInputLineButtonClick(object s, RoutedEventArgs e)
        {
            try
            {
                DataTable tagStation = _tagBLL.RataTable(Time.ToString());
                var dlg = new LineInputDialog(tagStation);
                if (dlg.ShowDialog() != true) return;

                string origin = dlg.dataMesStartStatu, dest = dlg.dataMesEndStatu;

                var rawData = JsonSerializer.Deserialize<CarData>(MqttClientWrapper.payloadAll);
                var worldPt = _painting.ReverseTransformCoordinate(
                    new System.Windows.Point(rawData.X, rawData.Y),
                    MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow,
                    MqttClientWrapper.ProportionNow);
                var curPos = new System.Windows.Point(worldPt.X / 10, worldPt.Y / 10);

                DataTable lineStation = _lineBLL.LinelistArrer(Time.ToString());
                DataTable routeStation = _routeBLL.RoutelistArrer(Time.ToString());

                DataTable expanded = Painting.ExpandBidirectionalRoutes(lineStation);
                DataTable route = Painting.ProcessDataTable(Painting.FindRouteNew(expanded, curPos, origin));
                DataTable route10 = TransformCoords10x(route);

                List<System.Windows.Point> points = ExtractPoints(route10);

                var tuples = points.Select(p =>
                    Tuple.Create(p, 0, 0.7, 0.3)).ToList();

                var transformed = _painting.TransformCoordinatesByRotate(
                    tuples, MqttClientWrapper.ActualWidthNow,
                    MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);
                transformed = _painting.RemoveRedundantRoutes(transformed);

                int rowIdx = _pointHandle.FindRowByTags(routeStation, origin, dest);
                var mesEnd = _pointHandle.FindMesEnd(routeStation, Time.ToString(), rowIdx);

                var combined = _pointHandle.CombineLists(transformed, mesEnd);
                var processed = PathHelper.ProcessPointsByTurnRotate(combined, 0.001);

                var stationData = new NewStationData();
                stationData.Stations.AddRange(processed);
                string json = JsonSerializer.Serialize(stationData,
                    new JsonSerializerOptions { WriteIndented = true });

                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MesPoints.json");
                File.WriteAllText(filePath, json);

                await MqttPublishHelper.PublishAsync(
                    "AGV/Carrier/MapLineMes", json, "点位数据已成功保存到文件。");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  充电 CBS DLL 测试
        // ─────────────────────────────────────────────────────────────────

        private async void InsertImg_Click(object s, RoutedEventArgs e)
        {
            try
            {
                string json = File.ReadAllText(@"E:\ATF\code\test.json");
                string output = CppCBSLib.GetMultiAgentPaths(json);
                await Painting.ProcessAndPublishPerAgentAsync(output);
                MessageBox.Show(output, "DLL 输出结果",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调用 DLL 出错：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  辅助
        // ─────────────────────────────────────────────────────────────────

        public static (double X, double Y)? GetTagCoordinates(DataTable tagTable, string resTag)
        {
            if (tagTable == null || string.IsNullOrEmpty(resTag)) return null;
            string id = resTag.Substring(2);
            var row = tagTable.AsEnumerable()
                .FirstOrDefault(r => r.Field<string>("TagName") == id);
            return row == null ? null
                : ((double X, double Y)?)(row.Field<double>("X"), row.Field<double>("Y"));
        }

        private static List<System.Windows.Point> ExtractPoints(DataTable routes)
        {
            var pts = new List<System.Windows.Point>();
            for (int i = 0; i < routes.Rows.Count; i++)
            {
                DataRow row = routes.Rows[i];
                pts.Add(new System.Windows.Point(
                    Convert.ToDouble(row["StartX"]), Convert.ToDouble(row["StartY"])));
                if (i == routes.Rows.Count - 1)
                    pts.Add(new System.Windows.Point(
                        Convert.ToDouble(row["EndX"]), Convert.ToDouble(row["EndY"])));
            }
            return pts;
        }

        private static DataTable TransformCoords10x(DataTable t)
        {
            var result = new DataTable();
            result.Columns.Add("StartX", typeof(double)); result.Columns.Add("StartY", typeof(double));
            result.Columns.Add("EndX", typeof(double)); result.Columns.Add("EndY", typeof(double));
            foreach (DataRow row in t.Rows)
                result.Rows.Add(
                    Convert.ToDouble(row["StartX"]) * 10, Convert.ToDouble(row["StartY"]) * 10,
                    Convert.ToDouble(row["EndX"]) * 10, Convert.ToDouble(row["EndY"]) * 10);
            return result;
        }

        private void ActivateButton(Button active)
        {
            var defaultBg = Color.FromRgb(96, 125, 139);
            var highlight = Color.FromRgb(249, 92, 38);
            var white = Colors.White;

            Button[] all = { btn_mouse, DeleteCircuit, Tags, Straight, Broken,
                             Btn_cren, Btn_bezierCurve, Area, InsertImg,
                             Btn_RoutePlanning, Btn_LineDistribution, Btn_Text };

            foreach (var b in all)
            {
                b.Background = new SolidColorBrush(defaultBg);
                b.Foreground = new SolidColorBrush(white);
            }
            active.Background = new SolidColorBrush(highlight);
            active.Foreground = new SolidColorBrush(white);
        }
    }
}