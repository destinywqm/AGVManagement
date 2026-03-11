using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using AGV.BLL;
using AGVManagement.Enumeration;
using AGVManagement.instrument;
using AGVManagement.MapPaint;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Xml.Linq;

namespace AGVManagement
{
    /// <summary>
    /// Map.xaml 的交互逻辑
    /// </summary>
    public partial class Map : Window
    {
        MapMessageBLL GesMap = new MapMessageBLL();
        MapManag MapManag = new MapManag();
        DeleteMap DeleteMap = new DeleteMap();
        private MapMessageBLL messageBLL = new MapMessageBLL();
        private long MapTime;
        private string MapNa;
        public Map()
        {
            InitializeComponent();
            MapDataBinding(null);

        }


        /// <summary>
        /// 查询所有地图
        /// </summary>
        public void MapDataBinding(string MpName)
        {
            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();

            Thread thread = new Thread(() =>
            {
                DataTable dt = new DataTable("Map");
                dt.Columns.Add(new DataColumn("MapName"));
                dt.Columns.Add(new DataColumn("MapInfo"));
                DataTable ga = GesMap.GetMapData(MpName);
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ga != null)
                    {
                        foreach (DataRow item in ga.Rows)
                        {
                            dt.Rows.Add(new object[] { item["Name"].ToString(), (UTC.ConvertLongDateTime(long.Parse(item["CreateTime"].ToString())).ToString() + "," + item["Width"].ToString() + "," + item["Height"].ToString()) });
                        }

                        MapData.ItemsSource = dt.DefaultView;
                        MapData.AutoGenerateColumns = false;
                        MapData.SelectedIndex = 0;
                        if (ga.Rows.Count > 0)
                        {
                            MapShow();
                        }
                    }
                }));
            });
            thread.IsBackground = true;
            thread.Start();
        }


        /// <summary>
        /// 行点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (MapData.SelectedItems.Count > 0)
                {
                    MapShow();
                }
            }
        }


        /// <summary>
        /// 显示具体地图信息
        /// </summary>
        private void MapShow()
        {
            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();
            Painting.siseWin = 1;
            MapIN.Children.Clear();
            string[] arr = ((DataRowView)MapData.SelectedValue).Row.ItemArray[1].ToString().Split(',');
            if (arr.Count().Equals(3))
            {
                string Times = arr[0];
                string MpName = ((DataRowView)MapData.SelectedValue).Row.ItemArray[0].ToString();
                MapName.Content = "地图区域信息（" + MpName + "）";
                MapNa = MpName;
                MapIN.Width = double.Parse(arr[1]) * MapManag.Sise;
                MapIN.Height = double.Parse(arr[2]) * MapManag.Sise;
                MapTime = long.Parse(UTC.ConvertDateTimeLong(Convert.ToDateTime(Times)).ToString());
                MapManag.SelectMap(UTC.ConvertDateTimeLong(Convert.ToDateTime(Times)), MapIN,false);
            }
        }

        /// <summary>
        /// 搜索地图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectMap_Click(object sender, RoutedEventArgs e)
        {
            MapDataBinding(SelectMap.Text.Trim());
        }

        /// <summary>
        /// 编辑地图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompileMap_Click(object sender, RoutedEventArgs e)
        {
            if (MapData.Items.Count == 0)
            {
                MessageBox.Show("暂无地图");
                return;
            }

            // 构造图片文件路径，假设所有背景图片存储在 "Images/" 文件夹下
            string backgroundImagePath = System.IO.Path.Combine("Images", $"{MapNa}.png");

            // 检查图片文件是否存在
            if (!System.IO.File.Exists(backgroundImagePath))
            {
                MessageBox.Show($"未找到与地图名 {MapNa} 对应的背景图片，使用默认背景。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            MainWindow mapRedact = new MainWindow(MapTime, MapNa, null, 0, 0, 0, 0, 0);

            // 调用方法将背景设置为找到的图片（如果图片存在）
            if (System.IO.File.Exists(backgroundImagePath))
            {
                mapRedact.SetBackgroundImage(backgroundImagePath);
            }
            mapRedact.Show();


        }

        private void AddMap_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            AddMap but = new AddMap();
            but.ShowDialog();

        }

        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Channel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "地图信息文件|*.tll|所有文件|*.*";
            if (dlg.ShowDialog() == true)
            {
                string sqlText = File.ReadAllText(dlg.FileName);
                if (GesMap.MapToleadNumber(sqlText) == true)
                {
                    MessageBox.Show("导入成功！");
                    SelectMap_Click(null,null);
                }
                else
                {
                    MessageBox.Show("导入失败！");
                }
            }
        }
        OperateDBBLL operateDB = new OperateDBBLL();
        /// <summary>
        /// 导出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Derive_Click(object sender, RoutedEventArgs e)
        {
            if (MapData.Items.Count == 0)
            {
                MessageBox.Show("暂无地图");
                return;
            }
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Filter = "地图信息文件|*.tll";
            sfd.FileName = "(" + MapNa + ")" + DateTime.Now.ToString("yyyyMMdd");
            if (sfd.ShowDialog() == true)
            {
                string sql = operateDB.ExportSettings(MapTime, "agv") + operateDB.ExportMySqlTables("tag" + MapTime, "agv") + operateDB.ExportMySqlTables("line" + MapTime, "agv") + operateDB.ExportMySqlTables("device" + MapTime, "agv") + operateDB.ExportMySqlTables("widget" + MapTime, "agv") + operateDB.ExportMySqlTables("route" + MapTime, "agv");
                sql = sql + operateDB.ExportTableContents("map", "agv", MapTime.ToString());
                File.WriteAllText(sfd.FileName, sql);
                MessageBox.Show("导出成功!");
            }
        }

        List<string> Sql = new List<string>();

        public void DeleteMapNow(string MapName, string MapTime)
        {
            Sql.Add(string.Format("DELETE agv.`map` SET `Name` = '{0}' WHERE CreateTime = {1}", MapName, MapTime));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //DeleteMapNow(MapName.ToString(), MapTime.ToString());
            //DeleteMap.deletetlas(Sql);
            MessageBoxResult confirmToDel = MessageBox.Show("确认要删除地图吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmToDel == MessageBoxResult.Yes)
            {
                if (messageBLL.DelMapAndRelatedTablesMap(MapTime))
                {
                    MessageBox.Show("删除成功");
                    SelectMap_Click(null, null);
                }
                else
                {
                    MessageBox.Show("删除失败");
                }
            }
        }
    }
}
