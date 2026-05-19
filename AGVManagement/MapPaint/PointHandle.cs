using AGV.BLL;
using AGVManagement.Models;   // ← 替代原来的 using static AGVManagement.MainWindow
using AGVManagement.Mqtt;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

namespace AGVManagement.MapPaint
{
    /// <summary>
    /// 路径点处理工具：路线行查找、末段路径生成、列表合并
    /// </summary>
    public class PointHandle
    {
        // ─────────────────────────────────────────────────────────────────
        //  路径点处理
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 生成含顶升的路径点列表（末尾自动延伸 0.3m 停车点）
        /// </summary>
        public List<NewPointStraightWithAngle> ProcessPointsByRotate(
            List<Tuple<Point, int, double, double>> pts, double insertDistance)
        {
            var result = new List<NewPointStraightWithAngle>();
            int idCounter = 0;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                Point p1 = pts[i].Item1, p2 = pts[i + 1].Item1;
                int v1 = pts[i].Item2;
                double v3 = pts[i].Item3, v5 = pts[i + 1].Item3;
                double v4 = pts[i].Item4, v6 = pts[i + 1].Item4;

                double angle = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y);
                result.Add(new NewPointStraightWithAngle(idCounter++, p1.X, p1.Y, angle, v3, v4, v1, true, 0));

                double dist = Distance(p1, p2);
                if (dist > insertDistance)
                {
                    double ratio = insertDistance / dist;
                    double nx = p2.X - ratio * (p2.X - p1.X);
                    double ny = p2.Y - ratio * (p2.Y - p1.Y);
                    result.Add(new NewPointStraightWithAngle(
                        idCounter++, nx, ny, CalculateAngle(nx, ny, p2.X, p2.Y), v5, v6, 0, false, 0));
                }
            }

            // 末尾：反向停车点 + 延伸 0.3m
            if (pts.Count > 0)
            {
                var last = pts[pts.Count - 1];
                double lastAcc = last.Item4;
                double baseAngle = pts.Count > 1
                    ? CalculateAngle(pts[pts.Count - 2].Item1.X, pts[pts.Count - 2].Item1.Y,
                                     last.Item1.X, last.Item1.Y)
                    : 0;

                double reversed = NormalizeAngle(baseAngle + Math.PI);
                result.Add(new NewPointStraightWithAngle(
                    idCounter++, last.Item1.X, last.Item1.Y, reversed, -1, lastAcc, last.Item2, true, 0));

                // 沿原方向延伸 0.3m
                double ex = last.Item1.X + 0.3 * Math.Cos(baseAngle);
                double ey = last.Item1.Y + 0.3 * Math.Sin(baseAngle);
                double extDist = Distance(last.Item1, new Point(ex, ey));

                if (extDist > insertDistance)
                {
                    double ratio = insertDistance / extDist;
                    double nx = ex - ratio * (ex - last.Item1.X);
                    double ny = ey - ratio * (ey - last.Item1.Y);
                    result.Add(new NewPointStraightWithAngle(
                        idCounter++, nx, ny, reversed, -1, lastAcc, 0, false, 1));
                }
                result.Add(new NewPointStraightWithAngle(
                    idCounter++, ex, ey, reversed, -1, lastAcc, 0, true, 1));
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        //  行查找
        // ─────────────────────────────────────────────────────────────────

        /// <summary>按 Tag 首尾匹配路线行（返回行索引，-1 表示未找到）</summary>
        public int FindRowByTags(DataTable dataTable, string dataMes, string dataEndMes)
        {
            try
            {
                string start = dataMes.Substring(2);
                string end = dataEndMes.Substring(2);

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string[] tags = dataTable.Rows[i]["Tag"].ToString().Split(',');
                    if (tags.Length > 0 && tags[0] == start && tags[tags.Length - 1] == end)
                        return i;
                }
                throw new Exception("未找到匹配的行");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return -1; }
        }

        /// <summary>按 Name 字段（格式"起-终"）匹配路线行</summary>
        public int FindRowByTagsName(DataTable dataTable, string dataMes, string dataEndMes)
        {
            try
            {
                string name = $"{dataMes.Substring(2)}-{dataEndMes.Substring(2)}";
                for (int i = 0; i < dataTable.Rows.Count; i++)
                    if (dataTable.Rows[i]["Name"].ToString() == name) return i;

                throw new Exception("未找到匹配的行");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return -1; }
        }

