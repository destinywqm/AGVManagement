using AGVManagement.Enumeration;
using Microsoft.Win32;
using MQTTnet;
using MQTTnet.Client;
using MySql.Data.MySqlClient.Memcached;
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml;
using Path = System.Windows.Shapes.Path;
using System.Drawing;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using System.Data;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using AGVManagement.Mqtt;
using static AGVManagement.MainWindow;

namespace AGVManagement.MapPaint
{
    /// <summary>
    /// 地图管理
    /// </summary>
    public class Painting
    {
        public Canvas mainPan;
        private int x = 100; //X轴刻度大小
        private int y = 100; //Y轴刻度大小
        private IMqttClient _client;

        // 静态坐标数据
        private static List<Point> coordinates;

        /// <summary>
        /// 绘制直线
        /// </summary>
        /// <param name="startPt">起始点</param>
        /// <param name="endPt">结束点</param>
        /// <param name="mainPanel">绘制容器</param>
        public Path DrawingLine(Point startPt, Point endPt, Canvas mainPanel)
        {
            LineGeometry myLineGeometry = new LineGeometry();//实例化一条直线
            myLineGeometry.StartPoint = startPt;//设置起点
            myLineGeometry.EndPoint = endPt;//设置终点
            

            Path myPath = new Path();
      
            myPath.Stroke = Brushes.Black;//设置颜色
            myPath.StrokeThickness = 1; //设置宽度
            myPath.Data = myLineGeometry;//设置绘制形状
            mainPanel.Children.Add(myPath);//绑定数据
            //mainP
            return myPath;
        }

        /// <summary>
        /// 绘制半圆 
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="mainPanel"></param>
        /// <returns></returns>
        public Path DrawingSemicircle(Point startPt, Point endPt, Canvas mainPanel)
        {
            return GetPath1(startPt, endPt, mainPanel, false, 50);
        }

        public Path GetPath1(Point startPt, Point endPt, Canvas mainPanel, bool Static, int Size)
        {
            Point controlPt = new Point((startPt.X + endPt.X) / 2, (startPt.Y + endPt.Y) / 2); // 控制点

            Path path = new Path();
            PathGeometry pathGeometry = new PathGeometry();

            BezierSegment bezier = new BezierSegment(startPt, controlPt, endPt, true);
            PathFigure figure = new PathFigure();
            figure.StartPoint = startPt;
            figure.Segments.Add(bezier);
            pathGeometry.Figures.Add(figure);


            path.Data = pathGeometry;
            path.Stroke = Brushes.Black;
            mainPanel.Children.Add(path);


            // 添加鼠标事件处理程序
            path.MouseMove += (sender, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    controlPt = e.GetPosition(mainPanel);
                    bezier.Point3 = controlPt;
                }
            };

            return path;
        }

        



        public Path GetPath(Point startPt, Point endPt, Canvas mainPanel, bool Static, int Size)
        {
            Path path = new Path();
            PathGeometry pathGeometry = new PathGeometry();
            ArcSegment arc = new ArcSegment(startPt, new Size(Size, Size), 0, false, Static ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true);
            PathFigure figure = new PathFigure();
            figure.StartPoint = endPt;
            figure.Segments.Add(arc);
            pathGeometry.Figures.Add(figure);
            path.Data = pathGeometry;
            path.Stroke = Brushes.Black;
            mainPanel.Children.Add(path);

            // 添加鼠标事件处理程序
            path.MouseLeftButtonDown += Path_MouseLeftButtonDown;
            path.MouseMove += Path_MouseMove;
            path.MouseLeftButtonUp += Path_MouseLeftButtonUp;
            return path;
        }

        public List<Point> GetPointsOnArc(Point startPt, Point endPt, bool isClockwise, int radius, double angleIncrement)
        {
            List<Point> pointsOnArc = new List<Point>();

            double angle = Math.Atan2(endPt.Y - startPt.Y, endPt.X - startPt.X);
            double startAngle = isClockwise ? angle : angle + Math.PI;
            double endAngle = isClockwise ? angle + Math.PI : angle;

            for (double a = startAngle; isClockwise ? a < endAngle : a > endAngle; a = isClockwise ? a + angleIncrement : a - angleIncrement)
            {
                double x = startPt.X + radius * Math.Cos(a);
                double y = startPt.Y + radius * Math.Sin(a);
                pointsOnArc.Add(new Point(x, y));
            }

            return pointsOnArc;
        }

        // 获取扫描角度
        private static double GetSweepAngle(ArcSegment arcSegment, double startAngle)
        {
            Point startPoint = arcSegment.Point;
            Point endPoint = arcSegment.Point + new Vector(arcSegment.Size.Width * Math.Cos(startAngle), arcSegment.Size.Height * Math.Sin(startAngle));

            Vector vector1 = startPoint - new Point(arcSegment.Size.Width, arcSegment.Size.Height);
            Vector vector2 = endPoint - new Point(arcSegment.Size.Width, arcSegment.Size.Height);

            double angle1 = Math.Atan2(vector1.Y, vector1.X) * (180 / Math.PI);
            double angle2 = Math.Atan2(vector2.Y, vector2.X) * (180 / Math.PI);

            return angle2 - angle1;
        }

        private List<Point> GetPointsOnArc(double startX, double startY, double endX, double endY, double sizeX, double sizeY, double rotationAngle, bool isLargeArc, bool isClockwise)
        {
            List<Point> points = new List<Point>();

            // 计算弧线的中心点坐标
            double centerX = (startX + endX) / 2;
            double centerY = (startY + endY) / 2;

            // 计算起始点和终止点相对于中心点的偏移
            double offsetX = startX - centerX;
            double offsetY = startY - centerY;

            // 计算起始角度和终止角度
            double startAngle = Math.Atan2(offsetY, offsetX);
            double endAngle = Math.Atan2(endY - centerY, endX - centerX);

            // 调整起始角度和终止角度，使其在 0 到 2π 之间
            if (startAngle < 0)
                startAngle += 2 * Math.PI;
            if (endAngle < 0)
                endAngle += 2 * Math.PI;

            // 根据顺时针或逆时针方向调整起始角度和终止角度
            if (isClockwise && startAngle < endAngle)
                startAngle += 2 * Math.PI;
            else if (!isClockwise && startAngle > endAngle)
                endAngle += 2 * Math.PI;

            // 计算弧线上的点
            int numPoints = 100; // 调整此值以控制点的数量
            double angleStep = (endAngle - startAngle) / numPoints;
            for (int i = 0; i <= numPoints; i++)
            {
                double angle = startAngle + i * angleStep;

                // 计算点的坐标
                double x = centerX + sizeX * Math.Cos(angle) * Math.Cos(rotationAngle) - sizeY * Math.Sin(angle) * Math.Sin(rotationAngle);
                double y = centerY + sizeX * Math.Cos(angle) * Math.Sin(rotationAngle) + sizeY * Math.Sin(angle) * Math.Cos(rotationAngle);

                points.Add(new Point(x, y));
            }

            return points;
        }

        private bool isDragging = false;
        private Point lastMousePosition;

