using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Security.Policy;
using System.Windows.Documents;

namespace AGVManagement.MapPaint
{
    internal class AbandonedHelpMethods
    {
        private void LoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            // 打开文件对话框并选择文本文件
            //OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.Filter = "文本文件 (*.txt)|*.txt";
            //if (openFileDialog.ShowDialog() == true)
            //{
            //    // 读取文本文件的所有行
            //    string[] lines = File.ReadAllLines(openFileDialog.FileName);

            //    // 创建坐标列表
            //    List<Point> coordinates = new List<Point>();

            //    foreach (string line in lines)
            //    {
            //        // 按空格分割每行数据
            //        string[] values = line.Split(' ');

            //        // 检查数据是否有效
            //        if (values.Length >= 2 && double.TryParse(values[0], out double x) && double.TryParse(values[1], out double y))
            //        {
            //            // 将坐标乘以 10
            //            x *= 100;
            //            y *= 100;

            //            // 创建 Point 对象并添加到列表中
            //            coordinates.Add(new Point(x, y));
            //        }
            //    }
            //    // 绘制坐标点
            //    foreach (Point point in coordinates)
            //    {
            //        // 转换坐标原点为画布中心点
            //        double centerX = mainPanel.ActualWidth / 2;
            //        double centerY = mainPanel.ActualHeight / 2;
            //        double translatedX = centerX + point.X;
            //        double translatedY = centerY - point.Y;
            //        //MessageBox.Show(centerX.ToString());
            //        // 创建圆形
            //        Ellipse ellipse = new Ellipse();
            //        ellipse.Width = 2;
            //        ellipse.Height = 2;
            //        ellipse.Fill = Brushes.Red;

            //        // 设置圆形的位置
            //        Canvas.SetLeft(ellipse, translatedX - ellipse.Width / 2);
            //        Canvas.SetTop(ellipse, translatedY - ellipse.Height / 2);

            //        // 将圆形添加到画布上
            //        mainPanel.Children.Add(ellipse);
            //    }
            //Path path = new Path();
            //PathGeometry pathGeometry = new PathGeometry();
            //ArcSegment arc = new ArcSegment(startPt, new Size(Sise, Sise), 0, false, Static ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true);
            //PathFigure figure = new PathFigure();
            //figure.StartPoint = endPt;
            //figure.Segments.Add(arc);
            //pathGeometry.Figures.Add(figure);


            //path.Data = pathGeometry;
            //path.Stroke = Brushes.Black;
            //mainPanel.Children.Add(path);
            //return path;

        }
        }
    
}
