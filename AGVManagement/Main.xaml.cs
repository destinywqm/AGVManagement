// ============================================================
//  Main.xaml.cs（重构后）
//  职责：主窗口 UI 事件响应
// ============================================================
using AGV.BLL;
using AGV.Models.Models;
using AGVDLL;
using AGVManagement.instrument;
using AGVManagement.MapPaint;
using AGVManagement.Mqtt;
using AGVManagement.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AGVManagement
{
    public partial class Main : Window
    {
        // ── 字段 ──────────────────────────────────────────────────────────
        private readonly OperateDBBLL _dBBLL = new OperateDBBLL();
        private readonly MapMessageBLL _mapMessage = new MapMessageBLL();
        private readonly MapManag _manag = new MapManag();
        private readonly PortService _portService = new PortService();

        private bool _mapLoaded = false;
        private string _selAgv = "1";

        // ── 构造 ──────────────────────────────────────────────────────────
        public Main()
        {
            InitializeComponent();

            MqttConnectionManager.ClientsChanged += RefreshAgvDropdown;

            BindAgvDataGrids();
            LoadMapAsync();
            GlobalDisplayData.BindToDataGrid(TabAgvInfo);

            _portService.PollTick += OnPortPollTick;

            // ── 给 TabAgvInfo 挂右键菜单 ──────────────────────────────────
            BuildTabAgvInfoContextMenu();
        }

        // ─────────────────────────────────────────────────────────────────
        //  初始化
        // ─────────────────────────────────────────────────────────────────

        private void BindAgvDataGrids()
        {
            TabAgvData.ItemsSource = GlobalData.AgvData.DefaultView;
            TabSystemStatusData.ItemsSource = GlobalData.SystemStatusData.DefaultView;

            TabAgvData.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
            TabAgvData.HeadersVisibility = DataGridHeadersVisibility.None;
            TabSystemStatusData.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
            TabSystemStatusData.HeadersVisibility = DataGridHeadersVisibility.None;
        }

        private void LoadMapAsync()
        {
            var t = new Thread(() => { _dBBLL.CreateDBMap(); FillMapComboBox(); })
            { IsBackground = true };
            t.Start();
        }

        private void FillMapComboBox()
        {
            var bll = new MapMessageBLL();
            DataTable maps = bll.GetMapData(null);
            string saved = _mapMessage.SettingInfoMap();

            Dispatcher.Invoke(() =>
            {
                Maplistq.Items.Add(new ComboBoxItem { Content = "请选择" });
                if (maps == null) { Maplistq.SelectedIndex = 0; return; }

                int target = 0, idx = 0;
                foreach (DataRow row in maps.Rows)
                {
                    if (saved != null && saved == row["CreateTime"].ToString()) target = idx;
                    Maplistq.Items.Add(new ComboBoxItem
                    {
                        Content = row["Name"].ToString(),
                        Tag = $"{row["Width"]},{row["Height"]},{row["CreateTime"]}"
                    });
                    idx++;
                }
                _mapLoaded = true;
                Maplistq.SelectedIndex = target + 1;
            });
        }

        // ─────────────────────────────────────────────────────────────────
        //  菜单事件
        // ─────────────────────────────────────────────────────────────────

        private void Map_Add_Click(object s, RoutedEventArgs e) => new AddMap().ShowDialog();
        private void Map_btn_Click(object s, RoutedEventArgs e) => new Map().Show();
        private void Circui_Click(object s, RoutedEventArgs e) => new Circuitredact().Show();
        private void BeaconCR_Click(object s, RoutedEventArgs e) => new BeaconRedact().ShowDialog();
        private void OpenMap_Click(object s, RoutedEventArgs e) => new Operation().ShowDialog();
        private void CarStation_Click(object s, RoutedEventArgs e) => new CarListWindow().ShowDialog();
        private void PortDern_Click(object s, RoutedEventArgs e) => new PortSetting(this).ShowDialog();

        private void Close_Click(object s, RoutedEventArgs e)
        {
            _portService.Clear();
            Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) => e.Cancel = true;

        private void Btnset_Click(object s, RoutedEventArgs e)
        {
            try
            {
                string output = CppCBSLib.GetMultiAgentPaths(File.ReadAllText("env1_sgt.json"));
                MessageBox.Show(output, "DLL 输出结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调用 DLL 出错：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  地图下拉框选择
        // ─────────────────────────────────────────────────────────────────

        public void Maplist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_mapLoaded) return;
            var item = Maplistq.SelectedItem as ComboBoxItem;
            if (item?.Tag == null || item.Content.Equals("请选择")) return;

            string[] parts = item.Tag.ToString().Split(',');
            long mapTime = long.Parse(parts[2]);

            MapIN.Children.Clear();
            _manag.Sise = 16;
            MapIN.Width = Convert.ToDouble(parts[0]) * _manag.Sise;
            MapIN.Height = Convert.ToDouble(parts[1]) * _manag.Sise;
            Open.IsEnabled = false;

            LoadSerialPortInfo(mapTime);
            _manag.SelectMapLOad(mapTime, MapIN);
            LoadAgvMoveTable(mapTime);
        }

        // ─────────────────────────────────────────────────────────────────
        //  串口信息加载
        // ─────────────────────────────────────────────────────────────────

        private void LoadSerialPortInfo(long mapTime)
        {
            PortInfo.AGVCom.Clear(); PortInfo.Baud.Clear(); PortInfo.agv.Clear();
            PortInfo.buttonCom.Clear(); PortInfo.buttonBaud.Clear(); PortInfo.buttonStr.Clear();
            PortInfo.chargeCom.Clear(); PortInfo.chargeBaud.Clear(); PortInfo.chargeStr.Clear();

            DataTable dt = _mapMessage.LoadDeviceMap(mapTime);
            var data = new DataTable();
            data.Columns.Add("串口"); data.Columns.Add("信息");

            if (dt.Rows.Count == 0)
            {
                foreach (var label in new[] { "COM", "波特率", "AGV / 其他", "状态" })
                    data.Rows.Add(label, "");
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    int com = Convert.ToInt32(row["Com"]);
                    int baud = Convert.ToInt32(row["Baud"]);
                    string type = row["Agv"].ToString();

                    data.Rows.Add("COM", com);
                    data.Rows.Add("波特率", baud);

                    switch (type)
                    {
                        case "Button":
                            data.Rows.Add("AGV / 其他", "按钮");
                            PortInfo.buttonCom.Add(com); PortInfo.buttonBaud.Add(baud); PortInfo.buttonStr.Add("Button");
                            break;
                        case "Charge":
                            data.Rows.Add("AGV / 其他", "充电机");
                            PortInfo.chargeCom.Add(com); PortInfo.chargeBaud.Add(baud); PortInfo.chargeStr.Add("Charge");
                            break;
                        default:
                            data.Rows.Add("AGV / 其他", type);
                            PortInfo.AGVCom.Add(com); PortInfo.Baud.Add(baud); PortInfo.agv.Add(type);
                            break;
                    }
                    data.Rows.Add("状态", "关闭");
                }
            }

            TabSerialPortData.ItemsSource = data.DefaultView;
            TabSerialPortData.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
            TabSerialPortData.HeadersVisibility = DataGridHeadersVisibility.None;
        }

        // ─────────────────────────────────────────────────────────────────
        //  AGV 运行信息表
        // ─────────────────────────────────────────────────────────────────

        public void LoadAgvMoveTable(long mapTime)
        {
            List<string> agvList = _dBBLL.AgvNumListMap(mapTime);

            var dt = new DataTable();
            foreach (var col in new[] { "type", "TagName", "Speed", "turn", "Dir", "Hook", "Rfid", "Program", "Step" })
                dt.Columns.Add(col);

            MainInfo.agvNo.Clear();
            foreach (string agv in agvList)
            {
                dt.Rows.Add("离线", agv, "", "", "", "", "", "", "");
                MainInfo.agvNo.Add(agv);
            }
            if (agvList.Count > 0) _selAgv = agvList[0];

            TabAgvMoveData.DataContext = dt.DefaultView;
            TabAgvMoveData.AutoGenerateColumns = false;
            Open.IsEnabled = true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  串口打开/关闭按钮
        // ─────────────────────────────────────────────────────────────────

        private void Btnset_OpenPort_Click(object sender, RoutedEventArgs e)
        {
            if (_portService.OpenPort())
            {
                Btnswitch.IsEnabled = true;
                Open.IsEnabled = false;
                Maplistq.IsEnabled = false;
                Menu.IsEnabled = false;
                SwitchText.Content = "串口状态：开";
                MessageBox.Show("打开串口成功！");
            }
        }

        private void Btnswitch_Click(object sender, RoutedEventArgs e)
        {
            _portService.ClosePort();
            RefreshPortStatusGrid();

            Btnswitch.IsEnabled = false;
            Open.IsEnabled = true;
            Maplistq.IsEnabled = true;
            Menu.IsEnabled = true;
            SwitchText.Content = "串口状态：关";

            ResetAgvMoveGrid();
            ResetAgvDetailPanel();
            MessageBox.Show("关闭串口成功！");
        }

        // ─────────────────────────────────────────────────────────────────
        //  串口轮询回调
        // ─────────────────────────────────────────────────────────────────

        private void OnPortPollTick()
        {
            Dispatcher.Invoke(() =>
            {
                RefreshPortStatusGrid();
                RefreshAgvMoveGrid();
            });
        }

        private void RefreshPortStatusGrid()
        {
            int idx = 0;
            void Set(int i, object v) => ((DataRowView)TabSerialPortData.Items[i])[1] = v;

            for (int i = 0; i < PortInfo.AGVCom.Count; i++, idx += 4)
            {
                Set(idx, PortInfo.AGVCom[i]);
                Set(idx + 1, PortInfo.Baud[i]);
                Set(idx + 2, PortInfo.agv[i]);
                Set(idx + 3, MainInfo.listPtr[i].ToInt32() == 0 ? "关闭" : "打开");
            }
            for (int i = 0; i < PortInfo.buttonCom.Count; i++, idx += 4)
            {
                Set(idx, PortInfo.buttonCom[i]); Set(idx + 1, PortInfo.buttonBaud[i]);
                Set(idx + 2, PortInfo.buttonStr[i]); Set(idx + 3, "关闭");
                SetCellColor(TabSerialPortData, idx + 3, Brushes.Red);
            }
            for (int i = 0; i < PortInfo.chargeCom.Count; i++, idx += 4)
            {
                Set(idx, PortInfo.chargeCom[i]); Set(idx + 1, PortInfo.chargeBaud[i]);
                Set(idx + 2, PortInfo.chargeStr[i]); Set(idx + 3, "关闭");
                SetCellColor(TabSerialPortData, idx + 3, Brushes.Red);
            }
        }

        private void RefreshAgvMoveGrid()
        {
            for (int i = 0; i < TabAgvMoveData.Items.Count; i++)
            {
                if (MainInfo.carStatusList.Count == 0) break;
                int agvNum = Convert.ToInt32(((DataRowView)TabAgvMoveData.Items[i])[1]);
                CarStatus car = PortService.FindCarStatus(agvNum);
                if (car == null) continue;

                UpdateAgvMoveRow(i, car);
                if (Convert.ToInt32(_selAgv) == agvNum)
                    UpdateAgvDetailPanel(car);
            }
        }

        private void UpdateAgvMoveRow(int rowIdx, CarStatus car)
        {
            var row = (DataRowView)TabAgvMoveData.Items[rowIdx];

            if (car.errorCode == 0 && car.carNum == 0)
            { row[0] = "连接中"; SetCheckBox(rowIdx, "checkbox has-error", Brushes.Red); }
            else if (car.errorCode == 205)
            { row[0] = "离线"; SetCheckBox(rowIdx, "checkbox has-error", Brushes.Red); }
            else
            {
                row[0] = "在线"; SetCheckBox(rowIdx, "checkbox has-success", Brushes.Green);
                row[2] = TagCompile.agvSpeed[car.speedNo] + "米/分钟";
                row[3] = car.agvRunRight ? "右转中" : car.agvRunLeft ? "左转中" : "直行";
                row[4] = car.agvRunDirection ? "正向" : "反向";
                row[5] = car.agvHookUP ? "上升" : "下降";
                row[6] = string.IsNullOrEmpty(car.rfidStatus) ? "无" : car.rfidStatus;
                row[7] = car.programNo;
                row[8] = car.stepNo;
            }
        }

        private void UpdateAgvDetailPanel(CarStatus car)
        {
            void Val(int r, object v) => ((DataRowView)TabAgvData.Items[r])[1] = v;
            void Col(int r, Brush b) => SetCellColor(TabAgvData, r, b);
            void Status(int r, bool ok, string y, string n) { Val(r, ok ? y : n); Col(r, ok ? Brushes.Green : Brushes.Red); }

            if (car.errorCode == 0 && car.carNum == 0)
            { Val(0, _selAgv); Val(1, "连接中"); Col(1, Brushes.Green); for (int r = 2; r <= 9; r++) Val(r, ""); }
            else if (car.errorCode == 205)
            { Val(0, _selAgv); Val(1, "离线！！！"); Col(1, Brushes.Red); for (int r = 2; r <= 9; r++) Val(r, ""); }
            else
            {
                Val(0, car.carNum); Val(1, "在线"); Col(1, Brushes.Green);
                Status(2, car.IsRunning, "行进中", "停止");
                Status(3, car.agvRunReady, "On", "Off");
                Status(4, car.agvDriverDown, "驱动下降", "驱动上升");
                Status(5, car.agvLineRead, "正常", "脱轨");
                Val(6, car.pbsArea);
                Val(7, car.powerCurrentF + "V");
                Status(8, !car.errorSwitch, "正常", "报警！！！");
                Val(9, Error.errorStr(car.errorCode));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  重置网格
        // ─────────────────────────────────────────────────────────────────

        private void ResetAgvMoveGrid()
        {
            for (int i = 0; i < TabAgvMoveData.Items.Count; i++)
            {
                ((DataRowView)TabAgvMoveData.Items[i])[0] = "离线";
                for (int s = 2; s < TabAgvMoveData.Columns.Count; s++)
                    ((DataRowView)TabAgvMoveData.Items[i])[s] = "";
                SetCheckBox(i, "checkbox has-error", Brushes.Red);
            }
        }

        private void ResetAgvDetailPanel()
        {
            for (int r = 0; r <= 9; r++)
                ((DataRowView)TabAgvData.Items[r])[1] = "";
        }

        // ─────────────────────────────────────────────────────────────────
        //  MQTT / AGV 下拉框
        // ─────────────────────────────────────────────────────────────────

        private void RefreshAgvDropdown()
        {
            AgvSelectComboBox.ItemsSource = null;
            AgvSelectComboBox.ItemsSource = MqttConnectionManager.MqttClients.Keys.ToList();
            if (!string.IsNullOrEmpty(MqttConnectionManager.CurrentAddress))
                AgvSelectComboBox.SelectedItem = MqttConnectionManager.CurrentAddress;
        }

        private void AgvSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AgvSelectComboBox.SelectedItem is string addr)
            {
                MqttConnectionManager.SetCurrent(addr);
                GlobalData.UpdateAgvInfo("AGV", addr);
                GlobalDisplayData.UpdateDisplayInfo(addr, "AGV", addr);
            }
        }

        private void TabAgvMoveData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released && TabAgvMoveData.SelectedItems.Count > 0)
                _selAgv = ((DataRowView)TabAgvMoveData.SelectedItem)[1].ToString();
        }

        // ─────────────────────────────────────────────────────────────────
        //  【新增】TabAgvInfo 右键菜单：连接 / 断开
        //  替换原来的单击重连逻辑
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 动态构建右键菜单并挂到 TabAgvInfo。
        /// 在构造函数中调用一次即可。
        /// </summary>
        private void BuildTabAgvInfoContextMenu()
        {
            var menu = new ContextMenu();

            var connectItem = new MenuItem { Header = "连接" };
            connectItem.Click += TabAgvInfo_ContextMenu_Connect_Click;

            var disconnectItem = new MenuItem { Header = "断开连接" };
            disconnectItem.Click += TabAgvInfo_ContextMenu_Disconnect_Click;

            menu.Items.Add(connectItem);
            menu.Items.Add(disconnectItem);

            TabAgvInfo.ContextMenu = menu;

            // 右键打开菜单前，先确认有行被选中；没有选中行则阻止弹出
            TabAgvInfo.ContextMenuOpening += (s, e) =>
            {
                if (TabAgvInfo.SelectedItem == null)
                    e.Handled = true;
            };
        }

        /// <summary>右键 → 连接：对选中行的 AGV 重新建立 MQTT 连接</summary>
        private async void TabAgvInfo_ContextMenu_Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!(TabAgvInfo.SelectedItem is DataRowView row)) return;

            string address = row["AGV"].ToString();
            if (string.IsNullOrWhiteSpace(address)) return;

            // 已连接则提示，不重复操作
            if (MqttConnectionManager.MqttClients.TryGetValue(address, out var existing)
                && existing.IsConnected)
            {
                MessageBox.Show($"{address} 已处于连接状态。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 找到当前打开的地图窗口（需要 mainPanel）
                var mapWin = Application.Current.Windows.OfType<MainWindow>()
                             .FirstOrDefault(w => w.IsVisible);
                if (mapWin == null)
                {
                    MessageBox.Show("请先打开地图页面。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 断开旧连接并重建新 wrapper，保证 LatestClient 指向有效对象
                if (MqttConnectionManager.MqttClients.TryGetValue(address, out var old))
                    await old.DisconnectAsync();

                var wrapper = new MqttClientWrapper(
                    mapWin.Dispatcher, address,
                    MqttConnectionManager.GetStoredLength(address),   // 见下方说明
                    MqttConnectionManager.GetStoredWidth(address));

                await wrapper.InitializeAsync(address, 1883, mapWin.mainPanel);

                // 更新连接池 + 设为当前，保证 LatestClient 可用
                MqttConnectionManager.AddClient(address, wrapper);
                MqttConnectionManager.SetCurrent(address);
                GlobalData.UpdateAgvInfo("AGV", address);
                GlobalDisplayData.UpdateDisplayInfo(address, "AGV", address);

                // 连接成功后锁定地图编辑区
                mapWin.LockEditing();

                MessageBox.Show($"{address} 连接成功。", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>右键 → 断开连接：断开选中行 AGV，断开后若全部断开则地图编辑解锁</summary>
        private async void TabAgvInfo_ContextMenu_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (!(TabAgvInfo.SelectedItem is DataRowView row)) return;

            string address = row["AGV"].ToString();
            if (string.IsNullOrWhiteSpace(address)) return;

            if (!MqttConnectionManager.MqttClients.TryGetValue(address, out var client)
                || !client.IsConnected)
            {
                MessageBox.Show($"{address} 当前未连接。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"确认断开 {address} 的连接？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                // 调用 Manager 统一断开（内部会触发 AllClientsDisconnected 事件）
                await MqttConnectionManager.DisconnectClientAsync(address);

                MessageBox.Show($"{address} 已断开。", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"断开失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  UI 辅助
        // ─────────────────────────────────────────────────────────────────

        private void SetCellColor(DataGrid grid, int rowIdx, Brush brush)
        {
            Dispatcher.Invoke(() =>
            {
                var cell = grid.Columns.Count > 1
                    ? grid.Columns[1].GetCellContent(grid.Items[rowIdx]) as TextBlock
                    : null;
                if (cell != null) cell.Foreground = brush;
            });
        }

        private void SetCheckBox(int rowIdx, string styleName, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                var col = TabAgvMoveData.Columns[0] as DataGridTemplateColumn;
                var elem = col?.GetCellContent(TabAgvMoveData.Items[rowIdx]);
                if (elem == null) return;
                var ck = col.CellTemplate.FindName("CheckBoxDN", elem) as CheckBox;
                if (ck == null) return;
                ck.Foreground = color;
                ck.Style = (Style)FindResource(styleName);
            });
        }

        // ─────────────────────────────────────────────────────────────────
        //  空实现（占位）
        // ─────────────────────────────────────────────────────────────────
        private void TabAgvData_SelectionChanged(object s, SelectionChangedEventArgs e) { }
        private void TabAgvMoveData_SelectionChanged(object s, SelectionChangedEventArgs e) { }
        private void MenuItem_Click(object s, RoutedEventArgs e) { }

        // 原来的单击重连处理——保留方法名避免 XAML 编译错误，但逻辑已移至右键菜单
        // 如果 XAML 里 TabAgvInfo 仍绑定了 MouseLeftButtonUp="TabAgvInfo_MouseLeftButtonUp"，
        // 保留此空实现即可；也可直接删除 XAML 中该绑定。
        private void TabAgvInfo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }
    }
}