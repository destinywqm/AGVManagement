using AGV.BLL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AGVManagement.MapPaint
{
    public class LineMap
    {
        MapInstrument map = new MapInstrument();
        public string[] GetTags = null;

        /// <summary>
        /// 查找关联Tag
        /// externalValuePairs / externalWirePointArrays 不为 null 时使用外部局部集合（Circuitredact用），
        /// 为 null 时使用全局静态集合（主窗口编辑用）
        /// </summary>
        public void TagClick(long Time, int TagNu, DataGrid grid, DataTable dt, bool type,
            Dictionary<int, Label> externalValuePairs = null,
            List<WirePointArray> externalWirePointArrays = null)
        {
            bool exists = false;
            if (GetTags != null)
            {
                foreach (string item in GetTags)
                {
                    if (!item.Equals("N/A"))
                    {
                        if (Convert.ToInt32(item).Equals(TagNu))
                            exists = true;
                    }
                }
            }
            if (exists || GetTags == null)
            {
                // 只有用全局集合时才调 TagFormer（否则会改主窗口颜色）
                if (externalValuePairs == null)
                    map.TagFormer();

                OperateDBBLL operate = new OperateDBBLL();
                string[] taglis = operate.SelectTagArr(Time, TagNu.ToString());
                GetTags = taglis;
                AddTagInfo(grid, dt, TagNu, taglis, type,
                    externalValuePairs, externalWirePointArrays);
            }
        }

        /// <summary>
        /// 添加Tag信息
        /// externalValuePairs / externalWirePointArrays 不为 null 时只操作局部集合
        /// </summary>
        public void AddTagInfo(DataGrid gid, DataTable table, int TagNu, string[] taglis, bool type,
            Dictionary<int, Label> externalValuePairs = null,
            List<WirePointArray> externalWirePointArrays = null)
        {
            // 决定用哪个集合
            var vp = externalValuePairs ?? MapInstrument.valuePairs;
            var wpa = externalWirePointArrays ?? MapInstrument.wirePointArrays;

            if (type)
            {
                table.Rows.Add(new object[] { TagNu, TagCompile.agvSpeed[10], TagCompile.agvPbs[16],
                TagCompile.agvTurn[0], TagCompile.agvDire[2], TagCompile.agvHook[4],
                TagCompile.agvTime[0], "default" });
            }
            gid.ItemsSource = table.DefaultView;
            gid.AutoGenerateColumns = false;

            for (int i = 0; i < gid.Items.Count; i++)
            {
                foreach (WirePointArray item in wpa)  // ← 用局部或全局集合
                {
                    if (i < gid.Items.Count - 1)
                    {
                        if ((item.GetPoint.TagID.Equals(Convert.ToInt32(((DataRowView)gid.Items[i])[0]))
                            && item.GetWirePoint.TagID.Equals(Convert.ToInt32(((DataRowView)gid.Items[i + 1])[0])))
                            || (item.GetPoint.TagID.Equals(Convert.ToInt32(((DataRowView)gid.Items[i + 1])[0]))
                            && item.GetWirePoint.TagID.Equals(Convert.ToInt32(((DataRowView)gid.Items[i])[0]))))
                        {
                            Path path = item.GetPath;
                            if (path != null)
                            {
                                path.Stroke = Brushes.Red;
                                path.StrokeThickness = 10;
                                item.GetPath = path;
                            }
                            List<Path> paths = item.Paths;
                            if (paths != null)
                            {
                                for (int s = 0; s < paths.Count; s++)
                                {
                                    Path ph = item.Paths[s];
                                    ph.Stroke = Brushes.Red;
                                    ph.StrokeThickness = 10;
                                    item.Paths[s] = ph;
                                }
                                // 只有 vp 里有对应 key 才访问，防止 KeyNotFoundException
                                int tagI = Convert.ToInt32(((DataRowView)gid.Items[i])[0]);
                                int tagI1 = Convert.ToInt32(((DataRowView)gid.Items[i + 1])[0]);
                                if (vp.ContainsKey(tagI) && vp.ContainsKey(tagI1))
                                {
                                    Point startPt = new Point { X = vp[tagI].Margin.Left - 19, Y = vp[tagI].Margin.Top - 11.5 };
                                    Point endPt = new Point { X = vp[tagI1].Margin.Left - 19, Y = vp[tagI1].Margin.Top - 11.5 };
                                }
                            }
                        }
                    }
                }

                // 安全访问：key 不存在时跳过，不崩溃
                int tagIdx = Convert.ToInt32(((DataRowView)gid.Items[i])[0]);
                if (vp.ContainsKey(tagIdx))
                    vp[tagIdx].Background = new SolidColorBrush(Colors.Purple);
            }

            if (vp.ContainsKey(TagNu))
                vp[TagNu].Background = new SolidColorBrush(Colors.Red);

            for (int i = 0; i < taglis.Length; i++)
            {
                if (!i.Equals(0))
                {
                    int tid = Convert.ToInt32(taglis[i]);
                    if (vp.ContainsKey(tid))
                        vp[tid].Background = new SolidColorBrush(Colors.Green);
                }
            }
        }
    }
}
