using System;
using System.Collections.Generic;
using System.Windows;
using AGVManagement.Models;

namespace AGVManagement.Helpers
{
    /// <summary>
    /// 路径点计算工具类（纯静态，无 UI 依赖）
    /// 提取自 MainWindow：CalculateAngle / ProcessPoints /
    ///   ProcessPointsByRotate / ProcessPointsByTurnRotate / GeneratePointsBetween
    /// </summary>
    public static class PathHelper
    {
        // ── 角度计算 ─────────────────────────────────────────────────────

        /// <summary>计算两点连线的弧度角（范围 (-π, π]）</summary>
        public static double CalculateAngle(double x1, double y1, double x2, double y2)
        {
            double a = Math.Atan2(y2 - y1, x2 - x1);
            return a == Math.PI ? -Math.PI : a;
        }

        // ── 不含顶升 ─────────────────────────────────────────────────────

        /// <summary>输出点位列表（不含顶升），在相邻两点之间插入临近点</summary>
        public static List<PointStraightWithAngle> ProcessPoints(
            List<Point> points, double insertDistance)
        {
            var result = new List<PointStraightWithAngle>();
            int idCounter = 0;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];
                double angleToNext = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y);

                result.Add(new PointStraightWithAngle(idCounter++, p1.X, p1.Y, angleToNext, 6.0, 5.0, 0, true));

