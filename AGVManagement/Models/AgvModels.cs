using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Data;
using System.Linq;
using AGVManagement.Mqtt;
using System;

namespace AGVManagement.Models
{
    /// <summary>AGV 坐标与姿态数据（MQTT 载荷反序列化用）</summary>
    public class CarData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    /// <summary>MES 任务模型</summary>
    public class TaskModel
    {
        public long TaskId { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
    }

    /// <summary>路径点（含角度，不含顶升）</summary>
    public class PointStraightWithAngle
    {
        public int ID { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double angle { get; set; }
        public double v_desired { get; set; }
        public double a_desired { get; set; }
        public int lift { get; set; }
        public bool rotate { get; set; }

        public PointStraightWithAngle(int id, double x, double y,
            double angleToNext, double v, double a, int liftStation, bool rotateNow)
        {
            ID = id; X = x; Y = y;
            angle = angleToNext;
            v_desired = v; a_desired = a;
            lift = liftStation;
            rotate = rotateNow;
        }
    }

    /// <summary>路径点（含角度 + 避障，新版）</summary>
    public class NewPointStraightWithAngle
    {
        public int ID { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double angle { get; set; }
        public double v_desired { get; set; }
        public double a_desired { get; set; }
        public int lift { get; set; }
        public bool rotate { get; set; }
        public double obs_avoidance { get; set; }

        public NewPointStraightWithAngle(int id, double x, double y,
            double angleToNext, double v, double a,
            int liftStation, bool rotateNow, double obsAvoidance)
        {
            ID = id; X = x; Y = y;
            angle = angleToNext;
            v_desired = v; a_desired = a;
            lift = liftStation;
            rotate = rotateNow;
            obs_avoidance = obsAvoidance;
        }
    }

    /// <summary>路径点列表（JSON 序列化用）</summary>
    public class StationData
    {
        [JsonPropertyName("station")]
        public List<PointStraightWithAngle> Stations { get; set; } = new List<PointStraightWithAngle>();
    }

    /// <summary>新版路径点列表（JSON 序列化用）</summary>
    public class NewStationData
    {
        [JsonPropertyName("station")]
        public List<NewPointStraightWithAngle> Stations { get; set; } = new List<NewPointStraightWithAngle>();
    }

    /// <summary>地图上的 AGV 小车（UI 绘制 + 位置更新）</summary>
    public class AgvCar
    {
        public Rectangle Shape { get; private set; }
        private readonly string _address;
        private readonly Point _origin;
        private readonly double _width;
        private readonly double _height;

        public AgvCar(string address, Point origin, double width, double height)
        {
            _address = address;
            _origin = origin;
            _width = width;
            _height = height;
            BuildShape();
        }

        private void BuildShape()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5)
            };
            brush.GradientStops.Add(new GradientStop(Colors.Red, 0));
            brush.GradientStops.Add(new GradientStop(Colors.Red, 0.7));
            brush.GradientStops.Add(new GradientStop(Colors.Blue, 0.7));
            brush.GradientStops.Add(new GradientStop(Colors.Blue, 1));

            Shape = new Rectangle
            {
                Width = _width,
                Height = _height,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = brush
            };
            Canvas.SetLeft(Shape, _origin.X - _width / 2);
            Canvas.SetTop(Shape, _origin.Y - _height / 2);
        }

        public void UpdatePosition(double x, double y, double angle, double rawX, double rawY)
        {
            Canvas.SetLeft(Shape, _origin.X + x - _width / 2);
            Canvas.SetTop(Shape, _origin.Y + y - _height / 2);

            double sx = _origin.X + x;
            double sy = _origin.Y + y;

            string displayX = $"{sx:F2}  ({rawX:F4})";
            string displayY = $"{sy:F2}  ({rawY:F4})";

            GlobalData.UpdateAgvInfo("车辆坐标X", displayX);
            GlobalData.UpdateAgvInfo("车辆坐标Y", displayY);
            GlobalDisplayData.UpdateDisplayInfo(_address, "车辆坐标X", displayX);
            GlobalDisplayData.UpdateDisplayInfo(_address, "车辆坐标Y", displayY);

            Shape.RenderTransform = new RotateTransform(angle, _width / 2, _height / 2);
        }
    }

    /// <summary>DataTable 工具：根据路线表生成含邻接信息的站点表</summary>
    public static class RouteTableHelper
    {
        public static DataTable GeneratePointTable(DataTable routeTable)
        {
            var result = new DataTable("Points");
            result.Columns.Add("TagName", typeof(string));
            result.Columns.Add("X", typeof(double));
            result.Columns.Add("Y", typeof(double));
            result.Columns.Add("neighborIds", typeof(string));

            var dict = new System.Collections.Generic.Dictionary<string,
                (double X, double Y, System.Collections.Generic.HashSet<string> Neighbors)>();

            foreach (DataRow row in routeTable.Rows)
            {
                string t1 = row["Tag1"].ToString(), t2 = row["Tag2"].ToString();
                double sx = System.Convert.ToDouble(row["StartX"]), sy = System.Convert.ToDouble(row["StartY"]);
                double ex = System.Convert.ToDouble(row["EndX"]), ey = System.Convert.ToDouble(row["EndY"]);

                if (!dict.ContainsKey(t1)) dict[t1] = (sx, sy, new System.Collections.Generic.HashSet<string>());
                dict[t1].Neighbors.Add(t2);
                if (!dict.ContainsKey(t2)) dict[t2] = (ex, ey, new System.Collections.Generic.HashSet<string>());
                dict[t2].Neighbors.Add(t1);
            }

            foreach (var kv in dict.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                result.Rows.Add(kv.Key, kv.Value.X, kv.Value.Y,
                    string.Join(",", new SortedSet<string>(
                        kv.Value.Neighbors, StringComparer.OrdinalIgnoreCase)));
            }
            return result;
        }
    }
}