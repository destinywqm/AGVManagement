using System.Data;

namespace AGVManagement
{
    internal static class AgvDataViewResetter
    {
        public static void Reset(DataTable agvData)
        {
            lock (agvData)
            {
                agvData.Clear();
                agvData.Rows.Add(new object[] { "AGV", "" });
                agvData.Rows.Add(new object[] { "网络状态", "未连接" });
                agvData.Rows.Add(new object[] { "运行状态", "" });
                agvData.Rows.Add(new object[] { "车辆坐标X", "" });
                agvData.Rows.Add(new object[] { "车辆坐标Y", "" });
                agvData.Rows.Add(new object[] { "运行准备", "" });
                agvData.Rows.Add(new object[] { "驱动下降", "" });
                agvData.Rows.Add(new object[] { "脱轨", "" });
                agvData.Rows.Add(new object[] { "扫描区域", "" });
                agvData.Rows.Add(new object[] { "电压", "" });
                agvData.Rows.Add(new object[] { "报警", "" });
                agvData.Rows.Add(new object[] { "报警信息", "" });
            }
        }
    }
}