                double dist = Distance(p1, p2);
                if (dist > insertDistance)
                {
                    double ratio = insertDistance / dist;
                    double nx = p2.X - ratio * (p2.X - p1.X);
                    double ny = p2.Y - ratio * (p2.Y - p1.Y);
                    result.Add(new PointStraightWithAngle(
                        idCounter++, nx, ny, CalculateAngle(nx, ny, p2.X, p2.Y), 6.0, 5.0, 0, false));
                }
            }

            if (points.Count > 0)
            {
                var last = points[points.Count - 1];
                result.Add(new PointStraightWithAngle(idCounter++, last.X, last.Y, 0, 6.0, 5.0, 0, true));
            }
            return result;
        }

        // ── 含顶升（含 Rotate）────────────────────────────────────────────

        /// <summary>输出点位列表（含顶升 lift），每段末尾插入临近点</summary>
        public static List<PointStraightWithAngle> ProcessPointsByRotate(
            List<Tuple<Point, int, int, int>> pts, double insertDistance)
        {
            var result = new List<PointStraightWithAngle>();
            int idCounter = 0;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                Point p1 = pts[i].Item1, p2 = pts[i + 1].Item1;
                int v1 = pts[i].Item2, v3 = pts[i].Item3, v4 = pts[i].Item4;
                int v5 = pts[i + 1].Item3, v6 = pts[i + 1].Item4;

                double angle = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y);
                result.Add(new PointStraightWithAngle(idCounter++, p1.X, p1.Y, angle, v3, v4, v1, true));

                double dist = Distance(p1, p2);
                if (dist > insertDistance)
                {
                    double ratio = insertDistance / dist;
                    double nx = p2.X - ratio * (p2.X - p1.X);
                    double ny = p2.Y - ratio * (p2.Y - p1.Y);
                    result.Add(new PointStraightWithAngle(
                        idCounter++, nx, ny, CalculateAngle(nx, ny, p2.X, p2.Y), v5, v6, 0, false));
                }
            }

            if (pts.Count > 0)
            {
                var last = pts[pts.Count - 1];
                double lastAngle = pts.Count > 1
                    ? CalculateAngle(pts[pts.Count - 2].Item1.X, pts[pts.Count - 2].Item1.Y,
                                     last.Item1.X, last.Item1.Y)
                    : 0;
                result.Add(new PointStraightWithAngle(
                    idCounter++, last.Item1.X, last.Item1.Y, lastAngle,
                    last.Item3, last.Item4, last.Item2, true));
            }
            return result;
        }

        // ── 含顶升 + Turn ────────────────────────────────────────────────

        /// <summary>输出点位列表（含顶升 + 转向标志），新版 newPointStraightWithAngle</summary>
        public static List<NewPointStraightWithAngle> ProcessPointsByTurnRotate(
            List<Tuple<Point, int, double, double, double, double>> pts, double insertDistance)
        {
            var result = new List<NewPointStraightWithAngle>();
            int idCounter = 0;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                Point p1 = pts[i].Item1, p2 = pts[i + 1].Item1;
                int v1 = pts[i].Item2;
                double v3 = pts[i].Item3, v5 = pts[i + 1].Item3;
                double v4 = pts[i].Item4, v6 = pts[i + 1].Item4;
                double dir = pts[i].Item5;
                double obs = pts[i].Item6, obs2 = pts[i + 1].Item6;

                double angle = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y);
                if (dir != 1) angle = NormalizeAngle(angle + Math.PI);

                result.Add(new NewPointStraightWithAngle(idCounter++, p1.X, p1.Y, angle, v3, v4, v1, true, obs));

                double dist = Distance(p1, p2);
                if (dist > insertDistance)
                {
                    double ratio = insertDistance / dist;
                    double nx = p2.X - ratio * (p2.X - p1.X);
                    double ny = p2.Y - ratio * (p2.Y - p1.Y);
                    result.Add(new NewPointStraightWithAngle(
                        idCounter++, nx, ny, angle, v5, v6, 0, false, obs2));
                }
            }

            if (pts.Count > 0)
            {
                var last = pts[pts.Count - 1];
                double lastAngle = pts.Count > 1
                    ? CalculateAngle(pts[pts.Count - 2].Item1.X, pts[pts.Count - 2].Item1.Y,
                                     last.Item1.X, last.Item1.Y)
                    : 0;

                double finalAngle = last.Item5 == 1 ? lastAngle : NormalizeAngle(lastAngle + Math.PI);
                result.Add(new NewPointStraightWithAngle(
                    idCounter++, last.Item1.X, last.Item1.Y, finalAngle,
                    last.Item3, last.Item4, last.Item2, true, last.Item6));
            }
            return result;
        }

        // ── 均匀插点 ─────────────────────────────────────────────────────

        /// <summary>在路径各段均匀插入点位（直线等间距）</summary>
        public static List<PointStraightWithAngle> GeneratePointsBetween(
            List<Point> pts, double interval)
        {
            var result = new List<PointStraightWithAngle>();
            int idCounter = 1;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                Point s = pts[i], e = pts[i + 1];
                double dist = Distance(s, e);
                double angle = Math.Atan2(e.Y - s.Y, e.X - s.X);
                int n = (int)(dist / interval);

                result.Add(new PointStraightWithAngle(idCounter++, s.X, s.Y, angle, 6.0, 5.0, 0, true));

                for (int j = 1; j < n; j++)
                {
                    double t = (double)j / n;
                    result.Add(new PointStraightWithAngle(
                        idCounter++,
                        s.X + t * (e.X - s.X),
                        s.Y + t * (e.Y - s.Y),
                        angle, 6.0, 5.0, 0, false));
                }
            }

            if (pts.Count > 0)
            {
                var last = pts[pts.Count - 1];
                result.Add(new PointStraightWithAngle(idCounter++, last.X, last.Y, 0, 6.0, 5.0, 0, true));
            }
            return result;
        }

        // ── 私有工具 ─────────────────────────────────────────────────────

        private static double Distance(Point a, Point b)
            => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

        /// <summary>将角度规范化到 (-π, π]</summary>
        private static double NormalizeAngle(double rad)
        {
            rad %= 2 * Math.PI;
            if (rad > Math.PI) rad -= 2 * Math.PI;
            if (rad < -Math.PI) rad += 2 * Math.PI;
            return rad;
        }
    }
}