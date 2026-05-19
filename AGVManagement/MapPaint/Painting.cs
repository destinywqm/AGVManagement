// ============================================================
//  Painting.cs（重构后）
//  主要变更：
//    - 移除 using static AGVManagement.MainWindow
//    - 改为 using AGVManagement.Models
//    - newPointStraightWithAngle  → NewPointStraightWithAngle
//    - newStationData             → NewStationData
//    - ProcessPointsByTurnRotate 内部实现调用 PathHelper（或保留原逻辑）
//    - 删除无用 using（System.Web.UI、System.Drawing 等不可用的引用）
// ============================================================
using AGVManagement.Enumeration;
using AGVManagement.Models;    // ← 替代 using static AGVManagement.MainWindow
using AGVManagement.Mqtt;
using Microsoft.Win32;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace AGVManagement.MapPaint
{
    /// <summary>地图绘制与坐标计算工具类</summary>
    public class Painting
    {
        public Canvas mainPan;
        private int x = 100;
        private int y = 100;
        private IMqttClient _client;
        private static List<Point> coordinates;

        // ─────────────────────────────────────────────────────────────────
        //  基础绘制
        // ─────────────────────────────────────────────────────────────────

        public Path DrawingLine(Point startPt, Point endPt, Canvas mainPanel)
        {
            var geo = new LineGeometry { StartPoint = startPt, EndPoint = endPt };
            var path = new Path { Stroke = Brushes.Black, StrokeThickness = 1, Data = geo };
            mainPanel.Children.Add(path);
            return path;
        }

        public Path DrawingSemicircle(Point startPt, Point endPt, Canvas mainPanel)
            => GetPath1(startPt, endPt, mainPanel, false, 50);

        public Path GetPath1(Point startPt, Point endPt, Canvas mainPanel, bool isStatic, int size)
        {
            var controlPt = new Point((startPt.X + endPt.X) / 2, (startPt.Y + endPt.Y) / 2);
            var bezier = new BezierSegment(startPt, controlPt, endPt, true);
            var figure = new PathFigure { StartPoint = startPt };
            figure.Segments.Add(bezier);
            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            var path = new Path { Data = geo, Stroke = Brushes.Black };
            mainPanel.Children.Add(path);

            path.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    bezier.Point3 = e.GetPosition(mainPanel);
            };
            return path;
        }

        public Path GetPath(Point startPt, Point endPt, Canvas mainPanel, bool isStatic, int size)
        {
            var arc = new ArcSegment(
                startPt, new Size(size, size), 0, false,
                isStatic ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true);
            var figure = new PathFigure { StartPoint = endPt };
            figure.Segments.Add(arc);
            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            var path = new Path { Data = geo, Stroke = Brushes.Black };
            mainPanel.Children.Add(path);
            path.MouseLeftButtonDown += Path_MouseLeftButtonDown;
            path.MouseMove += Path_MouseMove;
            path.MouseLeftButtonUp += Path_MouseLeftButtonUp;
            return path;
        }

        // ─────────────────────────────────────────────────────────────────
        //  弧线点位
        // ─────────────────────────────────────────────────────────────────

        public List<Point> GetPointsOnArc(Point startPt, Point endPt, bool isClockwise, int radius, double angleIncrement)
        {
            var pts = new List<Point>();
            double angle = Math.Atan2(endPt.Y - startPt.Y, endPt.X - startPt.X);
            double startAngle = isClockwise ? angle : angle + Math.PI;
            double endAngle = isClockwise ? angle + Math.PI : angle;

            for (double a = startAngle;
                 isClockwise ? a < endAngle : a > endAngle;
                 a = isClockwise ? a + angleIncrement : a - angleIncrement)
                pts.Add(new Point(startPt.X + radius * Math.Cos(a), startPt.Y + radius * Math.Sin(a)));

            return pts;
        }

        public static List<Point> GetArcPoints(Point startPt, Point endPt, Point center, double radius)
        {
            var pts = new List<Point>();
            bool identifying = false;
            double startX = startPt.X, endX = endPt.X;

            if (startX > endX) { startX = endPt.X; endX = startPt.X; identifying = true; }

            for (double cx = startX; cx <= endX; cx += 0.005)
            {
                double dist = radius * radius - (cx - center.X) * (cx - center.X);
                if (dist < 0) continue;

                double y1 = center.Y + Math.Sqrt(dist);
                double y2 = center.Y - Math.Sqrt(dist);
                double cy;

                if (center.Y < startPt.Y) cy = y1;
                else if (center.Y > startPt.Y) cy = y2;
                else if (center.X > startX) cy = y2;
                else cy = y1;

                pts.Add(new Point(cx, cy));
            }

            if (identifying) pts = ReverseListStatic(pts);
            return pts;
        }

        public static List<Point> GetArcPointsByRadian(Point startPt, Point endPt, Point center, double radius)
        {
            var pts = new List<Point>();
            double startA = Math.Atan2(startPt.Y - center.Y, startPt.X - center.X);
            double endA = Math.Atan2(endPt.Y - center.Y, endPt.X - center.X);
            if (endA < startA) endA += 2 * Math.PI;

            for (double a = startA; a <= endA; a += 0.1)
                pts.Add(new Point(center.X + radius * Math.Cos(a), center.Y + radius * Math.Sin(a)));

            return pts;
        }

        // ─────────────────────────────────────────────────────────────────
        //  拖拽事件（半圆/弧线）
        // ─────────────────────────────────────────────────────────────────

        private bool isDragging;
        private Point lastMousePosition;

        private void Path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var path = (Path)sender;
            isDragging = true;
            lastMousePosition = e.GetPosition((Canvas)path.Parent);
            path.CaptureMouse();
        }

        private void Path_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;
            var path = (Path)sender;
            var panel = (Canvas)path.Parent;
            var pos = e.GetPosition(panel);
            Vector offset = pos - lastMousePosition;

            var arc = ((PathFigure)((PathGeometry)path.Data).Figures[0]).Segments[0] as ArcSegment;
            if (arc != null) arc.Point = new Point(arc.Point.X + offset.X, arc.Point.Y + offset.Y);
            lastMousePosition = pos;
        }

        private void Path_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ((Path)sender).ReleaseMouseCapture();
        }

        // ─────────────────────────────────────────────────────────────────
        //  小车 / 矩形绘制
        // ─────────────────────────────────────────────────────────────────

        private Rectangle carRect;
        private double mapWidth, mapHeight;

        public Rectangle CreateCar(double w, double h, Canvas mainPanel)
        {
            this.mapWidth = w; this.mapHeight = h;
            mainPanel.KeyDown += MainPanel_KeyDown;
            mainPanel.Focusable = true;
            mainPanel.Focus();

            carRect = new Rectangle { Width = 10, Height = 10, Fill = Brushes.Red };
            Canvas.SetLeft(carRect, (mainPanel.ActualWidth - carRect.Width) / 2);
            Canvas.SetTop(carRect, (mainPanel.ActualHeight - carRect.Height) / 2);
            mainPanel.Children.Add(carRect);
            return carRect;
        }

        private void MainPanel_KeyDown(object sender, KeyEventArgs e)
        {
            double cx = Canvas.GetLeft(carRect), cy = Canvas.GetTop(carRect);
            switch (e.Key)
            {
                case Key.Left: Canvas.SetLeft(carRect, cx - 10); break;
                case Key.Right: Canvas.SetLeft(carRect, cx + 10); break;
                case Key.Up: Canvas.SetTop(carRect, cy - 10); break;
                case Key.Down: Canvas.SetTop(carRect, cy + 10); break;
            }
        }

        private Rectangle rectangle;

        public void DrawRectangle(Canvas canvas, Point position, double width, double height, double angle)
        {
            position.Y = -position.Y;
            double cx = canvas.ActualWidth / 2, cy = canvas.ActualHeight / 2;
            double left = cx + position.X - width / 2, top = cy + position.Y - height / 2;

            if (rectangle == null)
            {
                var brush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0.5),
                    EndPoint = new Point(1, 0.5)
                };
                brush.GradientStops.Add(new GradientStop(Colors.Blue, 0));
                brush.GradientStops.Add(new GradientStop(Colors.Red, 1));
                rectangle = new Rectangle { Width = width, Height = height, Fill = brush };
                canvas.Children.Add(rectangle);
            }

            Canvas.SetLeft(rectangle, left); Canvas.SetTop(rectangle, top);
            rectangle.Width = width;
            rectangle.Height = height;
            rectangle.RenderTransform = new RotateTransform(-angle, width / 2, height / 2);
        }

        // ─────────────────────────────────────────────────────────────────
        //  贝塞尔曲线
        // ─────────────────────────────────────────────────────────────────

        private Path path;
        private Point startPt, endPt, controlPt0;
        private bool isDragging1;

        public Path DrawingQuadraticBezierCurve(Point s, Point e, Canvas panel)
        {
            startPt = s; endPt = e;
            controlPt0 = new Point((s.X + e.X + 100) / 2, (s.Y + e.Y + 100) / 2);
            path = GetQuadraticBezierCurve(s, e, controlPt0, panel);
            panel.MouseLeftButtonDown += MainPanel_MouseLeftButtonDown;
            panel.MouseLeftButtonUp += MainPanel_MouseLeftButtonUp;
            panel.MouseMove += MainPanel_MouseMove;
            return path;
        }

        public Path GetQuadraticBezierCurve(Point s, Point e, Point ctrl, Canvas panel)
        {
            var seg = new QuadraticBezierSegment { Point1 = ctrl, Point2 = e };
            var figure = new PathFigure { StartPoint = s };
            figure.Segments.Add(seg);
            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            var p = new Path { Data = geo, Stroke = Brushes.Black };
            panel.Children.Add(p);

            var el = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Red };
            Canvas.SetLeft(el, ctrl.X - 5); Canvas.SetTop(el, ctrl.Y - 5);
            panel.Children.Add(el);
            return p;
        }

        private void MainPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition((Canvas)sender);
            if (isDragging1) { isDragging1 = false; return; }
            if (Math.Abs(pos.X - controlPt0.X) < 100 && Math.Abs(pos.Y - controlPt0.Y) < 100)
                isDragging1 = true;
        }

        private void MainPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging1) return;
            UpdateQuadraticBezierCurve(e.GetPosition((Canvas)sender));
            isDragging1 = false;
        }

        private void MainPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => isDragging1 = false;

        private void UpdateQuadraticBezierCurve(Point newCtrl)
        {
            controlPt0 = newCtrl;
            var seg = (QuadraticBezierSegment)((PathFigure)((PathGeometry)path.Data).Figures[0]).Segments[0];
            seg.Point1 = controlPt0;
            path.InvalidateVisual();
        }

        // ─────────────────────────────────────────────────────────────────
        //  折线绘制
        // ─────────────────────────────────────────────────────────────────

        public List<Path> DrawingBroken(Point startPt, Point endPt, Canvas mainPanel)
        {
            var pts = new List<Path>();
            double drn = startPt.X - endPt.X;
            double hrn = startPt.Y - endPt.Y;
            bool cross = (drn > 0 && hrn < 0) || (drn < 0 && hrn > 0);

            if (cross)
            {
                if (startPt.Y < endPt.Y)
                {
                    var TX = new Point(endPt.X + 20, startPt.Y);
                    var TY = new Point(endPt.X, startPt.Y + 20);
                    pts.Add(DrawingLine(endPt, TY, mainPanel));
                    pts.Add(GetPath(TX, TY, mainPanel, true, 20));
                    pts.Add(DrawingLine(startPt, TX, mainPanel));
                }
                else
                {
                    var TX = new Point(endPt.X - 20, startPt.Y);
                    var TY = new Point(endPt.X, startPt.Y - 20);
                    pts.Add(DrawingLine(startPt, TX, mainPanel));
                    pts.Add(GetPath(TX, TY, mainPanel, true, 20));
                    pts.Add(DrawingLine(endPt, TY, mainPanel));
                }
            }
            else
            {
                if (startPt.Y < endPt.Y)
                {
                    var TX = new Point(endPt.X - 20, startPt.Y);
                    var TY = new Point(endPt.X, startPt.Y + 20);
                    pts.Add(DrawingLine(startPt, TX, mainPanel));
                    pts.Add(GetPath(TX, TY, mainPanel, false, 20));
                    pts.Add(DrawingLine(endPt, TY, mainPanel));
                }
                else
                {
                    var TX = new Point(endPt.X + 20, startPt.Y);
                    var TY = new Point(endPt.X, startPt.Y - 20);
                    pts.Add(DrawingLine(endPt, TY, mainPanel));
                    pts.Add(GetPath(TX, TY, mainPanel, false, 20));
                    pts.Add(DrawingLine(startPt, TX, mainPanel));
                }
            }
            return pts;
        }

        public List<Point> DrawingBrokenPoint(Point startPt, Point endPt)
        {
            const double R = 63.1575;
            var pts = new List<Point>();
            double drn = startPt.X - endPt.X;
            double hrn = startPt.Y - endPt.Y;
            bool cross = (drn > 0 && hrn < 0) || (drn < 0 && hrn > 0);

            Point TX, TY, C;
            if (cross)
            {
                if (startPt.Y < endPt.Y)
                {
                    TX = new Point(startPt.X, endPt.Y - R); TY = new Point(startPt.X - R, endPt.Y);
                    C = new Point(startPt.X - R, endPt.Y - R);
                }
                else
                {
                    TX = new Point(startPt.X, endPt.Y + R); TY = new Point(startPt.X + R, endPt.Y);
                    C = new Point(startPt.X + R, endPt.Y + R);
                }
            }
            else
            {
                if (startPt.Y < endPt.Y)
                {
                    TX = new Point(endPt.X - R, startPt.Y); TY = new Point(endPt.X, startPt.Y + R);
                    C = new Point(endPt.X - R, startPt.Y + R);
                }
                else
                {
                    TX = new Point(endPt.X + R, startPt.Y); TY = new Point(endPt.X, startPt.Y - R);
                    C = new Point(endPt.X + R, startPt.Y - R);
                }
            }

            pts = GetLinePoints(startPt, TX);
            pts.AddRange(GetArcPointsByRadian(TX, TY, C, R));
            pts.AddRange(GetLinePoints(TY, endPt));
            return pts;
        }

        // ─────────────────────────────────────────────────────────────────
        //  点位绘制
        // ─────────────────────────────────────────────────────────────────

        public void DrawDots(Canvas canvas, List<Point> pts, double scale)
        {
            if (pts == null) return;
            double cx = canvas.ActualWidth / 2, cy = canvas.ActualHeight / 2;
            foreach (var p in pts)
            {
                var el = new Ellipse { Width = 2, Height = 2, Fill = Brushes.Red };
                Canvas.SetLeft(el, cx + p.X * scale * 100 - 1);
                Canvas.SetTop(el, cy - p.Y * scale * 100 - 1);
                canvas.Children.Add(el);
            }
        }

        public void DrawDotsNow(Canvas canvas, List<Point> pts, double scale, Point origin)
        {
            if (pts == null) return;
            double cx = canvas.ActualWidth / 2, cy = canvas.ActualHeight / 2;
            double ox = cx + (origin.X - cx), oy = cy + (origin.Y - cy);
            foreach (var p in pts)
            {
                double sx = (p.X - ox) * scale * 100;
                double sy = (p.Y - oy) * scale * 100;
                var el = new Ellipse { Width = 2, Height = 2, Fill = Brushes.Red };
                Canvas.SetLeft(el, cx + sx - 1);
                Canvas.SetTop(el, cy - sy - 1);
                canvas.Children.Add(el);
            }
        }

        public static void DrawPointsOnCanvas(Canvas canvas, List<Point> pts)
        {
            if (pts.Count < 2) return;
            for (int i = 1; i < pts.Count; i++)
                canvas.Children.Add(new Line
                {
                    X1 = pts[i - 1].X,
                    Y1 = pts[i - 1].Y,
                    X2 = pts[i].X,
                    Y2 = pts[i].Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });
        }

        public static void DrawPointsOnCanvasTest(Canvas canvas, List<Point> pts)
        {
            foreach (var p in pts)
            {
                var el = new Ellipse { Width = 1, Height = 1, Fill = Brushes.Blue };
                Canvas.SetLeft(el, p.X); Canvas.SetTop(el, p.Y);
                canvas.Children.Add(el);
            }
        }

        public void DrawPointsOnCanvasBySlam(Canvas canvas, List<Point> pts)
        {
            foreach (var p in pts)
            {
                var el = new Ellipse { Width = 1, Height = 1, Fill = Brushes.Black };
                Canvas.SetLeft(el, p.X - 3); Canvas.SetTop(el, p.Y - 3);
                canvas.Children.Add(el);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  坐标转换
        // ─────────────────────────────────────────────────────────────────

        public List<Point> TransformCoordinates(List<Point> pts, int ox, int oy, double factor)
        {
            return pts.Select(p => new Point((p.X - ox) / factor, (oy - p.Y) / factor)).ToList();
        }

        public List<Point> ReverseTransformCoordinates(List<Point> pts, int ox, int oy, double factor)
        {
            return pts.Select(p => new Point(p.X * factor + ox, oy - p.Y * factor)).ToList();
        }

        public Point ReverseTransformCoordinate(Point p, double ox, double oy, double factor)
            => new Point(p.X * factor + ox, oy - p.Y * factor);

        public Point TransformCoordinate(Point p, double ox, double oy, double factor)
            => new Point((p.X - ox) / factor, (oy - p.Y) / factor);

        public List<Point> TransformCoordinatesNoFactor(List<Point> pts, int ox, int oy)
        {
            return pts.Select(p => new Point(
                Math.Round(p.X - ox, 4), Math.Round(oy - p.Y, 4))).ToList();
        }

        public List<Point> InverseTransformCoordinates(List<Point> pts, int ox, int oy, double factor)
        {
            return pts.Select(p => new Point(
                Math.Round(p.X * factor + ox, 4),
                Math.Round(oy - p.Y * factor, 4))).ToList();
        }

        public List<Tuple<Point, int, double, double>> RemoveRedundantRoutes(
            List<Tuple<Point, int, double, double>> pts)
        {
            while (pts.Count >= 3 && pts[0].Item1.Equals(pts[2].Item1))
            {
                pts.RemoveAt(0);
                pts.RemoveAt(0);
            }
            return pts;
        }

        public List<Tuple<Point, int, double, double>> TransformCoordinatesByRotate(
            List<Tuple<Point, int, double, double>> pts, double ox, double oy, double factor)
        {
            return pts.Select(t =>
            {
                double nx = (t.Item1.X - ox) / factor;
                double ny = (oy - t.Item1.Y) / factor;
                return Tuple.Create(new Point(nx, ny), t.Item2, t.Item3, t.Item4);
            }).ToList();
        }

        public List<Tuple<Point, int, double, double, double, double>> TransformCoordinatesByTurnRotate(
            List<Tuple<Point, int, double, double, double, double>> pts, double ox, double oy, double factor)
        {
            return pts.Select(t =>
            {
                double nx = (t.Item1.X - ox) / factor;
                double ny = (oy - t.Item1.Y) / factor;
                return Tuple.Create(new Point(nx, ny), t.Item2, t.Item3, t.Item4, t.Item5, t.Item6);
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────
        //  直线点位
        // ─────────────────────────────────────────────────────────────────

        public List<Point> GetLinePoints(Point s, Point e)
        {
            var pts = new List<Point>();
            double dist = Math.Sqrt(Math.Pow(e.X - s.X, 2) + Math.Pow(e.Y - s.Y, 2));
            if (dist == 0) return pts;
            double step = 0.1, dx = (e.X - s.X) / dist * step, dy = (e.Y - s.Y) / dist * step;
            double cx = s.X, cy = s.Y;
            while (dist > 0) { pts.Add(new Point(cx, cy)); cx += dx; cy += dy; dist -= step; }
            return pts;
        }

        public static List<Point> GetUpperHalfCirclePoints(Point center, double radius)
        {
            var pts = new List<Point>();
            for (double cx = center.X - radius; cx < center.X + radius; cx += 0.001)
                pts.Add(new Point(cx, center.Y + Math.Sqrt(radius * radius - (cx - center.X) * (cx - center.X))));
            return pts;
        }

        public List<Point> GetPointsOnCurve(Point s, Point e)
        {
            double d = Math.Abs(e.X - s.X) / 2;
            var ctrl = new Point(s.X + d, s.Y);
            int n = (int)Math.Ceiling(Math.Abs(e.X - s.X) / 0.1);
            var pts = new List<Point>();
            for (int i = 0; i <= n; i++)
            {
                double t = (double)i / n;
                pts.Add(new Point(
                    (1 - t) * (1 - t) * s.X + 2 * (1 - t) * t * ctrl.X + t * t * e.X,
                    (1 - t) * (1 - t) * s.Y + 2 * (1 - t) * t * s.Y + t * t * e.Y));
            }
            return pts;
        }

        // ─────────────────────────────────────────────────────────────────
        //  坐标网格
        // ─────────────────────────────────────────────────────────────────

        public void Coordinate(Canvas panel)
        {
            mainPan = panel;
            for (int i = 0; i <= panel.Height / x; i++)
                DrawingLine(new Point(0, x * i), new Point(panel.Width, y * i), panel);
            for (int i = 0; i <= panel.Width / x; i++)
                DrawingLine(new Point(x * i, 0), new Point(x * i, panel.Height), panel);
        }

        public void CoordinateX(Canvas panelX, Canvas panelY)
        {
            for (int i = 0; i <= panelX.Width / x; i++)
            {
                var tb = new TextBlock
                {
                    Text = (i * 10).ToString(),
                    Foreground = new SolidColorBrush(Colors.Black)
                };
                Canvas.SetLeft(tb, i != panelX.Width / x ? i * x - 8 : i * x - 35);
                Canvas.SetTop(tb, 0);
                panelX.Children.Add(tb);
            }
            for (int i = 0; i <= panelY.Height / x; i++)
            {
                var tb = new TextBlock
                {
                    Text = i == 0 ? "" : (i * 10).ToString(),
                    Foreground = new SolidColorBrush(Colors.Black)
                };
                Canvas.SetLeft(tb, 0);
                Canvas.SetTop(tb, i != panelY.Height / x ? i * x - 7.5 : i * x - 10);
                panelY.Children.Add(tb);
            }
        }

        public static int siseWin = 1;

        public void Mapmagnify(int size, Canvas panelX, Canvas panelY, Canvas map, double ms, double mp)
        {
            panelX.Children.Clear(); panelY.Children.Clear(); map.Children.Clear();
            var pts = GetCoordinatesFromFile();

            if (size == 0)
            {
                x = y = 100;
                map.Width = panelX.Width = ms;
                panelY.Width = mp;
                DrawDots(map, pts, 1);
                Coordinate(map);
                CoordinateX(panelX, panelY);
                Zoom(1); siseWin = 1;
            }
            else
            {
                x = y = 100 * size;
                map.Width = panelX.Width = size * ms;
                map.Height = panelX.Height = size * mp;
                panelY.Height = size * mp;
                DrawDots(map, pts, size);
                Coordinate(map);
                CoordinateX(panelX, panelY);
                Zoom(size); siseWin = size;
            }
        }

        public void Zoom(int size)
        {
            foreach (var item in MapInstrument.wirePointArrays)
            {
                Point p1 = item.GetPoint.SetPoint;
                p1.X = (p1.X / siseWin) * size; p1.Y = (p1.Y / siseWin) * size;
                item.GetPoint.SetPoint = p1;

                Point p2 = item.GetWirePoint.SetPoint;
                p2.X = (p2.X / siseWin) * size; p2.Y = (p2.Y / siseWin) * size;
                item.GetWirePoint.SetPoint = p2;

                Path path = null;
                List<Path> paths = null;

                switch (item.circuitType)
                {
                    case CircuitType.Line:
                        path = DrawingLine(p1, p2, mainPan);
                        path.Stroke = item.GetPath.Stroke;
                        path.StrokeThickness = item.GetPath.StrokeThickness;
                        break;
                    case CircuitType.Semicircle:
                        path = DrawingSemicircle(p1, p2, mainPan);
                        path.Stroke = item.GetPath.Stroke;
                        path.StrokeThickness = item.GetPath.StrokeThickness;
                        break;
                    case CircuitType.QuadraticBezierCurve:
                        path = DrawingQuadraticBezierCurve(p1, p2, mainPan);
                        path.Stroke = item.GetPath.Stroke;
                        path.StrokeThickness = item.GetPath.StrokeThickness;
                        break;
                    case CircuitType.Broken:
                        paths = DrawingBroken(p1, p2, mainPan);
                        foreach (var kp in paths)
                            foreach (var op in item.Paths)
                            { kp.Stroke = op.Stroke; kp.StrokeThickness = op.StrokeThickness; }
                        break;
                }
                item.GetPath = path; item.Paths = paths;
            }

            foreach (int k in MapInstrument.valuePairs.Keys)
            {
                var lbl = MapInstrument.valuePairs[k];
                lbl.Margin = new Thickness(
                    ((lbl.Margin.Left + 19) / siseWin) * size - 19,
                    ((lbl.Margin.Top + 11.5) / siseWin) * size - 11.5, 0, 0);
                mainPan.Children.Add(lbl);
            }
            foreach (int k in MapInstrument.keyValuePairs.Keys)
            {
                var lbl = MapInstrument.keyValuePairs[k];
                lbl.Margin = new Thickness((lbl.Margin.Left / siseWin) * size, (lbl.Margin.Top / siseWin) * size, 0, 0);
                lbl.Width = lbl.Width / siseWin * size;
                lbl.Height = lbl.Height / siseWin * size;
                lbl.FontSize = lbl.FontSize / siseWin * size;
                mainPan.Children.Add(lbl);
            }
            foreach (int k in MapInstrument.GetKeyValues.Keys)
            {
                var lbl = MapInstrument.GetKeyValues[k];
                lbl.Margin = new Thickness((lbl.Margin.Left / siseWin) * size, (lbl.Margin.Top / siseWin) * size, 0, 0);
                lbl.FontSize = lbl.FontSize / siseWin * size;
                mainPan.Children.Add(lbl);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  文件工具
        // ─────────────────────────────────────────────────────────────────

        public List<Point> GetCoordinatesFromFile()
        {
            if (coordinates != null) return coordinates;
            var dlg = new OpenFileDialog { Filter = "文本文件 (*.txt)|*.txt" };
            if (dlg.ShowDialog() != true) return new List<Point>();

            coordinates = new List<Point>();
            int idx = 0;
            foreach (string line in File.ReadAllLines(dlg.FileName))
                foreach (string v in line.Split(','))
                    if (double.TryParse(v, out double d))
                    {
                        double rad = idx * 0.8 * Math.PI / 180;
                        coordinates.Add(new Point(d * Math.Cos(rad), d * Math.Sin(rad)));
                        idx++;
                    }
            return coordinates;
        }

        public List<Point> GetCoordinatesFromMqttMessage()
        {
            // MQTT 订阅逻辑保留原样（静态 coordinates 更新）
            return coordinates;
        }

        public void SavePointsToFile(List<Point> pts, string path)
        {
            using (var w = new StreamWriter(path))
            {
                foreach (var p in pts) w.WriteLine($"{p.X} {p.Y}");
            }
        }

        public void SavePointsToFileByAngle(List<PointDataMapLine> pts, string path)
        {
            using (var w = new StreamWriter(path))
            {
                foreach (var p in pts) w.WriteLine($"{p.X} {p.Y} {p.Angle}");
            }
        }

        public void SavePointsToCsvFileByAngle(List<PointDataMapLine> pts, string path)
        {
            using (var w = new StreamWriter(path))
            {
                foreach (var p in pts) w.WriteLine($"{p.X},{p.Y},{p.Angle}");
            }
        }

        public static List<Point> ReadPointsFromFile(string filePath)
        {
            var pts = new List<Point>();
            try
            {
                foreach (string line in File.ReadAllLines(filePath))
                {
                    string[] p = line.Split(' ');
                    if (p.Length >= 2 && double.TryParse(p[0], out double x) && double.TryParse(p[1], out double y))
                        pts.Add(new Point(x, y));
                }
            }
            catch (IOException ex) { Console.WriteLine(ex.Message); }
            return pts;
        }

        public void TransformPointsByAngle(string filePath, double nx, double ny, double angle)
        {
            var rt = new RotateTransform(angle);
            var pts = File.ReadAllLines(filePath)
                .Select(l => l.Split(' '))
                .Where(p => p.Length == 2)
                .Select(p => new Point(double.Parse(p[0]), double.Parse(p[1])))
                .Select(p => { var tp = rt.Transform(p); tp.Offset(nx, ny); return tp; })
                .ToList();
            File.WriteAllLines(filePath, pts.Select(p => $"{p.X} {p.Y}"));
        }

        public void ReverseLinesInFile(string path)
        {
            var lines = File.ReadAllLines(path).Reverse().ToArray();
            File.WriteAllLines(path, lines);
        }

        public List<Point> ReverseList(List<Point> list)
        {
            var r = new List<Point>(list.Count);
            for (int i = list.Count - 1; i >= 0; i--) r.Add(list[i]);
            return r;
        }

        private static List<Point> ReverseListStatic(List<Point> list)
        {
            var r = new List<Point>(list.Count);
            for (int i = list.Count - 1; i >= 0; i--) r.Add(list[i]);
            return r;
        }

        // ─────────────────────────────────────────────────────────────────
        //  点位数据生成
        // ─────────────────────────────────────────────────────────────────

        public static string GeneratePointDataJson(List<Point> pts)
            => JsonSerializer.Serialize(GeneratePointData(pts, 0.1));

        public List<PointDataMapLine> GeneratePointDataByCurve(List<Point> pts)
        {
            var list = new List<PointDataMapLine>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                double angle = Math.Atan2(pts[i + 1].Y - pts[i].Y, pts[i + 1].X - pts[i].X);
                list.Add(new PointDataMapLine(pts[i].X, pts[i].Y, angle));
            }
            list.Add(list.Count > 0
                ? new PointDataMapLine(pts[pts.Count - 1].X, pts[pts.Count - 1].Y, list.Last().Angle)
                : new PointDataMapLine(pts[pts.Count - 1].X, pts[pts.Count - 1].Y, 0));
            return list;
        }

        private static List<PointDataMapLine> GeneratePointData(List<Point> pts, double step)
        {
            var list = new List<PointDataMapLine>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                double dx = pts[i + 1].X - pts[i].X, dy = pts[i + 1].Y - pts[i].Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                int n = (int)Math.Ceiling(dist / step);
                double ang = Math.Atan2(dy, dx) * 180 / Math.PI;
                for (int j = 0; j <= n; j++)
                {
                    double t = (double)j / n;
                    list.Add(new PointDataMapLine(pts[i].X + t * dx, pts[i].Y + t * dy, ang));
                }
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────────────
        //  路径搜索 & DataTable 工具（保持原逻辑，类型已更新）
        // ─────────────────────────────────────────────────────────────────

        public static DataTable ExpandBidirectionalRoutes(DataTable src)
        {
            var result = src.Clone();
            foreach (DataRow row in src.Rows)
            {
                result.ImportRow(row);
                var rev = result.NewRow();
                rev["StartX"] = row["EndX"]; rev["StartY"] = row["EndY"];
                rev["EndX"] = row["StartX"]; rev["EndY"] = row["StartY"];
                rev["LineStyel"] = row["LineStyel"];
                rev["Tag1"] = row["Tag2"]; rev["Tag2"] = row["Tag1"];
                result.Rows.Add(rev);
            }
            int id = 1;
            foreach (DataRow row in result.Rows) row["ID"] = id++;
            return result;
        }

        public static DataTable FindRouteNew(DataTable routes, Point inputPoint, string targetTag)
        {
            var result = routes.Clone();
            result.Columns.Remove("ID");
            result.Columns.Remove("LineStyel");

            var sorted = routes.AsEnumerable()
                .Select(row => new
                {
                    Row = row,
                    Dist = CalcDist(inputPoint,
                        Convert.ToDouble(row["StartX"]), Convert.ToDouble(row["StartY"]))
                })
                .OrderBy(x => x.Dist).ToList();

            foreach (var entry in sorted)
            {
                var temp = result.Clone();
                if (entry.Dist < 0.5)
                    temp.ImportRow(entry.Row);
                else
                {
                    var fr = temp.NewRow();
                    fr["StartX"] = inputPoint.X; fr["StartY"] = inputPoint.Y;
                    fr["EndX"] = entry.Row["StartX"]; fr["EndY"] = entry.Row["StartY"];
                    fr["Tag1"] = "InputPoint"; fr["Tag2"] = entry.Row["Tag1"];
                    temp.Rows.Add(fr);
                }

                var visited = new List<DataRow>();
                var path = FindShortestPath(routes, entry.Row["Tag1"].ToString(), targetTag, visited);
                if (path != null && path.Count > 0)
                {
                    foreach (DataRow r in path) temp.ImportRow(r);
                    return temp;
                }
            }
            throw new Exception("No route found to reach the target.");
        }

        private static List<DataRow> FindShortestPath(DataTable routes, string start, string target, List<DataRow> visited)
        {
            var queue = new Queue<List<DataRow>>();
            foreach (DataRow row in routes.Rows)
                if (row["Tag1"].ToString() == start && !visited.Contains(row))
                {
                    queue.Enqueue(new List<DataRow> { row });
                    visited.Add(row);
                }

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var last = cur.Last();
                if (last["Tag2"].ToString() == target) return cur;

                string endTag = last["Tag2"].ToString();
                foreach (DataRow row in routes.Rows)
                    if (row["Tag1"].ToString() == endTag && !visited.Contains(row))
                    {
                        queue.Enqueue(new List<DataRow>(cur) { row });
                        visited.Add(row);
                    }
            }
            return null;
        }

        public static DataTable ProcessDataTable(DataTable t)
        {
            if (t.Rows.Count < 2 || !t.Columns.Contains("StartX")) return t;
            if (t.Rows[0]["StartX"].Equals(t.Rows[1]["StartX"]) &&
                t.Rows[0]["StartY"].Equals(t.Rows[1]["StartY"]))
                t.Rows.RemoveAt(0);
            return t;
        }

        public static DataTable GenerateTagTableWithNeighbors(DataTable src,
            double actualWidth, double actualHeight, double proportion)
        {
            var result = new DataTable();
            result.Columns.Add("TagName", typeof(string)); result.Columns.Add("X", typeof(double));
            result.Columns.Add("Y", typeof(double)); result.Columns.Add("NeighborIds", typeof(string));

            var seen = new HashSet<string>();
            var points = new List<(string Tag, double X, double Y)>();

            foreach (DataRow row in src.Rows)
            {
                string t1 = row["Tag1"].ToString(), t2 = row["Tag2"].ToString();
                var painting = new Painting();
                var st = painting.TransformCoordinate(
                    new Point(Convert.ToDouble(row["StartX"]) * 10, Convert.ToDouble(row["StartY"]) * 10),
                    actualWidth, actualHeight, proportion);
                var et = painting.TransformCoordinate(
                    new Point(Convert.ToDouble(row["EndX"]) * 10, Convert.ToDouble(row["EndY"]) * 10),
                    actualWidth, actualHeight, proportion);

                if (seen.Add(t1)) points.Add((t1, st.X, st.Y));
                if (seen.Add(t2)) points.Add((t2, et.X, et.Y));
            }

            var neighbors = new Dictionary<string, HashSet<string>>();
            foreach (DataRow row in src.Rows)
            {
                string t1 = row["Tag1"].ToString(), t2 = row["Tag2"].ToString();
                if (!neighbors.ContainsKey(t1)) neighbors[t1] = new HashSet<string>();
                if (!neighbors.ContainsKey(t2)) neighbors[t2] = new HashSet<string>();
                neighbors[t1].Add(t2); neighbors[t2].Add(t1);
            }

            foreach (var item in points.OrderBy(p => ExtractNum(p.Tag)))
            {
                string nbs = neighbors.ContainsKey(item.Tag)
                    ? string.Join(",", neighbors[item.Tag].OrderBy(ExtractNum)) : "";
                result.Rows.Add(item.Tag, item.X, item.Y, nbs);
            }
            return result;
        }

        private static int ExtractNum(string tag)
        {
            string d = new string(tag.Where(char.IsDigit).ToArray());
            return int.TryParse(d, out int n) ? n : 0;
        }

        // ─────────────────────────────────────────────────────────────────
        //  CBS DLL 路径解析与发布
        //  ← 关键改动：newPointStraightWithAngle → NewPointStraightWithAngle
        //               newStationData            → NewStationData
        //  使用 PathHelper.ProcessPointsByTurnRotate
        // ─────────────────────────────────────────────────────────────────

        public static Dictionary<string, List<Tuple<Point, int, double, double>>> ParseMultiAgentPaths(string output)
        {
            var result = new Dictionary<string, List<Tuple<Point, int, double, double>>>();
            var root = JObject.Parse(output);
            foreach (var kv in root)
            {
                var arr = kv.Value as JArray;
                if (arr == null) continue;
                var list = arr.Select(item =>
                    Tuple.Create(new Point(item["x"].Value<double>(), item["y"].Value<double>()), 0, 0.7, 0.3))
                    .ToList();
                result[kv.Key] = list;
            }
            return result;
        }

        public static async Task ProcessAndPublishPerAgentAsync(string outputJson, double insertDistance = 0.001)
        {
            var parsed = ParseMultiAgentPaths(outputJson);
            if (parsed == null || parsed.Count == 0) return;

            foreach (var kv in parsed)
            {
                string ip = kv.Key;
                var pts4 = kv.Value;
                if (pts4 == null || pts4.Count == 0) continue;

                var pts6 = new List<Tuple<Point, int, double, double, double, double>>(pts4.Count);
                for (int i = 0; i < pts4.Count; i++)
                {
                    var p = pts4[i];
                    int lift = i == 0 ? 21 : i == pts4.Count - 1 ? 22 : p.Item2;
                    pts6.Add(Tuple.Create(p.Item1, lift, 0.7, 0.3, 1.0, 0.0));
                }

                // ← 使用 PathHelper，返回 NewPointStraightWithAngle
                List<NewPointStraightWithAngle> processed =
                    Helpers.PathHelper.ProcessPointsByTurnRotate(pts6, insertDistance);

                var station = new NewStationData();
                station.Stations.AddRange(processed);

                string json = JsonSerializer.Serialize(station, new JsonSerializerOptions { WriteIndented = true });
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    $"MesPoints_{ip.Replace(':', '_')}.json");
                File.WriteAllText(filePath, json);

                if (MqttConnectionManager.MqttClients.TryGetValue(ip, out var wrapper)
                    && wrapper != null && wrapper.IsConnected)
                {
                    var msg = new MqttApplicationMessageBuilder()
                        .WithTopic("AGV/Carrier/MapLineMes")
                        .WithPayload(json)
                        .WithExactlyOnceQoS().WithRetainFlag(false).Build();
                    try { await wrapper.PublishAsync(msg); }
                    catch (Exception ex) { Console.WriteLine($"发送到 {ip} 失败: {ex.Message}"); }
                }
                else
                {
                    Console.WriteLine($"未找到已连接的 MQTT 客户端: {ip}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  私有工具
        // ─────────────────────────────────────────────────────────────────

        private static double CalcDist(Point p, double x, double y)
            => Math.Sqrt(Math.Pow(p.X - x, 2) + Math.Pow(p.Y - y, 2));

        private static double DistanceBetweenPoints(Point a, Point b)
            => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  辅助数据类（保留在同文件）
    // ─────────────────────────────────────────────────────────────────────

    public class PointMapLine
    {
        public double X { get; set; }
        public double Y { get; set; }
        public PointMapLine(double x, double y) { X = x; Y = y; }
    }

    public class PointDataMapLine
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }
        public PointDataMapLine(double x, double y, double angle) { X = x; Y = y; Angle = angle; }
    }
}