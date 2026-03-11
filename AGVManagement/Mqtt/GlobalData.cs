using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AGVManagement.Mqtt
{
    public static class GlobalData
    {
        public static DataTable AgvData { get; private set; } = new DataTable("TabAgvInfo");
        public static DataTable SystemStatusData { get; private set; } = new DataTable("TabSystemStatus"); // ✅ 新增系统状态表
        // 锁对象（私有）
        private static readonly object _lock = new object();
        private static readonly object _statusLock = new object();
        static GlobalData() 
        {
            // 初始化表格
            AgvData.Columns.Add("Agv", typeof(String));
            AgvData.Columns.Add("信息", typeof(String));
            AgvData.Rows.Add(new object[] { "AGV", "" });
            AgvData.Rows.Add(new object[] { "网络状态", "未连接" }); // 默认状态为“未连接”
            AgvData.Rows.Add(new object[] { "运行状态", "" });
            AgvData.Rows.Add(new object[] { "车辆坐标X", "" });
            AgvData.Rows.Add(new object[] { "车辆坐标Y", "" });
            AgvData.Rows.Add(new object[] { "运行准备", "" });
            AgvData.Rows.Add(new object[] { "驱动下降", "" });
            AgvData.Rows.Add(new object[] { "脱轨", "" });
            AgvData.Rows.Add(new object[] { "扫描区域", "" });
            AgvData.Rows.Add(new object[] { "电压", "" }); // 电压信息    
            AgvData.Rows.Add(new object[] { "报警", "" });    
            AgvData.Rows.Add(new object[] { "报警信息", "" });


            // 初始化系统状态表
            SystemStatusData.Columns.Add("项目", typeof(string));
            SystemStatusData.Columns.Add("状态", typeof(string));

            SystemStatusData.Rows.Add("MES连接", "正常");
            SystemStatusData.Rows.Add("系统运行状态", "正常");
            SystemStatusData.Rows.Add("数据库连接", "正常");
            SystemStatusData.Rows.Add("AGV通讯", "正常");
        }

        /// <summary>
        /// 更新 AGV 信息（线程安全）
        /// </summary>
        public static void UpdateAgvInfo(string key, string value)
        {
            lock (_lock) // 确保同一时间只有一个线程访问 DataTable
            {
                try
                {   
                    if (!AgvData.Columns.Contains("Agv") || !AgvData.Columns.Contains("信息"))
                        return; // 防御：列被意外清空

                    foreach (DataRow row in AgvData.Rows)
                    {
                        if (row["Agv"].ToString() == key)
                        {
                            row["信息"] = value;
                            return;
                        }
                    }

                    // 如果没找到对应的 key，可以选择自动新增行（避免报错）
                    AgvData.Rows.Add(new object[] { key, value });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"连接已断开：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        /// <summary>
        /// 更新系统状态（MES连接、系统运行等）
        /// </summary>
        public static void UpdateSystemStatus(string item, string status)
        {
            lock (_statusLock)
            {
                foreach (DataRow row in SystemStatusData.Rows)
                {
                    if (row["项目"].ToString() == item)
                    {
                        row["状态"] = status;
                        return;
                    }
                }

                // 如果没有该项目则新增一行
                SystemStatusData.Rows.Add(item, status);
            }
        }

        /// <summary>
        /// 获取当前系统状态
        /// </summary>
        public static string GetSystemStatus(string item)
        {
            lock (_statusLock)
            {
                foreach (DataRow row in SystemStatusData.Rows)
                {
                    if (row["项目"].ToString() == item)
                        return row["状态"].ToString();
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 读取 AGV 信息（线程安全）
        /// </summary>
        public static string GetAgvInfo(string key)
        {
            lock (_lock)
            {
                foreach (DataRow row in AgvData.Rows)
                {
                    if (row["Agv"].ToString() == key)
                        return row["信息"].ToString();
                }
                return string.Empty;
            }
        }
    }
}
