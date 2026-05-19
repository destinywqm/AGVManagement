// ============================================================
//  MapInfo.xaml.cs（重构后）
//  职责：地图列表与管理窗口（查询、预览、新建、编辑、删除、导入、导出）
//  变更说明：
//    - 合并了 3 个 MapMessageBLL / MapManag 重复实例为单一字段
//    - MapShow() 抽取为独立方法，不再在事件中内联
//    - DeleteMapNow / Sql 列表（死代码）已删除
//    - operateDB 提升为字段
// ============================================================
using AGV.BLL;
using AGVManagement.instrument;
using AGVManagement.MapPaint;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AGVManagement
{
    public partial class Map : Window
    {
        // ── 字段 ──────────────────────────────────────────────────────────
        private readonly MapMessageBLL _mapBLL = new MapMessageBLL();
        private readonly MapManag _mapManag = new MapManag();
        private readonly OperateDBBLL _dbBLL = new OperateDBBLL();

        private long _mapTime; // 当前选中地图的时间戳（CreateTime）
        private string _mapName; // 当前选中地图名称

        // ── 构造 ──────────────────────────────────────────────────────────
        public Map()
        {
            InitializeComponent();
            LoadMapList(null);
        }

        // ─────────────────────────────────────────────────────────────────
        //  地图列表
        // ─────────────────────────────────────────────────────────────────

        /// <summary>后台加载地图列表，可按名称过滤</summary>
        private void LoadMapList(string filterName)
        {
            // 清空地图工具状态（防止上次编辑残留）
            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();

            var t = new Thread(() =>
            {
                DataTable raw = _mapBLL.GetMapData(filterName);
                var dt = new DataTable("Map");
                dt.Columns.Add("MapName");
                dt.Columns.Add("MapInfo");

                if (raw != null)
                    foreach (DataRow row in raw.Rows)
                        dt.Rows.Add(
                            row["Name"].ToString(),
                            $"{UTC.ConvertLongDateTime(long.Parse(row["CreateTime"].ToString()))}" +
                            $",{row["Width"]},{row["Height"]}");

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MapData.ItemsSource = dt.DefaultView;
                    MapData.AutoGenerateColumns = false;
                    if (dt.Rows.Count > 0)
                    {
                        MapData.SelectedIndex = 0;
                        ShowSelectedMap();
                    }
                }));
            })
            { IsBackground = true };
            t.Start();
        }

        // ─────────────────────────────────────────────────────────────────
        //  地图预览
        // ─────────────────────────────────────────────────────────────────

        /// <summary>读取当前选中行，渲染地图到 Canvas</summary>
        private void ShowSelectedMap()
        {
            if (MapData.SelectedItems.Count == 0) return;

            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();
            Painting.siseWin = 1;
            MapIN.Children.Clear();

            string[] parts = ((DataRowView)MapData.SelectedValue).Row.ItemArray[1]
                .ToString().Split(',');
            if (parts.Length != 3) return;

            string timeStr = parts[0];
            _mapName = ((DataRowView)MapData.SelectedValue).Row.ItemArray[0].ToString();
            MapName.Content = $"地图区域信息（{_mapName}）";

            MapIN.Width = double.Parse(parts[1]) * _mapManag.Sise;
            MapIN.Height = double.Parse(parts[2]) * _mapManag.Sise;

            _mapTime = long.Parse(UTC.ConvertDateTimeLong(Convert.ToDateTime(timeStr)).ToString());
            _mapManag.SelectMap(_mapTime, MapIN, false);
        }

        // ─────────────────────────────────────────────────────────────────
        //  事件处理
        // ─────────────────────────────────────────────────────────────────

        private void SelectMap_Click(object sender, RoutedEventArgs e)
            => LoadMapList(SelectMap.Text.Trim());

        private void MapData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released && MapData.SelectedItems.Count > 0)
                ShowSelectedMap();
        }

        private void AddMap_Click(object sender, RoutedEventArgs e)
        {
            Close();
            new AddMap().ShowDialog();
        }

        private void CompileMap_Click(object sender, RoutedEventArgs e)
        {
            if (MapData.Items.Count == 0) { MessageBox.Show("暂无地图"); return; }

            var mapWin = new MainWindow(_mapTime, _mapName, null, 0, 0, 0, 0, 0);

            string imgPath = Path.Combine("Images", $"{_mapName}.png");
            if (File.Exists(imgPath))
                mapWin.SetBackgroundImage(imgPath);
            else
                MessageBox.Show($"未找到与地图名 {_mapName} 对应的背景图片，使用默认背景。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

            mapWin.Show();
        }

        private void Button_Click(object sender, RoutedEventArgs e) // 删除地图
        {
            if (MessageBox.Show("确认要删除地图吗？", "提示",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            bool ok = _mapBLL.DelMapAndRelatedTablesMap(_mapTime);
            MessageBox.Show(ok ? "删除成功" : "删除失败");
            if (ok) SelectMap_Click(null, null);
        }

        private void Channel_Click(object sender, RoutedEventArgs e) // 导入
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "地图信息文件|*.tll|所有文件|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            bool ok = _mapBLL.MapToleadNumber(File.ReadAllText(dlg.FileName));
            MessageBox.Show(ok ? "导入成功！" : "导入失败！");
            if (ok) SelectMap_Click(null, null);
        }

        private void Derive_Click(object sender, RoutedEventArgs e) // 导出
        {
            if (MapData.Items.Count == 0) { MessageBox.Show("暂无地图"); return; }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "地图信息文件|*.tll",
                FileName = $"({_mapName}){DateTime.Now:yyyyMMdd}"
            };
            if (dlg.ShowDialog() != true) return;

            string sql =
                _dbBLL.ExportSettings(_mapTime, "agv")
                + _dbBLL.ExportMySqlTables($"tag{_mapTime}", "agv")
                + _dbBLL.ExportMySqlTables($"line{_mapTime}", "agv")
                + _dbBLL.ExportMySqlTables($"device{_mapTime}", "agv")
                + _dbBLL.ExportMySqlTables($"widget{_mapTime}", "agv")
                + _dbBLL.ExportMySqlTables($"route{_mapTime}", "agv")
                + _dbBLL.ExportTableContents("map", "agv", _mapTime.ToString());

            File.WriteAllText(dlg.FileName, sql);
            MessageBox.Show("导出成功!");
        }
    }
}