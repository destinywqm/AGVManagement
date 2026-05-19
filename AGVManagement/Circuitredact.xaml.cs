// ============================================================
//  Circuitredact.xaml.cs（重构后）
//  主要变更：
//    - 移除 using static AGVManagement.MainWindow
//    - 改为 using AGVManagement.Models
//    - newPointStraightWithAngle → NewPointStraightWithAngle
//    - newStationData            → NewStationData
//    - ProcessPointsByTurnRotate 调用改为 PathHelper.ProcessPointsByTurnRotate
//    - 删除多余的空事件、无用 using
// ============================================================
using AGV.BLL;
using AGVManagement.Helpers;
using AGVManagement.instrument;
using AGVManagement.MapPaint;
using AGVManagement.Models;
using AGVManagement.Mqtt;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AGVManagement
{
    public partial class Circuitredact : Window
    {
        // ── 字段 ──────────────────────────────────────────────────────────
        private readonly MapManag _manag = new MapManag();
        private readonly TagCompile _tag = new TagCompile();
        private readonly OperateDBBLL _operate = new OperateDBBLL();
        private readonly MapMessageBLL _messageBLL = new MapMessageBLL();
        private readonly TagInfoBLL _tagInfo = new TagInfoBLL();

        private double _mpWidth, _mpHeight;
        private long _times;
        private DataTable _dtRoute = new DataTable();
        private int _editMode = 0; // 0=新建 1=编辑

        // ── 构造 ──────────────────────────────────────────────────────────
        public Circuitredact()
        {
            InitializeComponent();
            LoadMapComboBox();

            // 角速度列绑定 Converter
            var converter = new DefaultToChineseConverter();
            var col = EditlineData.Columns
                .OfType<DataGridTextColumn>()
                .FirstOrDefault(c => c.Header?.ToString() == "角速度");
            if (col != null)
                col.Binding = new Binding("ChangeProgram") { Converter = converter };
        }

        // ─────────────────────────────────────────────────────────────────
        //  地图下拉框
        // ─────────────────────────────────────────────────────────────────

        private void LoadMapComboBox()
        {
            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();
            Painting.siseWin = 1;
            SliMax.Value = 0;

            SubmitPro.IsEnabled = false;
            DelPro.IsEnabled = false;

            DataTable maps = new MapMessageBLL().GetMapData(null);
            if (maps == null)
            {
                maplist.Items.Add(new ComboBoxItem { Content = "请选择" });
            }
            else
            {
                foreach (DataRow row in maps.Rows)
                    maplist.Items.Add(new ComboBoxItem
                    {
                        Content = row["Name"].ToString(),
                        Tag = $"{row["Width"]},{row["Height"]},{row["CreateTime"]}"
                    });
            }
            maplist.SelectedIndex = 0;
        }

        private void Maplist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = maplist.SelectedItem as ComboBoxItem;
            if (item?.Tag == null) return;

            SliMax.Value = 0;
            SubmitPro.IsEnabled = true;
            DelPro.IsEnabled = true;
            EditlineData.ItemsSource = new DataTable().DefaultView;
            lineRo.Items.Clear();

            string[] arr = item.Tag.ToString().Split(',');
            _dtRoute = new MapMessageBLL().BLLMapRoute(arr[2]);

            lineRo.Items.Add(new ComboBoxItem { Content = "请选择" });
            if (_dtRoute.Rows.Count == 0)
            {
                SubmitPro.IsEnabled = false;
                DelPro.IsEnabled = false;
            }
            else
            {
                foreach (DataRow row in _dtRoute.Rows)
                    lineRo.Items.Add(new ComboBoxItem
                    {
                        Content = row["Name"].ToString(),
                        Tag = row["Program"].ToString()
                    });
            }
            lineRo.SelectedIndex = 0;

            // 渲染地图
            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();
            Painting.siseWin = 1;
            MapIN.Children.Clear();

            _mpWidth = Convert.ToDouble(arr[0]) * _manag.Sise;
            _mpHeight = Convert.ToDouble(arr[1]) * _manag.Sise;
            MapIN.Width = _mpWidth;
            MapIN.Height = _mpHeight;
            _times = long.Parse(arr[2]);
            _manag.Times = _times;
            _manag.GetData = EditlineData;
            _manag.SelectMap(_times, MapIN, true);
        }

        // ─────────────────────────────────────────────────────────────────
        //  地图缩放
        // ─────────────────────────────────────────────────────────────────

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) return;
            e.Handled = true;

            double newScale = MapScaleTransform.ScaleX * (e.Delta > 0 ? 1.05 : 0.95);
            if (newScale < 0.2 || newScale > 5) return;

            MapScaleTransform.CenterX = MapIN.ActualWidth / 2;
            MapScaleTransform.CenterY = MapIN.ActualHeight / 2;
            MapScaleTransform.ScaleX = newScale;
            MapScaleTransform.ScaleY = newScale;
        }

        private void SliMax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var painting = new Painting { mainPan = MapIN };
            int sis = Convert.ToInt32(e.NewValue);
            MapIN.Children.Clear();
            int scale = sis == 0 ? 1 : sis;
            MapIN.Width = _mpWidth * scale;
            MapIN.Height = _mpHeight * scale;
            painting.Zoom(scale);
            Painting.siseWin = scale;
        }

        // ─────────────────────────────────────────────────────────────────
        //  线路选择 / 还原 / 显示
        // ─────────────────────────────────────────────────────────────────

        private void Line_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ResetLineColors();
                var selItem = lineRo.SelectedItem as ComboBoxItem;
                bool hasSelection = selItem != null
                    && !selItem.Content.ToString().Equals("请选择")
                    && lineRo.Items.Count > 1;

                if (hasSelection)
                {
                    SubmitPro.IsEnabled = true;
                    DelPro.IsEnabled = true;
                    ProgramNO.IsEnabled = false;
                    ProgramNO.Text = selItem.Tag.ToString();
                    ProgramName.Text = selItem.Content.ToString();
                    _editMode = 1;
                    ShowLineDetail(lineRo.SelectedIndex - 1, selItem.Content.ToString());
                }
                else
                {
                    _manag.tagType = false;
                    SubmitPro.IsEnabled = false;
                    DelPro.IsEnabled = false;
                    ProgramName.Text = "";
                    EditlineData.ItemsSource = new DataTable().DefaultView;
                    ProgramNO.Text = "0";
                }
            }
            catch { ProgramNO.Text = "0"; }
        }

        /// <summary>将所有路线 / Tag 颜色还原为默认黑色</summary>
        public void ResetLineColors()
        {
            new MapInstrument().TagFormer();
            foreach (var item in MapInstrument.wirePointArrays)
            {
                if (item.GetPath != null)
                {
                    item.GetPath.Stroke = Brushes.Black;
                    item.GetPath.StrokeThickness = 1;
                }
                item.Paths?.ForEach(p => { p.Stroke = Brushes.Black; p.StrokeThickness = 1; });
            }
        }

        /// <summary>显示指定行的线路详情到 EditlineData</summary>
        public void ShowLineDetail(int index, string lineName)
        {
            string[] tags = _dtRoute.Rows[index]["Tag"].ToString().Split(',');
            string[] speeds = _dtRoute.Rows[index]["Speed"].ToString().Split(',');
            string[] pbs = _dtRoute.Rows[index]["Pbs"].ToString().Split(',');
            string[] turns = _dtRoute.Rows[index]["Turn"].ToString().Split(',');
            string[] dirs = _dtRoute.Rows[index]["Direction"].ToString().Split(',');
            string[] hooks = _dtRoute.Rows[index]["Hook"].ToString().Split(',');
            string[] stops = _dtRoute.Rows[index]["Stop"].ToString().Split(',');
            string[] programs = _dtRoute.Rows[index]["ChangeProgram"].ToString().Split(',');

            var dt = new DataTable();
            foreach (var col in new[] { "Tag", "Speed", "Pbs", "Turn", "Direction", "Hook", "Stop", "ChangeProgram" })
                dt.Columns.Add(col);

            for (int i = 0; i < tags.Length; i++)
                dt.Rows.Add(
                    tags[i],
                    TagCompile.agvSpeed[Convert.ToInt32(speeds[i])],
                    TagCompile.agvPbs[Convert.ToInt32(pbs[i])],
                    TagCompile.agvTurn[Convert.ToInt32(turns[i])],
                    TagCompile.agvDire[Convert.ToInt32(dirs[i])],
                    TagCompile.agvHook[Convert.ToInt32(hooks[i])],
                    stops[i],
                    programs[i]);

            if (tags.Length > 0)
            {
                GetScroll.ScrollToHorizontalOffset(MapInstrument.valuePairs[Convert.ToInt32(tags[0])].Margin.Left - 600);
                GetScroll.ScrollToVerticalOffset(MapInstrument.valuePairs[Convert.ToInt32(tags[0])].Margin.Top - 600);
            }

            EditlineData.ItemsSource = dt.DefaultView;
            EditlineData.AutoGenerateColumns = false;
            _manag.table = dt;
            _manag.tagType = true;
            _manag.TagCic();
            _manag.lineMap.TagClick(_times, Convert.ToInt32(dt.Rows[dt.Rows.Count - 1]["Tag"]),
                EditlineData, dt, false);
        }

        // ─────────────────────────────────────────────────────────────────
        //  DataGrid 行点击
        // ─────────────────────────────────────────────────────────────────

        private void EditlineData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Released || EditlineData.SelectedItems.Count == 0) return;

            var arr = ((DataRowView)EditlineData.SelectedValue).Row.ItemArray.ToList();
            bool isFirst = EditlineData.SelectedIndex == 0;
            int prevTag = isFirst
                ? Convert.ToInt32(((DataRowView)EditlineData.SelectedItem)["Tag"])
                : Convert.ToInt32(((DataRowView)EditlineData.Items[EditlineData.SelectedIndex - 1])["Tag"]);

            _manag.LineMapShow(arr, isFirst, prevTag, EditlineData.SelectedIndex);
        }

        // ─────────────────────────────────────────────────────────────────
        //  新建 / 删除 / 提交
        // ─────────────────────────────────────────────────────────────────

        private void AddPro_Click(object sender, RoutedEventArgs e)
        {
            ResetLineColors();
            SubmitPro.IsEnabled = true;
            DelPro.IsEnabled = true;
            ProgramNO.IsEnabled = true;
            ProgramName.Text = "";
            ProgramNO.Text = "0";
            _editMode = 0;

            var dr = new DataTable();
            foreach (var col in new[] { "Tag", "Speed", "Pbs", "Turn", "Direction", "Hook", "Stop", "ChangeProgram" })
                dr.Columns.Add(col);

            EditlineData.ItemsSource = dr.DefaultView;
            _manag.table = dr;
            _manag.tagType = true;
            _manag.TagCic();
        }

        private void DelPro_Click(object sender, RoutedEventArgs e)
        {
            if (!_editMode.Equals(0))
            {
                if (MessageBox.Show("确认要删除线路吗？", "提示",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                bool ok = _messageBLL.DelRouteMapWithName(_times, ProgramName.Text.Trim());
                MessageBox.Show(ok ? "删除成功" : "删除失败");
                if (ok) { AddPro_Click(null, null); Maplist_SelectionChanged(null, null); }
            }
            else
            {
                AddPro_Click(null, null);
            }
        }

        private void SubmitPro_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ProgramNO.Text) || string.IsNullOrEmpty(ProgramName.Text))
            { MessageBox.Show("请输入线路名称及线路号"); return; }
            if (!IsFloat(ProgramNO.Text.Trim()))
            { MessageBox.Show("线路号只能为数字"); return; }
            if (EditlineData.Items.Count == 0)
            { MessageBox.Show("未编辑线路"); return; }

            // 拼接各字段字符串
            var sb = new Dictionary<string, StringBuilder>();
            foreach (var k in new[] { "Tag", "Speed", "Stop", "Turn", "Dir", "Pbs", "Hook", "Program" })
                sb[k] = new StringBuilder();

            for (int i = 0; i < EditlineData.Items.Count; i++)
            {
                var row = (DataRowView)EditlineData.Items[i];
                sb["Tag"].Append(row[0]); sb["Tag"].Append(',');
                sb["Speed"].Append(_tag.agvSpeedIndex(row[1].ToString())); sb["Speed"].Append(',');
                sb["Stop"].Append(row[6]); sb["Stop"].Append(',');
                sb["Turn"].Append(_tag.agvTurnIndex(row[3].ToString())); sb["Turn"].Append(',');
                sb["Dir"].Append(_tag.agvDireIndex(row[4].ToString())); sb["Dir"].Append(',');
                sb["Pbs"].Append(_tag.agvPbsIndex(row[2].ToString())); sb["Pbs"].Append(',');
                sb["Hook"].Append(_tag.agvHookIndex(row[5].ToString())); sb["Hook"].Append(',');
                sb["Program"].Append(row[7]); sb["Program"].Append(',');
            }

            // 去掉末尾逗号
            string Tag = sb["Tag"].ToString().TrimEnd(',');
            string Speed = sb["Speed"].ToString().TrimEnd(',');
            string Stop = sb["Stop"].ToString().TrimEnd(',');
            string Turn = sb["Turn"].ToString().TrimEnd(',');
            string Dir = sb["Dir"].ToString().TrimEnd(',');
            string Pbs = sb["Pbs"].ToString().TrimEnd(',');
            string Hook = sb["Hook"].ToString().TrimEnd(',');
            string Program = sb["Program"].ToString().TrimEnd(',');
            string agv = "";

            if (_editMode == 0)
            {
                if (_messageBLL.Program(ProgramNO.Text.Trim(), _times))
                { MessageBox.Show("线路号已存在，请重新输入线路号"); return; }

                bool ok = _messageBLL.InsertRouteMap(ProgramNO.Text.Trim(), ProgramName.Text.Trim(),
                    UTC.ConvertDateTimeLong(DateTime.Now), _times,
                    Tag, Speed, Stop, Turn, Dir, Pbs, Hook, agv, Program);
                MessageBox.Show(ok ? "保存成功" : "保存失败");
                if (ok) Maplist_SelectionChanged(null, null);
            }
            else
            {
                bool ok = _messageBLL.UpdateRouteMap(_times, Convert.ToInt32(ProgramNO.Text.Trim()),
                    ProgramName.Text.Trim(), Tag, Speed, Stop, Turn, Dir, Pbs, Hook, agv, Program);
                MessageBox.Show(ok ? "保存成功" : "保存失败");
                if (ok) Maplist_SelectionChanged(null, null);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  路线下发
        // ─────────────────────────────────────────────────────────────────

        private async void Distribution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lineRo.SelectedIndex <= 0) { MessageBox.Show("请选择有效的线路！"); return; }

                int idx = lineRo.SelectedIndex - 1;
                string mapTag = ((ComboBoxItem)maplist.SelectedItem)?.Tag?.ToString();
                if (string.IsNullOrEmpty(mapTag)) { MessageBox.Show("未选择地图，请先选择地图！"); return; }

                string[] arr = mapTag.Split(',');
                DataTable tagTbl = _tagInfo.RataTable(arr[2]);
                if (tagTbl == null || tagTbl.Rows.Count == 0)
                { MessageBox.Show("点位数据为空，请检查地图和配置！"); return; }

                // 读取线路字段
                string[] tags = _dtRoute.Rows[idx]["Tag"].ToString().Split(',');
                string[] speeds = _dtRoute.Rows[idx]["Speed"].ToString().Split(',');
                string[] hooks = _dtRoute.Rows[idx]["Hook"].ToString().Split(',');
                string[] programs = _dtRoute.Rows[idx]["ChangeProgram"].ToString().Split(',');
                string[] turns = _dtRoute.Rows[idx]["Turn"].ToString().Split(',');
                string[] obsts = _dtRoute.Rows[idx]["Direction"].ToString().Split(',');

                // 构建详情表
                var detail = new DataTable();
                foreach (var col in new[] { "Tag", "X", "Y", "Speed", "Hook", "AngleSpeed", "Turn", "ObsAvoidance" })
                    detail.Columns.Add(col);

                for (int i = 0; i < tags.Length; i++)
                {
                    var tr = tagTbl.Select($"TagName = '{tags[i]}'");
                    if (tr.Length == 0) { MessageBox.Show($"无法找到点位 {tags[i]} 的数据！"); return; }
                    detail.Rows.Add(
                        tags[i],
                        Convert.ToDouble(tr[0]["X"]),
                        Convert.ToDouble(tr[0]["Y"]),
                        TagCompile.agvSpeed[Convert.ToInt32(speeds[i])],
                        TagCompile.agvHook[Convert.ToInt32(hooks[i])],
                        programs[i],
                        TagCompile.agvTurn[Convert.ToInt32(turns[i])],
                        TagCompile.agvDire[Convert.ToInt32(obsts[i])]);
                }

                DataTable processed = ProcessDataNow(ProcessData(detail));

                var tuples = PopulateListFromDataTable(processed);
                var painting = new Painting();
                var transformed = painting.TransformCoordinatesByTurnRotate(
                    tuples,
                    MqttClientWrapper.ActualWidthNow,
                    MqttClientWrapper.ActualHeightNow,
                    MqttClientWrapper.ProportionNow);

                // ← 改用 PathHelper，类型改为 NewPointStraightWithAngle
                List<NewPointStraightWithAngle> pts = PathHelper.ProcessPointsByTurnRotate(transformed, 0.001);

                var stationData = new NewStationData();
                stationData.Stations.AddRange(pts);
                string json = JsonSerializer.Serialize(stationData, new JsonSerializerOptions { WriteIndented = true });

                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TargetPoints.json");
                System.IO.File.WriteAllText(filePath, json);

                var client = MqttConnectionManager.LatestClient;
                if (client == null || !client.IsConnected)
                { MessageBox.Show("MQTT 客户端未连接，请先建立连接。"); return; }

                var msg = new MqttApplicationMessageBuilder()
                    .WithTopic("AGV/Carrier/MapLine")
                    .WithPayload(json)
                    .WithExactlyOnceQoS().WithRetainFlag(false).Build();

                // 重试最多 10 秒
                bool sent = false;
                var deadline = DateTime.Now.AddSeconds(10);
                while (!sent && DateTime.Now < deadline)
                {
                    try
                    {
                        await client.PublishAsync(msg);
                        sent = true;
                        MessageBox.Show("路线已下发！");
                    }
                    catch (Exception ex) when (DateTime.Now < deadline)
                    {
                        await Task.Delay(1000);
                        _ = ex;
                    }
                }
                if (!sent) MessageBox.Show("发送失败，已重试 10 秒。");

                // 刷新线路高亮
                ResetLineColors();
                var selItem = lineRo.SelectedItem as ComboBoxItem;
                if (selItem != null && !selItem.Content.ToString().Equals("请选择") && lineRo.Items.Count > 1)
                    ShowLineDetail(lineRo.SelectedIndex - 1, selItem.Content.ToString());
                else
                    MessageBox.Show("无线路，请重新选择");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  数据处理公共方法（供 PointHandle.FindMesEnd 复用）
        // ─────────────────────────────────────────────────────────────────

        /// <summary>将中文速度/顶升/转向/避障字符串映射为数值，生成标准化 DataTable</summary>
        public DataTable ProcessData(DataTable input)
        {
            var speedMap = new Dictionary<string, double>
            {
                {"0.5",0.5},{"-0.5",-0.5},{"0.6",0.6},{"-0.6",-0.6},
                {"0.8",0.8},{"-0.8",-0.8},{"1.0",1.0},{"-1.0",-1.0},
                {"1.2",1.2},{"-1.2",-1.2},{"缺省",0.7}
            };
            var hookMap = new Dictionary<string, int>
            {
                {"移动前下降",12},{"移动前升起",11},{"移动后下降",22},{"移动后升起",21},{"缺省",0}
            };
            var turnMap = new Dictionary<string, double> { { "默认", 1 }, { "正向", 1 }, { "反向", 0 } };
            var obsMap = new Dictionary<string, double> { { "避障", 0 }, { "不避障", 1 }, { "缺省", 0 } };

            var result = new DataTable();
            foreach (var (col, t) in new[] {
                ("Tag",typeof(int)),("X",typeof(double)),("Y",typeof(double)),
                ("Speed",typeof(double)),("Hook",typeof(int)),("AngleSpeed",typeof(double)),
                ("Turn",typeof(double)),("ObsAvoidance",typeof(double)) })
                result.Columns.Add(col, t);

            foreach (DataRow row in input.Rows)
            {
                double angleSpeed;
                string angleInput = row["AngleSpeed"].ToString().Trim();
                if (angleInput == "default") angleSpeed = 0.25;
                else if (!double.TryParse(angleInput, out angleSpeed))
                    throw new ArgumentException($"无效的角速度输入值：'{angleInput}'");

                result.Rows.Add(
                    Convert.ToInt32(row["Tag"]),
                    Convert.ToDouble(row["X"]),
                    Convert.ToDouble(row["Y"]),
                    speedMap[row["Speed"].ToString()],
                    hookMap[row["Hook"].ToString()],
                    angleSpeed,
                    turnMap[row["Turn"].ToString()],
                    obsMap[row["ObsAvoidance"].ToString()]);
            }
            return result;
        }

        /// <summary>将坐标 × 10（m → 像素基准）</summary>
        public DataTable ProcessDataNow(DataTable input)
        {
            DataTable copy = input.Copy();
            foreach (DataRow row in copy.Rows)
            {
                row[1] = Convert.ToDouble(row[1]) * 10;
                row[2] = Convert.ToDouble(row[2]) * 10;
            }
            return copy;
        }

        /// <summary>将 DataTable 行转换为 6-Tuple 列表（供坐标变换使用）</summary>
        public List<Tuple<Point, int, double, double, double, double>> PopulateListFromDataTable(DataTable dt)
        {
            var list = new List<Tuple<Point, int, double, double, double, double>>();
            foreach (DataRow row in dt.Rows)
                list.Add(Tuple.Create(
                    new Point(Convert.ToDouble(row["X"]), Convert.ToDouble(row["Y"])),
                    Convert.ToInt32(row["Hook"]),
                    Convert.ToDouble(row["Speed"]),
                    Convert.ToDouble(row["AngleSpeed"]),
                    Convert.ToDouble(row["Turn"]),
                    Convert.ToDouble(row["ObsAvoidance"])));
            return list;
        }

        // ─────────────────────────────────────────────────────────────────
        //  辅助
        // ─────────────────────────────────────────────────────────────────

        public bool IsFloat(string str)
            => Regex.IsMatch(str.Trim(), @"^(-?\d+)(\.\d+)?$");

        // 空实现占位
        private void ProgramNO_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) { }
        private void ProgramName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) { }
    }
}