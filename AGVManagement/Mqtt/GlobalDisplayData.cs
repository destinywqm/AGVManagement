using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AGVManagement.Mqtt
{
    public static class GlobalDisplayData
    {
        public static DataTable AgvData { get; private set; } = new DataTable("TabAgvInfoDisplay");

        private static readonly object _lock = new object();

        static GlobalDisplayData()
        {
            AgvData.Columns.Add("AGV", typeof(string));
            AgvData.Columns.Add("网络状态", typeof(string));
            AgvData.Columns.Add("运行状态", typeof(string));
            AgvData.Columns.Add("车辆坐标X", typeof(string));
            AgvData.Columns.Add("车辆坐标Y", typeof(string));
            AgvData.Columns.Add("电压", typeof(string));

            AgvData.Columns.Add("起点", typeof(string));
            AgvData.Columns.Add("终点", typeof(string));
            AgvData.Columns.Add("报警", typeof(string));
        }

        /// <summary>
        /// 更新显示数据（线程安全），如果不存在该AGV则自动添加
        /// </summary>
        public static void UpdateDisplayInfo(string agvAddress, string column, string value)
        {
            lock (_lock)
            {
                try
                {
                    if (!AgvData.Columns.Contains(column)) return;

                    var rows = AgvData.Select($"AGV = '{agvAddress}'");
                    if (rows.Length == 0)
                    {
                        // 新建一行
                        DataRow newRow = AgvData.NewRow();
                        newRow["AGV"] = agvAddress;
                        newRow["网络状态"] = "未连接";
                        newRow[column] = value;
                        AgvData.Rows.Add(newRow);
                    }
                    else
                    {
                        rows[0][column] = value;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"更新显示数据异常：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 初始化绑定 DataGrid
        /// </summary>
        public static void BindToDataGrid(System.Windows.Controls.DataGrid dataGrid)
        {
            dataGrid.ItemsSource = AgvData.DefaultView;
        }

        public static void SetAgvConnected(string address)
        {
            UpdateDisplayInfo(address, "网络状态", "已连接");
        }

        public static void SetAgvDisconnected(string address)
        {
            UpdateDisplayInfo(address, "网络状态", "未连接");
        }
    }
}