        private void Path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Path path = (Path)sender;
            isDragging = true;
            Canvas mainPanel = (Canvas)path.Parent;
            lastMousePosition = e.GetPosition(mainPanel);
            path.CaptureMouse();

        }

        private void Path_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Path path = (Path)sender;
                Canvas mainPanel = (Canvas)path.Parent;
                Point mousePosition = e.GetPosition(mainPanel);
                Vector offset = mousePosition - lastMousePosition;

                // 更新半圆的位置
                PathGeometry pathGeometry = (PathGeometry)path.Data;
                PathFigure figure = pathGeometry.Figures[0];
                ArcSegment arc = (ArcSegment)figure.Segments[0];
                arc.Point = new Point(arc.Point.X + offset.X, arc.Point.Y + offset.Y);

                lastMousePosition = mousePosition;
            }
        }

        private void Path_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Path path = (Path)sender;
            isDragging = false;
            path.ReleaseMouseCapture();
        }


        /// <summary>
        /// 绘制可移动小车
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="mainPanel"></param>
        /// <returns></returns>

        private Rectangle carRect;
        private double mapWidth, mapHeight;
        //private Image carImage;
        //private double mapWidth, mapHeight;
        public Rectangle CreateCar(double mapWidth, double mapHeight, Canvas mainPanel)
        {
            mainPanel.KeyDown += MainPanel_KeyDown;

            mainPanel.Focusable = true;
            mainPanel.Focus();
            Debug.WriteLine($"mainPanel Focusable: {mainPanel.Focusable}, HasKeyDownEvent: {Keyboard.IsKeyDown(Key.Left)}");

            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;

            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
            carRect = new Rectangle();
            carRect.Width = 10;
            carRect.Height = 10;
            carRect.Fill = Brushes.Red; // 矩形填充颜色

            // 设置初始位置为地图中心
            Canvas.SetLeft(carRect, (mainPanel.ActualWidth - carRect.Width) / 2);
            Canvas.SetTop(carRect, (mainPanel.ActualHeight - carRect.Height) / 2);

            mainPanel.Children.Add(carRect);

            return carRect;
            //carImage = new Image();
            //carImage.Width = 50;
            //carImage.Height = 30;
            //carImage.Stretch = Stretch.Fill; // 图片填充模式

            //BitmapImage bitmap = new BitmapImage();
            //bitmap.BeginInit();
            //bitmap.UriSource = new Uri("car.png", UriKind.Relative); // 车辆图片路径
            //bitmap.EndInit();
            //carImage.Source = bitmap;

            // 设置初始位置为地图中心
            //Canvas.SetLeft(carImage, (mainPanel.ActualWidth - carImage.Width) / 2);
            //Canvas.SetTop(carImage, (mainPanel.ActualHeight - carImage.Height) / 2);

            //mainPanel.Children.Add(carImage);

            //return carImage;
        }

        private void MainPanel_KeyDown(object sender, KeyEventArgs e)
        {
            // 获取当前矩形的位置
            double currX = Canvas.GetLeft(carRect);
            double currY = Canvas.GetTop(carRect);

            switch (e.Key)
            {
                case Key.Left:
                    Canvas.SetLeft(carRect, currX - 10);
                    break;
                case Key.Right:
                    Canvas.SetLeft(carRect, currX + 10);
                    break;
                case Key.Up:
                    Canvas.SetTop(carRect, currY - 10);
                    break;
                case Key.Down:
                    Canvas.SetTop(carRect, currY + 10);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 绘制曲线
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="controlPt"></param>
        /// <param name="mainPanel"></param>
        /// <returns></returns>
        /// 

        private Path path;
        private Point startPt;
        private Point endPt;
        private Point controlPt0;
        private bool isDragging1;
    
        public Path DrawingQuadraticBezierCurve(Point startPt, Point endPt, Canvas mainPanel)
        {
            this.startPt = startPt;
            this.endPt = endPt;
            this.controlPt0 = new Point((startPt.X + endPt.X +100 ) / 2, (startPt.Y + endPt.Y +100) / 2);

            path = GetQuadraticBezierCurve(startPt, endPt, controlPt0, mainPanel);

            mainPanel.MouseLeftButtonDown += MainPanel_MouseLeftButtonDown;
            mainPanel.MouseLeftButtonUp += MainPanel_MouseLeftButtonUp;
            mainPanel.MouseMove += MainPanel_MouseMove;
            
            

            return path;
        }

        public Path GetQuadraticBezierCurve(Point startPt, Point endPt, Point controlPt, Canvas mainPanel)
        {
            Path path = new Path();
            PathGeometry pathGeometry = new PathGeometry();

            QuadraticBezierSegment bezierSegment = new QuadraticBezierSegment();
            bezierSegment.Point1 = controlPt;
            bezierSegment.Point2 = endPt;

            PathFigure figure = new PathFigure();
            figure.StartPoint = startPt;
            figure.Segments.Add(bezierSegment);

            pathGeometry.Figures.Add(figure);
            path.Data = pathGeometry;
            path.Stroke = Brushes.Black;
            mainPanel.Children.Add(path);

            // 在控制点周围绘制一个圆形来高亮显示
            Ellipse highlightEllipse = new Ellipse();
            highlightEllipse.Width = 10;
            highlightEllipse.Height = 10;
            highlightEllipse.Fill = Brushes.Red; // 可以根据需要选择颜色
            Canvas.SetLeft(highlightEllipse, controlPt.X - 5);
            Canvas.SetTop(highlightEllipse, controlPt.Y - 5);
            mainPanel.Children.Add(highlightEllipse);

            return path;
        }

        private void MainPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition((Canvas)sender);
            //isDragging1 = false;
            //MessageBox.Show("nihao");
            if (isDragging1)
            {
                isDragging1 = false;
                return;
            }

            if (Math.Abs(mousePos.X - controlPt0.X) < 100 && Math.Abs(mousePos.Y - controlPt0.Y) < 100)
            {
                isDragging1 = true;
                return;
            }

        }

        private void MainPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging1)
            {
                Point mousePos = e.GetPosition((Canvas)sender);
                UpdateQuadraticBezierCurve(mousePos);
                isDragging1 = false;
            }
            //MessageBox.Show("move 结束");
        }

        private void MainPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging1)
            {
                
                isDragging1 = false;
            }
            //MessageBox.Show("AAA");
        }

        private void UpdateQuadraticBezierCurve(Point newControlPt)
        {
            controlPt0 = newControlPt;

            PathGeometry pathGeometry = (PathGeometry)path.Data;
            QuadraticBezierSegment bezierSegment = (QuadraticBezierSegment)((PathFigure)pathGeometry.Figures[0]).Segments[0];
            bezierSegment.Point1 = controlPt0;

            // 更新曲线的形状
            path.InvalidateVisual();
        }
        /// <summary>
        /// 绘制半圆线路
        /// </summary>
        /// <param name="mainPanel"></param>
        //public Path DrawingSemicircle(Point startPt, Point endPt, Canvas mainPanel)
        //{
        //    return GetPath(startPt, endPt, mainPanel, false, 0);
        //}

        /// <summary>
        /// 半圆
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="mainPanel"></param>
        /// <param name="Static"></param>
        /// <param name="Sise"></param>
        /// <returns></returns>
        //public Path GetPath(Point startPt, Point endPt, Canvas mainPanel, bool Static, int Sise)
        //{
        //    Path path = new Path();
        //    PathGeometry pathGeometry = new PathGeometry();

        //    Point controlPt = CalculateControlPoint(startPt, endPt, Sise);

        //    BezierSegment bezier = new BezierSegment(startPt, controlPt, endPt, true);
        //    PolyBezierSegment polyBezier = new PolyBezierSegment();
        //    polyBezier.Points.Add(startPt);
        //    polyBezier.Points.Add(controlPt);
        //    polyBezier.Points.Add(endPt);

        //    PathFigure figure = new PathFigure();
        //    figure.StartPoint = startPt;
        //    figure.Segments.Add(bezier);
        //    figure.Segments.Add(polyBezier);
        //    pathGeometry.Figures.Add(figure);

        //    path.Data = pathGeometry;
        //    path.Stroke = Brushes.Black;
        //    mainPanel.Children.Add(path);

        //    return path;
        //}

        //private Point CalculateControlPoint(Point startPt, Point endPt, int size)
        //{
        //    double controlX = (startPt.X + endPt.X) / 2;
        //    double controlY = (startPt.Y - size > endPt.Y) ? startPt.Y - size : endPt.Y - size;

        //    return new Point(controlX, controlY);
        //}

        /// <summary>
        /// 获取地图list坐标
        /// </summary>
        /// <returns></returns>
        public List<Point> GetCoordinatesFromFile()
        {
            // 如果已经读取过坐标数据，则直接返回
            if (coordinates != null)
                return coordinates;

            // 否则，读取文件获取坐标数据
            // 打开文件对话框并选择文本文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "文本文件 (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() == true)
            {
                // 读取文本文件的所有行
                string[] lines = File.ReadAllLines(openFileDialog.FileName);

                // 创建坐标列表
                coordinates = new List<Point>();

                int angleIndex = 0; // 角度索引

                foreach (string line in lines)
                {
                    // 按逗号分割每行数据
                    string[] values = line.Split(',');

                    foreach (string value in values)
                    {
                        // 检查数据是否有效，且为 double 类型
                        if (double.TryParse(value, out double distance))
                        {
                            // 计算每次0.8度偏转的角度，并转换为弧度
                            double angle = angleIndex * 0.8;
                            double radian = angle * Math.PI / 180.0;

                            // 根据距离和角度计算xy坐标点
                            double x = distance * Math.Cos(radian);
                            double y = distance * Math.Sin(radian);

                            // 将坐标点添加到列表中
                            coordinates.Add(new Point(x, y));

                            angleIndex++;
                        }
                    }
                }
            }

            return coordinates;
        }

        private string message = "0";
        /// <summary>
        /// 处理mqtt传输回来的数据
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public List<Point> GetCoordinatesFromMqttMessage()
        {
            if (_client != null)
            {
                // 订阅AGV/Response/MapDrawing主题
                string topic = "AGV/Response/MapDrawing";
                _client.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(topic)
                    .Build());

                // 设置消息接收处理器
                _client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args =>
                {
                    if (args.ApplicationMessage.Topic == topic)
                    {
                        string message = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
                        //MessageBox.Show(receiveMessage, "收到消息");
                        // 如果已经读取过坐标数据，则直接返回
                        // 从字符串中读取坐标数据
                        string[] lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        //MessageBox.Show(message,"更新后的message");

                        // 创建坐标列表
                        List<Point> coordinates = new List<Point>();

                        int angleIndex = 0; // 角度索引

                        foreach (string line in lines)
                        {
                            // 按逗号分割每行数据
                            string[] values = line.Split(',');

                            foreach (string value in values)
                            {
                                // 检查数据是否有效，且为 double 类型
                                if (double.TryParse(value, out double distance))
                                {
                                    // 计算每次0.8度偏转的角度，并转换为弧度
                                    double angle = angleIndex * 0.8;
                                    double radian = angle * Math.PI / 180.0;

                                    // 根据距离和角度计算xy坐标点
                                    double x = distance * Math.Cos(radian);
                                    double y = distance * Math.Sin(radian);

                                    // 将坐标点添加到列表中
                                    coordinates.Add(new Point(x, y));

                                    angleIndex++;
                                }
                            }

                        }
                    }
                });
            }
            return coordinates;

        }

        /// <summary>
        /// 绘制地图
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="coordinates"></param>
        /// <param name="scaleFactor"></param>
        public void DrawDots(Canvas canvas, List<Point> coordinates, double scaleFactor)
        {
            if (coordinates != null)
            {

                // 获取画布中心的坐标
                double centerX = canvas.ActualWidth / 2;
                double centerY = canvas.ActualHeight / 2;

                foreach (Point point in coordinates)
                {
                    // 根据比例尺进行放大缩小操作
                    double scaledX = point.X * scaleFactor*100;
                    double scaledY = point.Y * scaleFactor*100;

                    // 转换坐标原点为画布中心
                    double translatedX = centerX + scaledX;
                    double translatedY = centerY - scaledY;

                    // 创建圆形
                    Ellipse ellipse = new Ellipse();
                    ellipse.Width = 2;
                    ellipse.Height = 2;
                    ellipse.Fill = Brushes.Red;
                        
                    // 设置圆形的位置
                    double circleX = translatedX - (ellipse.Width / 2);
                    double circleY = translatedY - (ellipse.Height / 2);
                    Canvas.SetLeft(ellipse, circleX);
                    Canvas.SetTop(ellipse, circleY);

                    // 将圆形添加到画布上
                    canvas.Children.Add(ellipse);
                }
            }
        }

        /// <summary>
        /// 点位绘制
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="points"></param>
        public static void DrawPointsOnCanvasTest(Canvas canvas, List<Point> points)
        {
            foreach (Point point in points)
            {
                // 创建 Ellipse 作为点的图形表示
                Ellipse ellipse = new Ellipse
                {
                    Width = 1,
                    Height = 1,
                    Fill = Brushes.Blue // 选择填充颜色
                };

                // 设置点的位置
                Canvas.SetLeft(ellipse, point.X);
                Canvas.SetTop(ellipse, point.Y);

                // 将 Ellipse 添加到 Canvas 中
                canvas.Children.Add(ellipse);
            }
        }

        /// <summary>
        /// 小车变化绘制地图
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="coordinates"></param>
        /// <param name="scaleFactor"></param>
        public void DrawDotsNow(Canvas canvas, List<Point> coordinates, double scaleFactor, Point originPoint)
        {
            if (coordinates != null)
            {

                // 获取画布中心的坐标
                double centerX = canvas.ActualWidth / 2;
                double centerY = canvas.ActualHeight / 2;

                // 转换originPoint的原点为画布中心
                double translatedOriginX = centerX + (originPoint.X - centerX);
                double translatedOriginY = centerY + (originPoint.Y - centerY);
                foreach (Point point in coordinates)
                {
                    // 根据比例尺进行放大缩小操作，并以传入的originPoint为原点
                    double scaledX = (point.X - translatedOriginX) * scaleFactor * 100;
                    double scaledY = (point.Y - translatedOriginY) * scaleFactor * 100;

                    // 转换坐标原点为画布中心
                    double translatedX = centerX + scaledX;
                    double translatedY = centerY - scaledY;

                    // 创建圆形
                    Ellipse ellipse = new Ellipse();
                    ellipse.Width = 2;
                    ellipse.Height = 2;
                    ellipse.Fill = Brushes.Red;

                    // 设置圆形的位置
                    double circleX = translatedX - (ellipse.Width / 2);
                    double circleY = translatedY - (ellipse.Height / 2);
                    Canvas.SetLeft(ellipse, circleX);
                    Canvas.SetTop(ellipse, circleY);

                    // 将圆形添加到画布上
                    canvas.Children.Add(ellipse);
                }
            }
        }
        /// <summary>
        /// 实时更新小车位置
        /// </summary>
        private Rectangle rectangle;
        public void DrawRectangle(Canvas canvas, Point position, double width, double height, double angle)
        {
            // 获取画板的宽度和高度
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            position.Y = -position.Y;

            // 计算圆点的位置
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;
            double left = centerX + position.X - width / 2;
            double top = centerY + position.Y - height / 2;

            // 如果矩形元素不存在，则创建一个新的矩形元素并添加到画布中
            if (rectangle == null)
            {
                // 更新矩形的位置和大小
                rectangle = new Rectangle();
                rectangle.Width = width;
                rectangle.Height = height;

                // 创建线性渐变画笔
                LinearGradientBrush brush = new LinearGradientBrush();
                brush.StartPoint = new Point(0, 0.5);
                brush.EndPoint = new Point(1, 0.5);
                brush.GradientStops.Add(new GradientStop(Colors.Blue, 0));
                brush.GradientStops.Add(new GradientStop(Colors.Red, 1));
                rectangle.Fill = brush;

                canvas.Children.Add(rectangle);
            }

            // 更新矩形的位置和大小
            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            rectangle.Width = width;
            rectangle.Height = height;
            double nowAngle = - angle;

            // 应用旋转变换
            RotateTransform rotateTransform = new RotateTransform(nowAngle, width / 2, height / 2);
            rectangle.RenderTransform = rotateTransform;
        }

        /// <summary>
        /// 绘制折线线路
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="mainPanel"></param>
        public List<Path> DrawingBroken(Point startPt, Point endPt, Canvas mainPanel)
        {
            List<Path> GetPatjs = new List<Path>();
            double drn = startPt.X - endPt.X;
            double hrn = startPt.Y - endPt.Y;

            if ((drn > 0 && hrn < 0) || (drn < 0 && hrn > 0))
            {
                double diff = startPt.Y - endPt.Y;
                if (diff < 0)
                {
                    //Point TX = new Point() { X = startPt.X, Y = endPt.Y-10 };
                    //Point TY = new Point() { X = startPt.X-10, Y = endPt.Y };

                    Point TX = new Point() { X = endPt.X + 20, Y = startPt.Y };
                    Point TY = new Point() { X = endPt.X, Y = startPt.Y + 20 };
                    GetPatjs.Add(DrawingLine(endPt, TY, mainPanel));
                    GetPatjs.Add(GetPath(TX, TY, mainPanel, true, 20));
                    GetPatjs.Add(DrawingLine(startPt, TX, mainPanel));
                }
                else
                {
                    Point TX = new Point() { X = endPt.X - 20, Y = startPt.Y };
                    Point TY = new Point() { X = endPt.X, Y = startPt.Y - 20 };
                    GetPatjs.Add(DrawingLine(startPt, TX, mainPanel));
                    GetPatjs.Add(GetPath(TX, TY, mainPanel, true, 20));
                    GetPatjs.Add(DrawingLine(endPt, TY, mainPanel));
                }
            }
            else
            {
                double diff = startPt.Y - endPt.Y;
                if (diff < 0)
                {
                    Point TX = new Point() { X = endPt.X - 20, Y = startPt.Y };
                    Point TY = new Point() { X = endPt.X, Y = startPt.Y + 20 };
                    GetPatjs.Add(DrawingLine(startPt, TX, mainPanel));
                    GetPatjs.Add(GetPath(TX, TY, mainPanel, false, 20));
                    GetPatjs.Add(DrawingLine(endPt, TY, mainPanel));

                }
                else
                {
                    Point TX = new Point() { X = endPt.X + 20, Y = startPt.Y };
                    Point TY = new Point() { X = endPt.X, Y = startPt.Y - 20 };
                    GetPatjs.Add(DrawingLine(endPt, TY, mainPanel));
                    GetPatjs.Add(GetPath(TX, TY, mainPanel, false, 20));
                    GetPatjs.Add(DrawingLine(startPt, TX, mainPanel));
                }
            }
            
            return GetPatjs;
        }

        /// <summary>
        /// 折线点位输出
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="mainPanel"></param>
        /// <returns></returns>
        public List<Point> DrawingBrokenPoint(Point startPt, Point endPt)
        {
            List<Point> GetPatjs = new List<Point>();
            double drn = startPt.X - endPt.X;
            double hrn = startPt.Y - endPt.Y;

            //40
            if ((drn > 0 && hrn < 0) || (drn < 0 && hrn > 0))
            {
                double diff = startPt.Y - endPt.Y;
                if (diff < 0)
                {
                    Point TX = new Point() { X = startPt.X, Y = endPt.Y - 63.1575 };
                    Point TY = new Point() { X = startPt.X - 63.1575, Y = endPt.Y };
                    Point CenterPoint = new Point() { X = startPt.X - 63.1575, Y = endPt.Y - 63.1575 };

                    //原本的点位反了（已注释）
                    /*Point TX = new Point() { X = endPt.X + 30, Y = startPt.Y };
                    Point TY = new Point() { X = endPt.X, Y = startPt.Y + 30 };
                    Point CenterPoint = new Point() { X = endPt.X + 30, Y = startPt.Y + 30 };*/

                    GetPatjs = GetLinePoints(startPt, TX);
                    GetPatjs.AddRange(GetArcPointsByRadian(TX, TY, CenterPoint, 63.1575));
                    //GetPatjs.AddRange(GetArcPoints(TX, TY, CenterPoint, 30));
                    GetPatjs.AddRange(GetLinePoints(TY, endPt));
                }
                else
                {
                    Point TX = new Point() { X = startPt.X, Y = endPt.Y + 63.1575 };
                    Point TY = new Point() { X = startPt.X + 63.1575, Y = endPt.Y };
                    Point CenterPoint = new Point() { X = startPt.X + 63.1575, Y = endPt.Y + 63.1575 };

                    //原本的点位反了（已注释）
                    //Point TX = new Point() { X = endPt.X - 30, Y = startPt.Y };
                    //Point TY = new Point() { X = endPt.X, Y = startPt.Y - 30 };
                    //Point CenterPoint = new Point() { X = endPt.X - 30, Y = startPt.Y - 30 };
                    GetPatjs = GetLinePoints(startPt, TX);
                    GetPatjs.AddRange(GetArcPointsByRadian(TX, TY, CenterPoint, 63.1575));
                    //GetPatjs.AddRange(GetArcPoints(TX, TY, CenterPoint, 30));
                    GetPatjs.AddRange(GetLinePoints(TY, endPt));

                }
            }
            else
            {
                double diff = startPt.Y - endPt.Y;
                if (diff < 0)
                {
                    Point TX = new Point() { X = endPt.X - 63.1575, Y = startPt.Y };
                    Point TY = new Point() { X = endPt.X, Y = startPt.Y + 63.1575 }; 
                    Point CenterPoint = new Point() { X = endPt.X - 63.1575, Y = startPt.Y + 63.1575 };
                    GetPatjs = GetLinePoints(startPt, TX);
                    GetPatjs.AddRange(GetArcPointsByRadian(TX, TY, CenterPoint, 63.1575));
                    //GetPatjs.AddRange(GetArcPoints(TX, TY, CenterPoint, 30));
                    GetPatjs.AddRange(GetLinePoints(TY, endPt));
                }
                else
                {
                    Point TX = new Point() { X = endPt.X + 63.1575, Y = startPt.Y };
                    Point TY = new Point() { X = endPt.X, Y = startPt.Y - 63.1575 };
                    Point CenterPoint = new Point() { X = endPt.X + 63.1575, Y = startPt.Y - 63.1575 };
                    GetPatjs = GetLinePoints(startPt, TX);
                    GetPatjs.AddRange(GetArcPointsByRadian(TX, TY, CenterPoint, 63.1575));
                    //GetPatjs.AddRange(GetArcPoints(TX, TY, CenterPoint, 30));
                    GetPatjs.AddRange(GetLinePoints(TY, endPt));

                    //原本的点位反了（已注释）
                    //GetPatjs = GetLinePoints(endPt, TY);
                    //GetPatjs.AddRange(GetArcPoints(TX, TY, CenterPoint, 30));
                    //GetPatjs.AddRange(GetLinePoints(startPt, TX));
                }
            }

            return GetPatjs;
        }

        /// <summary>
        /// 依据点绘制路径
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="points"></param>
        public void DrawPointsOnCanvasBySlam(Canvas canvas, List<Point> points)
        {
            foreach (Point point in points)
            {
                // 创建一个圆形作为点的可视化
                Ellipse ellipse = new Ellipse
                {
                    Width = 1,    // 设置圆形的宽度
                    Height = 1,   // 设置圆形的高度
                    Fill = Brushes.Black  // 设置圆形的填充颜色为黑色
                };

                // 将圆形添加到 Canvas 中并设置其位置
                Canvas.SetLeft(ellipse, point.X - 3);   // 减去一半的宽度以使点的中心位于指定坐标
                Canvas.SetTop(ellipse, point.Y - 3);    // 减去一半的高度以使点的中心位于指定坐标
                canvas.Children.Add(ellipse);           // 将圆形添加到 Canvas 的子元素中
            }
        }


        /// <summary>
        /// 坐标转换
        /// </summary>
        /// <param name="points"></param>
        /// <param name="newOriginX"></param>
        /// <param name="newOriginY"></param>
        /// <returns></returns>
        public List<Point> TransformCoordinates(List<Point> points, int newOriginX, int newOriginY, double factor)
        {
            List<Point> transformedPoints = new List<Point>();
            foreach (Point point in points)
            {
                //double newX = Math.Round((point.X - newOriginX) / factor, 4);
                //double newY = Math.Round((newOriginY - point.Y) / factor, 4);
                double newX = (point.X - newOriginX) / factor;
                double newY = (newOriginY - point.Y) / factor;
                transformedPoints.Add(new Point(newX, newY));
            }
            return transformedPoints;
        }

        /// <summary>
        /// 反向转换
        /// </summary>
        /// <param name="points"></param>
        /// <param name="originalOriginX"></param>
        /// <param name="originalOriginY"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public List<Point> ReverseTransformCoordinates(List<Point> points, int originalOriginX, int originalOriginY, double factor)
        {
            List<Point> originalPoints = new List<Point>();
            foreach (Point point in points)
            {
                // 使用反向公式计算原始坐标
                double originalX = (point.X * factor) + originalOriginX;
                double originalY = (originalOriginY - (point.Y * factor));

                originalPoints.Add(new Point(originalX, originalY));
            }
            return originalPoints;
        }

        //单点转换
        public Point ReverseTransformCoordinate(Point point, double originalOriginX, double originalOriginY, double factor)
        {
            // 使用反向公式计算原始坐标
            double originalX = (point.X * factor) + originalOriginX;
            double originalY = (originalOriginY - (point.Y * factor));

            return new Point(originalX, originalY);
        }

        //单个点位坐标转换
        public Point TransformCoordinate(Point point, double newOriginX, double newOriginY, double factor)
        {
            // 计算转换后的坐标
            double newX = (point.X - newOriginX) / factor;
            double newY = (newOriginY - point.Y) / factor;

            // 返回新的单个点
            return new Point(newX, newY);
        }

        //防止mes起点就在那个自动寻路点上
        public List<Tuple<Point, int, double, double>> RemoveRedundantRoutes(List<Tuple<Point, int, double, double>> points)
        {
            // 检查列表长度，至少要有三个点才做判断
            while (points.Count >= 3)
            {
                // 比较第一个点和第三个点的坐标是否相同
                if (points[0].Item1.Equals(points[2].Item1))
                {
                    // 如果相同，删除前两个点
                    points.RemoveAt(0); // 删除第一个点
                    points.RemoveAt(0); // 删除新的第二个点 (原列表中的第三个点)
                }
                else
                {
                    // 如果不相同，跳出循环
                    break;
                }
            }

            return points;
        }

        public List<Tuple<Point, int, double, double>> TransformCoordinatesByRotate(List<Tuple<Point, int, double, double>> stagingTagPoint, double newOriginX, double newOriginY, double factor)
        {
            List<Tuple<Point, int, double, double>> transformedPoints = new List<Tuple<Point, int, double, double>>();
            foreach (var item in stagingTagPoint)
            {
                Point point = item.Item1;
                int value = item.Item2;
                double value1 = item.Item3;
                double value2 = item.Item4;

                double newX = (point.X - newOriginX) / factor;
                double newY = (newOriginY - point.Y) / factor;

                transformedPoints.Add(new Tuple<Point, int, double, double>(new Point(newX, newY), value, value1, value2));
            }
            return transformedPoints;
        }




        //多一个Turn的转换
        public List<Tuple<Point, int, double, double, double, double>> TransformCoordinatesByTurnRotate(List<Tuple<Point, int, double, double, double, double>> stagingTagPoint, double newOriginX, double newOriginY, double factor)
        {
            List<Tuple<Point, int, double, double, double, double>> transformedPoints = new List<Tuple<Point, int, double, double, double, double>>();
            foreach (var item in stagingTagPoint)
            {
                Point point = item.Item1;
                int value = item.Item2;
                double value1 = item.Item3;
                double value2 = item.Item4;
                double value3 = item.Item5;
                double value4 = item.Item6;

                double newX = (point.X - newOriginX) / factor;
                double newY = (newOriginY - point.Y) / factor;

                transformedPoints.Add(new Tuple<Point, int, double, double, double, double>(new Point(newX, newY), value, value1, value2, value3, value4));
            }
            return transformedPoints;
        }

        /// <summary>
        /// 坐标转换
        /// </summary>
        /// <param name="points"></param>
        /// <param name="newOriginX"></param>
        /// <param name="newOriginY"></param>
        /// <returns></returns>
        public List<Point> TransformCoordinatesNoFactor(List<Point> points, int newOriginX, int newOriginY)
        {
            List<Point> transformedPoints = new List<Point>();
            foreach (Point point in points)
            {
                double newX = Math.Round((point.X - newOriginX), 4);
                double newY = Math.Round((newOriginY - point.Y), 4);
                transformedPoints.Add(new Point(newX, newY));
            }
            return transformedPoints;
        }


        /// <summary>
        /// 坐标反向转换（依据实际坐标对原本坐标转化为调度地图坐标）
        /// </summary>
        /// <param name="transformedPoints"></param>
        /// <param name="newOriginX"></param>
        /// <param name="newOriginY"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public List<Point> InverseTransformCoordinates(List<Point> transformedPoints, int newOriginX, int newOriginY, double factor)
        {
            List<Point> originalPoints = new List<Point>();
            foreach (Point point in transformedPoints)
            {
                double originalX = Math.Round((point.X * factor) + newOriginX, 4);
                double originalY = Math.Round(newOriginY - (point.Y * factor), 4);
                originalPoints.Add(new Point(originalX, originalY));
            }
            return originalPoints;
        }

        /// <summary>
        /// 获取直线的点位
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public List<Point> GetLinePoints(Point startPoint, Point endPoint)
        {
            List<Point> points = new List<Point>();

            // 计算起始点和终止点之间的距离
            double distance = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));

            // 计算步长
            double step = 0.1;

            // 计算方向向量
            double dx = (endPoint.X - startPoint.X) / distance * step;
            double dy = (endPoint.Y - startPoint.Y) / distance * step;

            // 计算所有点位
            double x = startPoint.X;
            double y = startPoint.Y;
            while (distance > 0)
            {
                points.Add(new Point(x, y));
                x += dx;
                y += dy;
                distance -= step;
            }

            // 返回所有点位
            return points;
        }

        /// <summary>
        /// 半圆的点
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point> GetUpperHalfCirclePoints(Point center, double radius)
        {
            List<Point> points = new List<Point>();

            double x = center.X - radius;
            while (x < center.X + radius)
            {
                double y = center.Y + Math.Sqrt(radius * radius - (x - center.X) * (x - center.X));
                points.Add(new Point(x, y));
                x += 0.001;
            }

            return points;
        }

        /// <summary>
        /// 获得弧线点位
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public List<Point> GetPointsOnCurve(Point startPoint, Point endPoint)
        {
            MessageBox.Show(startPoint.ToString());
            MessageBox.Show(endPoint.ToString());
            double controlDistance = Math.Abs(endPoint.X - startPoint.X) / 2;

            // 计算控制点的坐标
            Point controlPoint = new Point(startPoint.X + controlDistance, startPoint.Y);

            List<Point> points = new List<Point>();

            // 计算步数，根据起始点和终止点的横坐标差值确定
            int steps = (int)Math.Ceiling(Math.Abs(endPoint.X - startPoint.X) / 0.1);

            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                // 参数方程计算
                double x = (1 - t) * (1 - t) * startPoint.X + 2 * (1 - t) * t * controlPoint.X + t * t * endPoint.X;
                double y = (1 - t) * (1 - t) * startPoint.Y + 2 * (1 - t) * t * startPoint.Y + t * t * endPoint.Y;

                points.Add(new Point(x, y));
            }

            return points;
        }

        /// <summary>
        /// 输出1/4圆上的点
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point> GetArcPoints(Point startPt, Point endPt, Point center, double radius)
        {
            Painting painting = new Painting();
            List<Point> points = new List<Point>();  // 创建一个空的 Point 列表，用于存储坐标点
            bool identifying = false;
            double startX = startPt.X;
            double endX = endPt.X;
            if (startX > endX)
            {
                startX = endPt.X;
                endX = startPt.X;
                identifying = true;
            }

            double x = startX;
            while (x <= endX)
            {
                double distance = radius * radius - (x - center.X) * (x - center.X);
                if (distance >= 0)
                {
                    double y = 0;
                    double y1 = center.Y + Math.Sqrt(distance);
                    double y2 = center.Y - Math.Sqrt(distance);
                    // 根据圆心位置选择合适的 y 值
                    if(center.Y < startPt.Y)
                    {
                        y = y1;
                    }
                    else if(center.Y > startPt.Y)
                    {
                        y = y2;
                        //if(center.Y > endPt.Y) { y = y2; }
                        //if((center.Y == endPt.Y) && (center.X > startX))
                        //    { y = y2; }
                        //if ((center.Y == endPt.Y) && (center.X < startX))
                        //{ y = y1; }
                    }   
                    else if(center.X > startX)
                    {
                        y = y2;
                    }   
                    else 
                    {
                        y = y1;
                    }
                    points.Add(new Point(x, y));
                }
                x += 0.005;
            }

            if (identifying == true)
            {
                points = painting.ReverseList(points);
            }
            return points;
        }

        /// <summary>
        /// 依据角度输出点位
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point> GetArcPointsByRadian(Point startPt, Point endPt, Point center, double radius)
        {
            List<Point> points = new List<Point>();

            // 计算起始点和结束点的角度
            double startAngle = Math.Atan2(startPt.Y - center.Y, startPt.X - center.X);
            double endAngle = Math.Atan2(endPt.Y - center.Y, endPt.X - center.X);

            // 确保角度在0到2π之间
            if (endAngle < startAngle)
            {
                endAngle += 2 * Math.PI;
            }

            // 计算圆弧上的点
            double angle = startAngle;
            while (angle <= endAngle)
            {
                double x = center.X + radius * Math.Cos(angle);
                double y = center.Y + radius * Math.Sin(angle);
                points.Add(new Point(x, y));
                angle += 0.1;  // 根据需要调整步长
            }

            return points;
        }

        /// <summary>
        /// 文件传入
        /// </summary>
        /// <param name="points"></param>
        /// <param name="filePath"></param>
        public void SavePointsToFile(List<Point> points, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (Point point in points)
                {

                    writer.WriteLine($"{point.X} {point.Y}");
                }
                //MessageBox.Show("保存成功");
            }
        }

        /// <summary>
        /// 文件传入（带角度）
        /// </summary>
        /// <param name="points"></param>
        /// <param name="filePath"></param>
        public void SavePointsToFileByAngle(List<PointDataMapLine> pointsByAngle, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var pointData in pointsByAngle)
                {

                    writer.WriteLine($"{pointData.X} {pointData.Y} {pointData.Angle}");
                }
                //MessageBox.Show("保存成功");
            }
        }

        /// <summary>
        /// 文件传入（带角度）CSV
        /// </summary>
        /// <param name="pointsByAngle"></param>
        /// <param name="filePath"></param>
        public void SavePointsToCsvFileByAngle(List<PointDataMapLine> pointsByAngle, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var pointData in pointsByAngle)
                {

                    writer.WriteLine($"{pointData.X},{pointData.Y},{pointData.Angle}");
                }
                //MessageBox.Show("保存成功");
            }
        }


        //点位转换（附带角度旋转）
        public void TransformPointsByAngle(string filePath, double newX, double newY, double rotationAngle)
        {
            // 读取文件内容
            List<Point> points = new List<Point>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(' '); // 假设数据是以空格分隔的
                    if (parts.Length == 2)
                    {
                        double x = double.Parse(parts[0]);
                        double y = double.Parse(parts[1]);
                        points.Add(new Point(x, y));
                    }
                }
            }

            // 转换坐标系和旋转点
            RotateTransform rotationTransform = new RotateTransform(rotationAngle);

            // 更新点位并写回到文件   
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (Point point in points)
                {
                    Point transformedPoint = rotationTransform.Transform(point);
                    transformedPoint.Offset(newX, newY);

                    writer.WriteLine($"{transformedPoint.X} {transformedPoint.Y}");
                }
            }
        }



        /// <summary>
        /// 点位文件翻转函数
        /// </summary>
        /// <param name="filePath"></param>
        public void ReverseLinesInFile(string filePath)
        {
            List<string> lines = new List<string>();
            

            // 读取文件内容
    
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            // 将行逆序
            lines.Reverse();

            // 将逆序后的内容写入文件
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (string reversedLine in lines)
                {
                    writer.WriteLine(reversedLine);
                }
            }
        }
            
        public List<Point> ReverseList(List<Point> inputList)
        {
            List<Point> reversedList = new List<Point>(inputList.Count);

            for (int i = inputList.Count - 1; i >= 0; i--)
            {
                reversedList.Add(inputList[i]);
            }

            return reversedList;
        }

        /// <summary>
        /// 折线路线点位输出
        /// </summary>
        /// <param name="pathList"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        public List<Point> GetAllPoints(List<System.Windows.Shapes.Path> paths, double stepSize)
        {
            List<Point> allPoints = new List<Point>();

            foreach (var path in paths)
            {
                var pathGeometry = path.Data as PathGeometry;
                if (pathGeometry != null)
                {
                    foreach (var figure in pathGeometry.Figures)
                    {
                        var startPoint = figure.StartPoint;
                        allPoints.Add(startPoint);

                        foreach (var segment in figure.Segments)
                        {
                            if (segment is PolyLineSegment polyLineSegment)
                            {
                                foreach (var point in polyLineSegment.Points)
                                {
                                    allPoints.Add(point);
                                }
                            }
                            else if (segment is LineSegment lineSegment)
                            {
                                var endPoint = lineSegment.Point;
                                allPoints.Add(endPoint);
                            }
                            else if (segment is ArcSegment arcSegment)
                            {
                                double arcLength = arcSegment.Point.X - arcSegment.Point.Y;
                                int numSteps = (int)Math.Ceiling(arcLength / stepSize);

                                for (int i = 0; i <= numSteps; i++)
                                {
                                    double t = (double)i / numSteps;
                                    Point point = GetPointOnArc(arcSegment, t);
                                    allPoints.Add(point);
                                }
                            }
                        }
                    }
                }
            }

            return allPoints;
        }


        private Point GetPointOnArc(ArcSegment arcSegment, double t)
        {
            double angle = arcSegment.RotationAngle * t;
            double radians = angle * (Math.PI / 180.0);

            double centerX = arcSegment.Point.X - arcSegment.Size.Width / 2;
            double centerY = arcSegment.Point.Y - arcSegment.Size.Height / 2;

            double x = centerX + arcSegment.Size.Width / 2 * Math.Cos(radians);
            double y = centerY + arcSegment.Size.Height / 2 * Math.Sin(radians);

            return new Point(x, y);
        }

        /// <summary>
        /// path转point（待测试）
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        public List<Point> GetPointsOnPath(List<Path> paths, double stepSize)
        {
            List<Point> points = new List<Point>(); // 创建一个空的 Point 列表，用于存储路线上的点
            foreach (Path path in paths)
            {
                if (path.Data is LineGeometry)
                {
                    LineGeometry lineGeometry = path.Data as LineGeometry;
                    Point startPoint = lineGeometry.StartPoint;
                    Point endPoint = lineGeometry.EndPoint;

                    double distance = DistanceBetweenPoints(startPoint, endPoint); // 计算起始点和终点之间的距离
                    int numOfSteps = (int)(distance / stepSize); // 计算需要迭代的步数

                    for (int i = 0; i <= numOfSteps; i++)
                    {
                        double ratio = i / (double)numOfSteps; // 计算当前步数对应的比例
                        double x = startPoint.X + (endPoint.X - startPoint.X) * ratio; // 根据比例计算 X 坐标
                        double y = startPoint.Y + (endPoint.Y - startPoint.Y) * ratio; // 根据比例计算 Y 坐标
                        points.Add(new Point(x, y)); // 将计算得到的点添加到列表中
                    }
                }
                else if (path.Data is PathGeometry)
                {
                    PathGeometry pathGeometry = path.Data as PathGeometry;
                    PathFigure figure = pathGeometry.Figures[0];
                    ArcSegment arc = figure.Segments[0] as ArcSegment;
                    Point startPoint = figure.StartPoint;
                    Point endPoint = arc.Point;

                    double radiusX = arc.Size.Width / 2; // 计算椭圆的 X 半径
                    double radiusY = arc.Size.Height / 2; // 计算椭圆的 Y 半径
                    double centerX = startPoint.X + radiusX; // 计算椭圆的中心点 X 坐标
                    double centerY = startPoint.Y + radiusY; // 计算椭圆的中心点 Y 坐标
                    double startAngle = GetAngle(startPoint, new Point(centerX, centerY)); // 计算起始角度
                    double endAngle = GetAngle(endPoint, new Point(centerX, centerY)); // 计算终止角度

                    if (arc.SweepDirection == SweepDirection.Counterclockwise)
                    {
                        while (endAngle > startAngle)
                        {
                            endAngle -= 360; // 处理终止角度超过起始角度的情况
                        }
                    }
                    else
                    {
                        while (endAngle < startAngle)
                        {
                            endAngle += 360; // 处理起始角度超过终止角度的情况
                        }
                    }

                    double angleRange = Math.Abs(endAngle - startAngle); // 计算角度范围
                    int numOfSteps = (int)(angleRange / stepSize); // 计算需要迭代的步数

                    for (int i = 0; i <= numOfSteps; i++)
                    {
                        double ratio = i / (double)numOfSteps; // 计算当前步数对应的比例
                        double angle = startAngle + angleRange * ratio; // 根据比例计算角度
                        double x = centerX + radiusX * Math.Cos(angle * Math.PI / 180); // 根据角度计算 X 坐标
                        double y = centerY + radiusY * Math.Sin(angle * Math.PI / 180); // 根据角度计算 Y 坐标
                        points.Add(new Point(x, y)); // 将计算得到的点添加到列表中
                    }
                }
            }
        

            return points; // 返回整个路线上的点列表
        }

        private double DistanceBetweenPoints(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private double GetAngle(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Atan2(dy, dx) * (180 / Math.PI);
        }

        /// <summary>
        /// 绘制地图坐标系
        /// </summary>
        /// <param name="mainPanel"></param>
        public void Coordinate(Canvas mainPanel)
        {
            mainPan = mainPanel;
            for (int i = 0; i <= (mainPanel.Height / x); i++)
            {
                DrawingLine(new Point(0, x * i), new Point(mainPanel.Width, y * i), mainPanel);
            }
            for (int i = 0; i <= (mainPanel.Width / x); i++)
            {
                DrawingLine(new Point(x * i, 0), new Point(x * i, mainPanel.Height), mainPanel);
            }
        }

        /// <summary>
        /// 绘制X轴Y轴刻度
        /// </summary>
        /// <param name="mainPanel"></param>
        /// <param name="mainPane2"></param>
        public void CoordinateX(Canvas mainPanel, Canvas mainPane2)
        {

            for (int i = 0; i <= mainPanel.Width / x; i++)
            {
                //绘制X轴刻度
                TextBlock textX = new TextBlock();
                textX.Text = (i * 10).ToString();
                textX.Foreground = new SolidColorBrush(Colors.Black);
                if (i != (mainPanel.Width / x))
                {
                    Canvas.SetLeft(textX, (i * x) - 8);//10(X轴偏移量)
                }
                else
                {
                    Canvas.SetLeft(textX, (i * x) - 35);//10(X轴偏移量)
                }
                Canvas.SetTop(textX, 0);
                mainPanel.Children.Add(textX);


            }

            for (int i = 0; i <= (mainPane2.Height / x); i++)
            {
                //绘制Y轴刻度
                TextBlock textY = new TextBlock();
                if (!i.Equals(0))
                    textY.Text = (i * 10).ToString();
                textY.Foreground = new SolidColorBrush(Colors.Black);
                Canvas.SetLeft(textY, 0);

                if (i != (mainPane2.Height / x))
                {
                    Canvas.SetTop(textY, (i * x) - 7.5);//(-7.5Y轴偏移量)
                }
                else
                {
                    Canvas.SetTop(textY, (i * x) - 10);//(-7.5Y轴偏移量)
                }
                
                mainPane2.Children.Add(textY);
            }
        }

        public static int siseWin = 1;//上一次缩放大小
        /// <summary>
        /// 地图比例尺缩放
        /// </summary>
        /// <param name="Size"></param>
        /// <param name="mainPanel"></param>
        /// <param name="mainPane2"></param>
        public void Mapmagnify(int Size, Canvas mainPanel, Canvas mainPane2, Canvas mainPane3,double ms,double mp)
        {
            mainPane2.Children.Clear();
            mainPane3.Children.Clear();
            mainPanel.Children.Clear();

            double scaleFactor = 1; // 默认缩放因子为1，不发生缩放
            if (Size != 0)
            {
                scaleFactor = Size; // 根据缩放级别设置缩放因子
            }
            List<Point> coordinates = GetCoordinatesFromFile();
            DrawDots(mainPane3, coordinates, scaleFactor);
            if (Size.Equals(0))
            {
                x = 100;
                y = 100;
                mainPane3.Width = ms;
                mainPanel.Width = ms;
                mainPane2.Width = mp;
                Coordinate(mainPane3);
                CoordinateX(mainPanel, mainPane2);
                Zoom(1);
                siseWin = 1;
            }
            else
            {
                mainPane3.Width = Size * ms;
                mainPane3.Height = Size * mp;
                mainPanel.Width = Size * ms;
                mainPane2.Height = Size * mp;
                x = 100 * Size;
                y = 100 * Size;
                Coordinate(mainPane3);
                CoordinateX(mainPanel, mainPane2);
                Zoom(Size);
                siseWin = Size;
            }
        }

        /// <summary>
        /// 比例尺控件缩放
        /// </summary>
        /// <param name="Sise"></param>
        public void Zoom(int Sise)
        {
            foreach (WirePointArray item in MapInstrument.wirePointArrays)//线路缩放
            {
                Path path = null;
                List<Path> Kaths = null;
                Point Henpoint = item.GetPoint.SetPoint;
                Henpoint.X =( (Henpoint.X / siseWin) * Sise);
                Henpoint.Y = (((Henpoint.Y) / siseWin) * Sise);
                item.GetPoint.SetPoint = Henpoint;
                Point point = item.GetWirePoint.SetPoint;
                point.X = (((point.X) / siseWin)) * Sise ;
                point.Y = (((point.Y ) / siseWin) * Sise);
                item.GetWirePoint.SetPoint = point;
                if (item.circuitType.Equals(CircuitType.Line))//直线
                {
                    path = DrawingLine(item.GetPoint.SetPoint, item.GetWirePoint.SetPoint, mainPan);
                    path.StrokeThickness = item.GetPath.StrokeThickness;
                    path.Stroke = item.GetPath.Stroke;
                }
                else if (item.circuitType.Equals(CircuitType.Semicircle))//半圆
                {
                    path = DrawingSemicircle(item.GetPoint.SetPoint, item.GetWirePoint.SetPoint, mainPan);
                    path.StrokeThickness = item.GetPath.StrokeThickness;
                    path.Stroke = item.GetPath.Stroke;
                }
                else if (item.circuitType.Equals(CircuitType.QuadraticBezierCurve))//曲线
                {
                    //Point controlPt = new Point();//to do
                    path = DrawingQuadraticBezierCurve(item.GetPoint.SetPoint, item.GetWirePoint.SetPoint, mainPan);
                    path.StrokeThickness = item.GetPath.StrokeThickness;
                    path.Stroke = item.GetPath.Stroke;
                }
                else if (item.circuitType.Equals(CircuitType.Broken))//折线
                {
                    Kaths = DrawingBroken(item.GetPoint.SetPoint, item.GetWirePoint.SetPoint, mainPan);
                    foreach (var ite in Kaths)
                    {
                        foreach (var it in item.Paths)
                        {
                            ite.StrokeThickness = it.StrokeThickness;
                            ite.Stroke = it.Stroke;
                        }
                    }
                    
                }
                item.GetPath = path;
                item.Paths = Kaths;
            }
            foreach (int item in MapInstrument.valuePairs.Keys)//信标缩放
            {
                MapInstrument.valuePairs[item].Margin = new Thickness(((MapInstrument.valuePairs[item].Margin.Left+19) / siseWin) * Sise-19, ((MapInstrument.valuePairs[item].Margin.Top+11.5) / siseWin) * Sise-11.5, 0, 0);
                mainPan.Children.Add(MapInstrument.valuePairs[item]);
            }
            foreach (int item in MapInstrument.keyValuePairs.Keys)//区域缩放
            {
                MapInstrument.keyValuePairs[item].Margin = new Thickness((MapInstrument.keyValuePairs[item].Margin.Left / siseWin) * Sise, (MapInstrument.keyValuePairs[item].Margin.Top / siseWin) * Sise, 0, 0);
                //长宽计算
                MapInstrument.keyValuePairs[item].Width = MapInstrument.keyValuePairs[item].Width /siseWin* Sise ;
                MapInstrument.keyValuePairs[item].Height = MapInstrument.keyValuePairs[item].Height / siseWin * Sise;
                //字体计算
                MapInstrument.keyValuePairs[item].FontSize = MapInstrument.keyValuePairs[item].FontSize / siseWin * Sise;
                mainPan.Children.Add(MapInstrument.keyValuePairs[item]);    
            }

            foreach (int item in MapInstrument.GetKeyValues.Keys)//文字缩放
            {
                MapInstrument.GetKeyValues[item].Margin = new Thickness((MapInstrument.GetKeyValues[item].Margin.Left / siseWin) * Sise, (MapInstrument.GetKeyValues[item].Margin.Top / siseWin) * Sise, 0, 0);
                MapInstrument.GetKeyValues[item].FontSize = MapInstrument.GetKeyValues[item].FontSize / siseWin * Sise;
                mainPan.Children.Add(MapInstrument.GetKeyValues[item]);
            }
        }


        //private List<Point> GetPointsOnLine(Line line, double pointDistance = 1.0)
        //{
        //    List<Point> points = new List<Point>();

        //    // 获取直线的路径
        //    PathGeometry pathGeometry = line.GetFlattenedPathGeometry();

        //    // 迭代遍历所有路径段
        //    foreach (PathFigure pathFigure in pathGeometry.Figures)
        //    {
        //        Point startPoint = pathFigure.StartPoint;
        //        Point endPoint;

        //        foreach (PathSegment pathSegment in pathFigure.Segments)
        //        {
        //            if (pathSegment is LineSegment lineSegment)
        //            {
        //                endPoint = lineSegment.Point;

        //                double distance = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));
        //                int segmentsCount = (int)Math.Round(distance / pointDistance);

        //                for (int i = 0; i < segmentsCount; i++)
        //                {
        //                    double ratio = (double)i / (double)segmentsCount;
        //                    double x = startPoint.X + ratio * (endPoint.X - startPoint.X);
        //                    double y = startPoint.Y + ratio * (endPoint.Y - startPoint.Y);

        //                    points.Add(new Point(x, y));
        //                }

        //                startPoint = endPoint;
        //            }
        //        }
        //    }

        //    return points;
        //}

        //private string GeneratePointDataJson(List<Point> points)
        //{
        //    double stepSize = 0.1;
        //    List<PointDataMapLine> pointDataList = GeneratePointData(points, stepSize);

        //    string json = JsonConvert.SerializeObject(pointDataList, Formatting.Indented);
        //    return json;
        //}


        /// <summary>
        /// 手动点位发送
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static string GeneratePointDataJson(List<Point> points)
        {
            // 计算步长
            double stepSize = 0.1;

            // 生成点位数据
            List<PointDataMapLine> pointDataList = GeneratePointData(points, stepSize);

            // 将点位数据转换为JSON字符串
            string json = JsonSerializer.Serialize(pointDataList);
            

            return json;    
        }

        /// <summary>
        /// 曲线角度计算
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public  List<PointDataMapLine> GeneratePointDataByCurve(List<Point> points)
        {
            List<PointDataMapLine> pointDataList = new List<PointDataMapLine>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point startPoint = points[i];
                Point endPoint = points[i + 1];

                double dx = endPoint.X - startPoint.X;
                double dy = endPoint.Y - startPoint.Y;
                double angle = Math.Atan2(dy, dx) /** 180 / Math.PI*/;

                PointDataMapLine pointData = new PointDataMapLine(startPoint.X, startPoint.Y, angle);
                pointDataList.Add(pointData);
            }

            // 处理最后一个点角度
            if (pointDataList.Count > 1)
            {
                pointDataList.Add(new PointDataMapLine(points[points.Count - 1].X, points[points.Count - 1].Y, pointDataList[pointDataList.Count - 1].Angle));
            }
            else
            {
                pointDataList.Add(new PointDataMapLine(points[points.Count - 1].X, points[points.Count - 1].Y, 0));
            }
            // You may need to calculate the angle based on the previous point

            return pointDataList;
        }

        /// <summary>
        /// 手动点位发送的步长处理逻辑
        /// </summary>
        /// <param name="points"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        static List<PointDataMapLine> GeneratePointData(List<Point> points, double stepSize)
        {
            List<PointDataMapLine> pointDataList = new List<PointDataMapLine>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point startPoint = points[i];
                Point endPoint = points[i + 1];

                double dx = endPoint.X - startPoint.X;
                double dy = endPoint.Y - startPoint.Y;  
                double totalDistance = Math.Sqrt(dx * dx + dy * dy);
                int numSteps = (int)Math.Ceiling(totalDistance / stepSize);

                double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

                for (int j = 0; j <= numSteps; j++)
                {
                    double ratio = (double)j / numSteps;
                    double x = startPoint.X + ratio * dx;
                    double y = startPoint.Y + ratio * dy;

                    PointDataMapLine pointData = new PointDataMapLine(x, y, angle);
                    pointDataList.Add(pointData);
                }
            }

            return pointDataList;
        }

        /// <summary>
        /// 从自动规划路线生成的文件中读取点位
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<Point> ReadPointsFromFile(string filePath)
        {
            List<Point> points = new List<Point>();

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    string[] parts = line.Split(' ');

                    if (parts.Length >= 2 && double.TryParse(parts[0], out double x) && double.TryParse(parts[1], out double y))
                    {
                        points.Add(new Point(x, y));
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"An error occurred while reading the file: {ex.Message}");
            }

            return points;
        }

        // <summary>
        // 自动规划路线的路线绘制
        // </summary>
        // <param name = "canvas" ></ param >
        // < param name="points"></param>
        
        public static void DrawPointsOnCanvas(Canvas canvas, List<Point> points)
        {
            if (points.Count < 2)
            {
                return;
            }

            for (int i = 1; i < points.Count; i++)
            {
                Line line = new Line()
                {
                    X1 = points[i - 1].X,
                           Y1 = points[i - 1].Y,
                    X2 = points[i].X,
                    Y2 = points[i].Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };


                canvas.Children.Add(line);
            }
        }
        
        //DataTable双向扩充
        public static DataTable ExpandBidirectionalRoutes(DataTable lineStation)
        {
            // 创建一个新的 DataTable，用于存储双向扩展后的路线
            DataTable expandedTable = lineStation.Clone();

            // 遍历原始表，首先添加正向的线路
            foreach (DataRow row in lineStation.Rows)
            {
                // 原始方向的行
                expandedTable.ImportRow(row);

                // 创建反向行
                DataRow reverseRow = expandedTable.NewRow();
                reverseRow["StartX"] = row["EndX"];
                reverseRow["StartY"] = row["EndY"];
                reverseRow["EndX"] = row["StartX"];
                reverseRow["EndY"] = row["StartY"];
                reverseRow["LineStyel"] = row["LineStyel"]; // 保留原始 LineStyle
                reverseRow["Tag1"] = row["Tag2"]; // 反转 Tag1 和 Tag2
                reverseRow["Tag2"] = row["Tag1"];

                // 将反向行添加到表中
                expandedTable.Rows.Add(reverseRow);
            }

            // 重新排列所有行的 ID，从1开始顺序赋值
            int newId = 1;
            foreach (DataRow row in expandedTable.Rows)
            {
                row["ID"] = newId;
                newId++;
            }

            return expandedTable;
        }

        //Mes路径搜索
        public static DataTable FindRouteByErgodic(DataTable routes, Point inputPoint, string targetTag)
        {
            DataTable resultTable = new DataTable();
            resultTable.Columns.Add("StartX", typeof(double));
            resultTable.Columns.Add("StartY", typeof(double));
            resultTable.Columns.Add("EndX", typeof(double));
            resultTable.Columns.Add("EndY", typeof(double));

            // 创建一个包含路线和其距离的列表
            List<Tuple<DataRow, double>> routeDistances = new List<Tuple<DataRow, double>>();

            DataRow initialRow = null;
            Point initialPoint = inputPoint;

            // 计算每条路线的起点与输入点的距离
            foreach (DataRow row in routes.Rows)
            {
                double startX = Convert.ToDouble(row["StartX"]);
                double startY = Convert.ToDouble(row["StartY"]);
                double endX = Convert.ToDouble(row["EndX"]);
                double endY = Convert.ToDouble(row["EndY"]);

                // 先检查输入点是否在起点或终点的0.1米范围内
                if (DistanceToPointByNew(inputPoint, startX, startY) <= 0.1)
                {
                    // 如果输入点在起点附近，则直接选择起点作为初始点
                    initialRow = row;
                    initialPoint = new Point(startX, startY);
                    break; // 找到匹配的点后可以直接跳出循环
                }
                else if (DistanceToPointByNew(inputPoint, endX, endY) <= 0.1)
                {
                    // 如果输入点在终点附近，则选择终点作为初始点
                    initialRow = row;
                    initialPoint = new Point(endX, endY);
                    break;
                }

                // 如果没有找到匹配的点，则计算到起点或终点的最小距离
                double distanceToStart = DistanceToPointByNew(inputPoint, startX, startY);
                double distanceToEnd = DistanceToPointByNew(inputPoint, endX, endY);
                routeDistances.Add(new Tuple<DataRow, double>(row, Math.Min(distanceToStart, distanceToEnd)));
            }

            // 如果找到符合条件的初始点，直接从此点开始寻找路径
            if (initialRow != null)
            {
                List<DataRow> pathRows = new List<DataRow>();
                HashSet<string> visitedTags = new HashSet<string>();

                // 从已找到的起始路线开始寻找路径
                if (FindPathByErgodic(routes, initialRow, targetTag, pathRows, visitedTags))
                {
                    // 构建结果表格，并确保方向性连贯
                    DataRow firstRouteRow = resultTable.NewRow();
                    firstRouteRow["StartX"] = initialPoint.X;

                    firstRouteRow["StartY"] = initialPoint.Y;
                    firstRouteRow["EndX"] = Convert.ToDouble(initialRow["EndX"]);
                    firstRouteRow["EndY"] = Convert.ToDouble(initialRow["EndY"]);
                    resultTable.Rows.Add(firstRouteRow);

                    // 添加后续路径
                    DataRow previousRow = firstRouteRow;
                    foreach (var row in pathRows)
                    {
                        DataRow nextRouteRow = resultTable.NewRow();
                        double prevEndX = Convert.ToDouble(previousRow["EndX"]);
                        double prevEndY = Convert.ToDouble(previousRow["EndY"]);

                        // 判断路径的连贯性
                        if (DistanceToPointByNew(new Point(prevEndX, prevEndY), Convert.ToDouble(row["StartX"]), Convert.ToDouble(row["StartY"])) <
                            DistanceToPointByNew(new Point(prevEndX, prevEndY), Convert.ToDouble(row["EndX"]), Convert.ToDouble(row["EndY"])))
                        {
                            nextRouteRow["StartX"] = row["StartX"]; 
                            nextRouteRow["StartY"] = row["StartY"];
                            nextRouteRow["EndX"] = row["EndX"]; 
                            nextRouteRow["EndY"] = row["EndY"];
                        }   
                        else
                        {   
                            // 反向插入
                            nextRouteRow["StartX"] = row["EndX"];
                            nextRouteRow["StartY"] = row["EndY"];
                            nextRouteRow["EndX"] = row["StartX"];
                            nextRouteRow["EndY"] = row["StartY"];
                        }

                        resultTable.Rows.Add(nextRouteRow);
                        previousRow = nextRouteRow;
                    }

                    return resultTable; // 找到路径后立即返回结果
                }
            }

            // 如果没有找到符合条件的初始点，则继续按原逻辑查找
            routeDistances = routeDistances.OrderBy(t => t.Item2).ToList();

            // 从最近的路线开始逐个检查是否存在通向目标标签的路径
            foreach (var routeTuple in routeDistances)
            {
                DataRow startRow = routeTuple.Item1;
                List<DataRow> pathRows = new List<DataRow>();
                HashSet<string> visitedTags = new HashSet<string>();

                // 查找从该起始路线到目标标签的路径
                bool pathFound = FindPathByErgodic(routes, startRow, targetTag, pathRows, visitedTags);

                if (pathFound)
                {
                    // 确定输入点到起始路线的连接方式
                    DataRow firstRouteRow = resultTable.NewRow();
                    firstRouteRow["StartX"] = inputPoint.X;
                    firstRouteRow["StartY"] = inputPoint.Y;

                    // 允许双向连接，找最近的点作为起点
                    if (DistanceToPointByNew(inputPoint, Convert.ToDouble(startRow["StartX"]), Convert.ToDouble(startRow["StartX"])) <=
                        DistanceToPointByNew(inputPoint, Convert.ToDouble(startRow["EndX"]), Convert.ToDouble(startRow["EndY"])))
                    {
                        firstRouteRow["EndX"] = Convert.ToDouble(startRow["StartX"]);
                        firstRouteRow["EndY"] = Convert.ToDouble(startRow["StartY"]);
                    }
                    else
                    {
                        firstRouteRow["EndX"] = Convert.ToDouble(startRow["EndX"]);
                        firstRouteRow["EndY"] = Convert.ToDouble(startRow["EndY"]);
                    }

                    resultTable.Rows.Add(firstRouteRow);

                    // 添加后续路径
                    DataRow previousRow = firstRouteRow;
                    foreach (var row in pathRows)
                    {
                        DataRow nextRouteRow = resultTable.NewRow();
                        double prevEndX = Convert.ToDouble(previousRow["EndX"]);
                        double prevEndY = Convert.ToDouble(previousRow["EndY"]);

                        // 判断路径的连贯性
                        if (DistanceToPointByNew(new Point(prevEndX, prevEndY), Convert.ToDouble(row["StartX"]), Convert.ToDouble(row["StartY"])) <
                            DistanceToPointByNew(new Point(prevEndX, prevEndY), Convert.ToDouble(row["EndX"]), Convert.ToDouble(row["EndY"])))
                        {
                            nextRouteRow["StartX"] = row["StartX"];
                            nextRouteRow["StartY"] = row["StartY"];
                            nextRouteRow["EndX"] = row["EndX"];
                            nextRouteRow["EndY"] = row["EndY"];
                        }
                        else
                        {
                            // 反向插入
                            nextRouteRow["StartX"] = row["EndX"];
                            nextRouteRow["StartY"] = row["EndY"];
                            nextRouteRow["EndX"] = row["StartX"];
                            nextRouteRow["EndY"] = row["StartY"];
                        }

                        resultTable.Rows.Add(nextRouteRow);
                        previousRow = nextRouteRow;
                    }

                    return resultTable; // 找到一条路径后立即返回结果
                }
            }

            // 如果没有找到任何路径，抛出异常
            throw new Exception("找不到到目标标签的路线。");
        }

        
        private static bool FindPathByErgodic(DataTable routes, DataRow currentRow, string targetTag, List<DataRow> pathRows, HashSet<string> visitedTags)
        {
            string tag1 = currentRow["Tag1"].ToString();
            string tag2 = currentRow["Tag2"].ToString();

            // 如果当前行包含目标标签，直接添加到路径并返回
            if (tag1 == targetTag || tag2 == targetTag)
            {
                pathRows.Add(currentRow);
                return true;
            }

            visitedTags.Add(tag1);
            visitedTags.Add(tag2);

            // 查找可以继续前进的下一条路线
            var nextRows = routes.AsEnumerable()
                .Where(row => !visitedTags.Contains(row["Tag1"].ToString()) || !visitedTags.Contains(row["Tag2"].ToString()))
                .Where(row =>
                    (row["Tag1"].ToString() == tag1 || row["Tag2"].ToString() == tag1 ||
                     row["Tag1"].ToString() == tag2 || row["Tag2"].ToString() == tag2))
                .ToList();

            foreach (var nextRow in nextRows)
            {
                pathRows.Add(currentRow);

                // 无论起点或终点，都可以继续搜索
                if (FindPathByErgodic(routes, nextRow, targetTag, pathRows, visitedTags))
                {
                    return true;
                }

                pathRows.RemoveAt(pathRows.Count - 1);  // 回溯
            }

            return false;
        }










        //自动充电
        public static DataTable FindRoute(DataTable routes, Point inputPoint, string targetTag)
        {
            DataTable resultTable = new DataTable();
            resultTable.Columns.Add("StartX", typeof(double));
            resultTable.Columns.Add("StartY", typeof(double));
            resultTable.Columns.Add("EndX", typeof(double));
            resultTable.Columns.Add("EndY", typeof(double));

            DataRow closestStartRow = null;
            double minDistance = double.MaxValue;

            foreach (DataRow row in routes.Rows)
            {
                double startX = Convert.ToDouble(row["StartX"]);
                double startY = Convert.ToDouble(row["StartY"]);
                double endX = Convert.ToDouble(row["EndX"]);
                double endY = Convert.ToDouble(row["EndY"]);

                // 计算输入点到路线的距离
                double distanceToStart = DistanceToPoint(inputPoint, startX, startY, endX, endY);
                if (distanceToStart < minDistance)
                {
                    minDistance = distanceToStart;
                    closestStartRow = row;
                }
            }

            if (closestStartRow == null)
            {
                throw new Exception("没有找到距离输入点足够近的起始点。");
            }

            // 添加输入点到最近的路线起点的路线
            DataRow firstRouteRow = resultTable.NewRow();
            firstRouteRow["StartX"] = inputPoint.X;
            firstRouteRow["StartY"] = inputPoint.Y;
            firstRouteRow["EndX"] = Convert.ToDouble(closestStartRow["StartX"]);
            firstRouteRow["EndY"] = Convert.ToDouble(closestStartRow["StartY"]);
            resultTable.Rows.Add(firstRouteRow);

            List<DataRow> pathRows = new List<DataRow>();
            HashSet<string> visitedTags = new HashSet<string>();
            bool pathFound = FindPath(routes, closestStartRow, targetTag, pathRows, visitedTags);

            if (!pathFound)
            {
                throw new Exception("找不到到目标标签的路线。");
            }

            // 将找到的路径行添加到 resultTable 中
            foreach (var row in pathRows)
            {
                resultTable.ImportRow(row);
            }

            return resultTable;
        }

        private static bool FindPath(DataTable routes, DataRow currentRow, string targetTag, List<DataRow> pathRows, HashSet<string> visitedTags)
        {
            string tag1 = currentRow["Tag1"].ToString();
            string tag2 = currentRow["Tag2"].ToString();

            // 如果当前行包含目标标签，直接添加到路径并返回
            if (tag1 == targetTag || tag2 == targetTag)
            {
                pathRows.Add(currentRow);
                return true;
            }

            visitedTags.Add(tag1);
            visitedTags.Add(tag2);

            // 查找可以继续前进的下一条路线
            var nextRows = routes.AsEnumerable()
                .Where(row => !visitedTags.Contains(row["Tag1"].ToString()) || !visitedTags.Contains(row["Tag2"].ToString()))
                .Where(row =>
                    (row["Tag1"].ToString() == tag1 || row["Tag2"].ToString() == tag1 ||
                     row["Tag1"].ToString() == tag2 || row["Tag2"].ToString() == tag2))
                .ToList();

            foreach (var nextRow in nextRows)
            {
                pathRows.Add(currentRow);
                if (FindPath(routes, nextRow, targetTag, pathRows, visitedTags))
                {
                    return true;
                }
                pathRows.RemoveAt(pathRows.Count - 1);  // 回溯
            }

            return false;
        }

        private static double DistanceToPointByNew(Point inputPoint, double startX, double startY)
        {
            return Math.Sqrt(Math.Pow(inputPoint.X - startX, 2) + Math.Pow(inputPoint.Y - startY, 2));
        }


        private static double DistanceToPoint(Point point, double startX, double startY, double endX, double endY)
        {
            double A = point.X - startX;
            double B = point.Y - startY;
            double C = endX - startX;
            double D = endY - startY;

            double dot = A * C + B * D;
            double len_sq = C * C + D * D;
            double param = -1.0;

            if (len_sq != 0)
                param = dot / len_sq;

            double xx, yy;

            if (param < 0)
            {
                xx = startX;
                yy = startY;
            }
            else if (param > 1)
            {
                xx = endX;
                yy = endY;
            }
            else
            {
                xx = startX + param * C;
                yy = startY + param * D;
            }

            double dx = point.X - xx;
            double dy = point.Y - yy;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        // 定义欧几里得距离计算
        private static double CalculateDistance(Point p1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(p1.X - x2, 2) + Math.Pow(p1.Y - y2, 2));
        }

        //新的自动充电和Mes路径算法
        public static DataTable FindRouteNew(DataTable routes, Point inputPoint, string targetTag)
        {
            // 创建结果 DataTable
            DataTable resultTable = routes.Clone();
            resultTable.Columns.Remove("ID"); // 删除ID列
            resultTable.Columns.Remove("LineStyel"); // 删除LineStyle列

            // Step 1: 找到距离 inputPoint 最近的所有起点，按距离升序排列
            var sortedRows = routes.AsEnumerable()
                .Select(row => new
                {
                    Row = row,
                    Distance = CalculateDistance(inputPoint, Convert.ToDouble(row["StartX"]), Convert.ToDouble(row["StartY"]))
                })
                .OrderBy(x => x.Distance)
                .ToList();

            // Step 2: 遍历每个可能的起始点，直到找到到达目标站点的路径
            foreach (var entry in sortedRows)
            {
                DataRow startRow = entry.Row;
                double minDistance = entry.Distance;

                DataTable tempTable = resultTable.Clone(); // 临时存储路径
                

                // 处理最近点距离，生成第一段路径
                if (minDistance < 0.5)
                {
                    // 距离小于 0.1m 直接从最近的点开始
                    tempTable.ImportRow(startRow);

                    // 标记第一段路径为已访问
                    //visited.Add(startRow);
                }
                else
                {
                    // 生成从 inputPoint 到最近起点的第一段路径
                    DataRow firstRow = tempTable.NewRow();
                    firstRow["StartX"] = inputPoint.X;
                    firstRow["StartY"] = inputPoint.Y;
                    firstRow["EndX"] = startRow["StartX"];
                    firstRow["EndY"] = startRow["StartY"];
                    firstRow["Tag1"] = "InputPoint";
                    firstRow["Tag2"] = startRow["Tag1"];
                    tempTable.Rows.Add(firstRow);
                }

                // Step 3: 从起始点开始查找路径
                List<DataRow> visited = new List<DataRow>();
                List<DataRow> path = FindShortestPath(routes, startRow["Tag1"].ToString(), targetTag, visited);

                // 如果找到路径，返回完整的结果
                if (path != null && path.Count > 0)
                {
                    foreach (DataRow row in path)
                    {
                        tempTable.ImportRow(row);
                    }
                    return tempTable; // 返回找到的路径
                }
            }

            // 如果遍历所有可能的起点后仍然找不到路径
            throw new Exception("No route found to reach the target.");
        }

        // 广度优先遍历（BFS）寻找从 startTag 到 targetTag 的最短路径
        private static List<DataRow> FindShortestPath(DataTable routes, string startTag, string targetTag, List<DataRow> visited)
        {
            Queue<List<DataRow>> queue = new Queue<List<DataRow>>();

            // 找到所有从起始点 startTag 出发的线路
            foreach (DataRow row in routes.Rows)
            {
                if (row["Tag1"].ToString() == startTag && !visited.Contains(row))
                {
                    List<DataRow> initialPath = new List<DataRow> { row };
                    queue.Enqueue(initialPath);
                    visited.Add(row);
                }
            }

            // BFS 寻找最短路径
            while (queue.Count > 0)
            {
                List<DataRow> currentPath = queue.Dequeue();
                DataRow lastRow = currentPath.Last();

                if (lastRow["Tag2"].ToString() == targetTag)
                {
                    // 找到目标
                    return currentPath;
                }

                // 找到以当前路径最后一点为起点的所有线路
                string currentEndTag = lastRow["Tag2"].ToString();

                foreach (DataRow row in routes.Rows)
                {
                    if (row["Tag1"].ToString() == currentEndTag && !visited.Contains(row))
                    {
                        List<DataRow> newPath = new List<DataRow>(currentPath) { row };
                        queue.Enqueue(newPath);
                        visited.Add(row);
                    }
                }
            }

            return null; // 没有找到路径
        }

        public static DataTable ProcessDataTable(DataTable inputTable)
        {
            // 如果表格没有两行或者没有指定列，直接返回原始表格
            if (inputTable.Rows.Count < 2 ||
                !inputTable.Columns.Contains("StartX") ||
                !inputTable.Columns.Contains("StartY"))
            {
                return inputTable;
            }

            // 获取第一行和第二行的 StartX 和 StartY 值
            var startX1 = inputTable.Rows[0]["StartX"];
            var startY1 = inputTable.Rows[0]["StartY"];
            var startX2 = inputTable.Rows[1]["StartX"];
            var startY2 = inputTable.Rows[1]["StartY"];

            // 判断第一行和第二行的 StartX 和 StartY 是否相同
            if (startX1.Equals(startX2) && startY1.Equals(startY2))
            {
                inputTable.Rows.RemoveAt(0); // 删除第一行
            }

            //// 如果表格有更多行，检查第一行与第三行是否有往复点位
            //if (inputTable.Rows.Count > 2)
            //{
            //    var startX3 = inputTable.Rows[2]["StartX"];
            //    var startY3 = inputTable.Rows[2]["StartY"];

            //    if (startX1.Equals(startX3) && startY1.Equals(startY3))
            //    {
            //        inputTable.Rows.RemoveAt(1); // 删除第二行
            //        inputTable.Rows.RemoveAt(1); // 删除第三行 (注意，删除第二行后，第三行变为第二行)
            //    }
            //}

            return inputTable;
        }

        /// <summary>
        /// 根据连线 DataTable 生成站点点表（包含邻接点）
        /// </summary>
        public static DataTable GenerateTagTableWithNeighbors(DataTable sourceTable,
                                              double actualWidthNow,
                                              double actualHeightNow,
                                              double proportionNow)
        {
            DataTable resultTable = new DataTable();
            resultTable.Columns.Add("TagName", typeof(string));
            resultTable.Columns.Add("X", typeof(double));
            resultTable.Columns.Add("Y", typeof(double));
            resultTable.Columns.Add("NeighborIds", typeof(string));

            // 用 HashSet 防止重复添加
            HashSet<string> seenTags = new HashSet<string>();
            List<(string Tag, double X, double Y)> allPoints = new List<(string, double, double)>();

            foreach (DataRow row in sourceTable.Rows)
            {
                string tag1 = row["Tag1"].ToString();
                string tag2 = row["Tag2"].ToString();

                double startX = Convert.ToDouble(row["StartX"]);
                double startY = Convert.ToDouble(row["StartY"]);
                double endX = Convert.ToDouble(row["EndX"]);
                double endY = Convert.ToDouble(row["EndY"]);

                Point startPoint = new Point(10 * startX, 10 * startY);
                Point endPoint = new Point(10 * endX, 10 * endY);
                Painting painting = new Painting();
                Point startTrans = painting.TransformCoordinate(startPoint, actualWidthNow, actualHeightNow, proportionNow);
                Point endTrans = painting.TransformCoordinate(endPoint, actualWidthNow, actualHeightNow, proportionNow);

                if (!seenTags.Contains(tag1))
                {
                    allPoints.Add((tag1, startTrans.X, startTrans.Y));
                    seenTags.Add(tag1);
                }

                if (!seenTags.Contains(tag2))
                {
                    allPoints.Add((tag2, endTrans.X, endTrans.Y));
                    seenTags.Add(tag2);
                }
            }

            // 改进排序：按 Tag 数字部分排序
            var sortedPoints = allPoints
                .OrderBy(p => ExtractTagNumber(p.Tag))
                .ToList();

            // 构建邻接表
            Dictionary<string, HashSet<string>> neighbors = new Dictionary<string, HashSet<string>>();

                foreach (DataRow row in sourceTable.Rows)
                {
                    string tag1 = row["Tag1"].ToString();
                    string tag2 = row["Tag2"].ToString();

                    if (!neighbors.ContainsKey(tag1))
                        neighbors[tag1] = new HashSet<string>();
                    if (!neighbors.ContainsKey(tag2))
                        neighbors[tag2] = new HashSet<string>();

                    neighbors[tag1].Add(tag2);
                    neighbors[tag2].Add(tag1);
                }

            // 输出结果
            foreach (var item in sortedPoints)
            {
                string neighborList = neighbors.ContainsKey(item.Tag)
                    ? string.Join(",", neighbors[item.Tag].OrderBy(t => ExtractTagNumber(t)))
                    : "";

                resultTable.Rows.Add(item.Tag, item.X, item.Y, neighborList);
            }

            return resultTable;
        }

        // 提取 Tag 数字部分用于排序
        private static int ExtractTagNumber(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return 0;

            string digits = new string(tag.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int number))
                return number;

            return 0;
        }
        /// <summary>
        /// 输出路径规划算法dll返回的路线数据
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public static Dictionary<string, List<Tuple<Point, int, double, double>>> ParseMultiAgentPaths(string output)
        {
            var result = new Dictionary<string, List<Tuple<Point, int, double, double>>>();

            // 解析 JSON
            var root = JObject.Parse(output);

            // 遍历每个 AGV 的 IP 键
            foreach (var kvp in root)
            {
                string ip = kvp.Key;
                var array = kvp.Value as JArray;
                if (array == null) continue;

                var pointList = new List<Tuple<Point, int, double, double>>();

                foreach (var item in array)
                {
                    double x = item["x"].Value<double>();
                    double y = item["y"].Value<double>();

                    // 创建 Tuple<Point, int, double, double>
                    pointList.Add(Tuple.Create(new Point(x, y), 0, 0.7, 0.3));
                }

                result[ip] = pointList;
            }

            return result;
        }

        /// <summary>
        /// 解析 DLL 输出并按 AGV 地址发送对应路径数据
        /// </summary>
        /// <param name="outputJson">CppCBSLib.GetMultiAgentPaths(json) 输出的 JSON</param>
        /// <param name="insertDistance">插入点距离参数</param>
        public static async Task ProcessAndPublishPerAgentAsync(string outputJson, double insertDistance = 0.001)
        {
            // 解析 DLL 输出 => Dictionary<string, List<Tuple<Point, int, double, double>>>
            var parsed = Painting.ParseMultiAgentPaths(outputJson);
            if (parsed == null || parsed.Count == 0)
                return;

            foreach (var kv in parsed)
            {
                string ip = kv.Key;  // AGV 地址（也是连接 key）
                var pts4 = kv.Value; // 路径点集合

                if (pts4 == null || pts4.Count == 0)
                    continue;       

                // 构建 6 元组输入：Point, lift, v_desired, a_desired, angleFlag, obsAvoid
                var pts6 = new List<Tuple<Point, int, double, double, double, double>>(pts4.Count);
                for (int i = 0; i < pts4.Count; i++)
                {
                    var p = pts4[i];
                    int lift = (i == 0) ? 21 : (i == pts4.Count - 1 ? 22 : p.Item2);
                    double vDesired = 0.7;  
                    double aDesired = 0.3;
                    double angleFlag = 1.0;
                    double obsAvoid = 0.0;
                    pts6.Add(Tuple.Create(p.Item1, lift, vDesired, aDesired, angleFlag, obsAvoid));
                }   

                // 调用核心路径处理逻辑（保持角度与插值计算不变）
                List<newPointStraightWithAngle> processedPoints = ProcessPointsByTurnRotate(pts6, insertDistance);

                // 封装 JSON
                newStationData stationData = new newStationData();  
                stationData.Stations.AddRange(processedPoints);

                string jsonString = JsonSerializer.Serialize(stationData, new JsonSerializerOptions { WriteIndented = true });

                // 写文件
                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string fileName = $"MesPoints_{ip.Replace(':', '_')}.json";
                File.WriteAllText(System.IO.Path.Combine(folderPath, fileName), jsonString);

                // 按 IP 找对应 MQTT 客户端
                if (MqttConnectionManager.MqttClients.TryGetValue(ip, out MqttClientWrapper wrapper) &&
                    wrapper != null && wrapper.IsConnected)
                {
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("AGV/Carrier/MapLineMes")
                        .WithPayload(jsonString)
                        .WithExactlyOnceQoS()
                        .WithRetainFlag(false)
                        .Build();

                    try
                    {   
                        await wrapper.PublishAsync(message);
                        Console.WriteLine($"✅ 已向 {ip} 发送 MapLineMes 路径数据。");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ 发送到 {ip} 失败: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ 未找到已连接的 MQTT 客户端: {ip}");
                }
            }
        }


    }


    public class PointMapLine
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointMapLine(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class PointDataMapLine
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }

        public PointDataMapLine(double x, double y, double angle)
        {
            //X = Math.Round(x, 2);
            //Y = Math.Round(y, 2);
            //Angle = Math.Round(angle, 2);
            X = x;
            Y = y;
            Angle = angle;
        }
    }

}