        /// <summary>
        /// 按 Name 匹配，并从多条候选路线中选取起点离当前车辆位置最近的一条。
        /// 特判：路线 1-8 且车辆距 Tag8 &lt;1m 时直接返回 -2（任务已完成）。
        /// </summary>
        public int FindRowByTagsNameNew(DataTable dataTable,
            string dataMes, string dataEndMes,
            Point currentPos, DataTable tagStation)
        {
            try
            {
                string name = $"{dataMes.Substring(2)}-{dataEndMes.Substring(2)}";

                // 特判：已到达终点
                if (name == "1-8")
                {
                    var tag8 = tagStation.AsEnumerable()
                        .FirstOrDefault(r => r["TagName"].ToString() == "Tag8");
                    if (tag8 != null)
                    {
                        double dx = Convert.ToDouble(tag8["X"]) - currentPos.X;
                        double dy = Convert.ToDouble(tag8["Y"]) - currentPos.Y;
                        if (Math.Sqrt(dx * dx + dy * dy) < 1) return -2;
                    }
                }

                // 找出所有同名路线行
                var matched = dataTable.AsEnumerable()
                    .Where(r => r["Name"].ToString() == name)
                    .ToList();

                if (!matched.Any()) throw new Exception("未找到匹配的路线");

                // 选距离起点最近的一行
                DataRow best = null;
                double bestDist = double.MaxValue;

                foreach (var row in matched)
                {
                    string startTag = row["Tag"].ToString().Split(',')[0];
                    var station = tagStation.AsEnumerable()
                        .FirstOrDefault(r => r["TagName"].ToString() == startTag);
                    if (station == null) continue;

                    double dx = Convert.ToDouble(station["X"]) - currentPos.X;
                    double dy = Convert.ToDouble(station["Y"]) - currentPos.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < bestDist) { bestDist = dist; best = row; }
                }

                if (best == null) throw new Exception("没有找到匹配的起点站点");
                return dataTable.Rows.IndexOf(best);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return -1; }
        }

        // ─────────────────────────────────────────────────────────────────
        //  末段路径生成
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 从路线表指定行读取站点序列，查坐标、转换坐标系，返回可直接用于路径计算的 Tuple 列表
        /// </summary>
        public List<Tuple<Point, int, double, double, double, double>> FindMesEnd(
            DataTable dataTable, string mapTime, int rowIndex)
        {
            var tagBLL = new TagInfoBLL();
            var circuitRedact = new Circuitredact();
            var painting = new Painting();

            DataTable tagTable = tagBLL.RataTable(mapTime);

            // 按列拆分行数据
            string[] tags = dataTable.Rows[rowIndex]["Tag"].ToString().Split(',');
            string[] speeds = dataTable.Rows[rowIndex]["Speed"].ToString().Split(',');
            string[] hooks = dataTable.Rows[rowIndex]["Hook"].ToString().Split(',');
            string[] programs = dataTable.Rows[rowIndex]["ChangeProgram"].ToString().Split(',');
            string[] turns = dataTable.Rows[rowIndex]["Turn"].ToString().Split(',');
            string[] obstacles = dataTable.Rows[rowIndex]["Direction"].ToString().Split(',');

            var detail = new DataTable();
            foreach (var col in new[] { "Tag", "X", "Y", "Speed", "Hook", "AngleSpeed", "Turn", "ObsAvoidance" })
                detail.Columns.Add(col);

            for (int i = 0; i < tags.Length; i++)
            {
                var tagRow = tagTable.Select($"TagName = '{tags[i]}'")[0];
                detail.Rows.Add(
                    tags[i],
                    Convert.ToDouble(tagRow["X"]),
                    Convert.ToDouble(tagRow["Y"]),
                    TagCompile.agvSpeed[Convert.ToInt32(speeds[i])],
                    TagCompile.agvHook[Convert.ToInt32(hooks[i])],
                    programs[i],
                    TagCompile.agvTurn[Convert.ToInt32(turns[i])],
                    TagCompile.agvDire[Convert.ToInt32(obstacles[i])]);
            }

            var processed = circuitRedact.ProcessDataNow(circuitRedact.ProcessData(detail));
            var tupleList = circuitRedact.PopulateListFromDataTable(processed);

            return painting.TransformCoordinatesByTurnRotate(
                tupleList,
                MqttClientWrapper.ActualWidthNow,
                MqttClientWrapper.ActualHeightNow,
                MqttClientWrapper.ProportionNow);
        }

        // ─────────────────────────────────────────────────────────────────
        //  列表合并
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 将前段路线（4-Tuple）和末段路线（6-Tuple）合并为统一 6-Tuple 列表，
        /// 前段末尾元素删除（与末段起点重叠），并补全 Item5=1.0、Item6=0.0
        /// </summary>
        public List<Tuple<Point, int, double, double, double, double>> CombineLists(
            List<Tuple<Point, int, double, double>> front,
            List<Tuple<Point, int, double, double, double, double>> tail)
        {
            if (front.Count > 0)
                front.RemoveAt(front.Count - 1);

            var expanded = front.ConvertAll(p =>
                Tuple.Create(p.Item1, p.Item2, p.Item3, p.Item4, 1.0, 0.0));

            var result = new List<Tuple<Point, int, double, double, double, double>>(expanded);
            result.AddRange(tail);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        //  私有工具
        // ─────────────────────────────────────────────────────────────────

        private static double CalculateAngle(double x1, double y1, double x2, double y2)
        {
            double a = Math.Atan2(y2 - y1, x2 - x1);
            return a == Math.PI ? -Math.PI : a;
        }

        private static double Distance(Point a, Point b)
            => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

        private static double NormalizeAngle(double angle)
        {
            if (angle > Math.PI || Math.Abs(angle - Math.PI) < 1e-5) return angle - 2 * Math.PI;
            if (angle < -Math.PI) return angle + 2 * Math.PI;
            return angle;
        }
    }
}