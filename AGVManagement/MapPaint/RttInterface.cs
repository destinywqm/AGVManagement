using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace AGVManagement.MapPaint
{
    public class RttInterface
    {
        public void DrawPointsAndLines(Canvas canvas, List<Point> coordinates)
        {
            int radius = 5;
            System.Windows.Media.Brush brush = System.Windows.Media.Brushes.Red;
            System.Windows.Media.Pen pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);

            // 绘制圆点
            foreach (Point coordinate in coordinates)
            {
                int x = (int)coordinate.X - radius / 2;
                int y = (int)coordinate.Y - radius / 2;
                Ellipse circle = new Ellipse
                {
                    Width = radius,
                    Height = radius,
                    Fill = brush
                };
                Canvas.SetLeft(circle, x);
                Canvas.SetTop(circle, y);
                canvas.Children.Add(circle);
            }

            // 绘制连线
            if (coordinates.Count >= 2)
            {
                for (int i = 1; i < coordinates.Count; i++)
                {
                    Point startPoint = coordinates[i - 1];
                    Point endPoint = coordinates[i];
                    Line line = new Line
                    {
                        X1 = startPoint.X,
                        Y1 = startPoint.Y,
                        X2 = endPoint.X,
                        Y2 = endPoint.Y,
                        Stroke = pen.Brush,
                        StrokeThickness = pen.Thickness
                    };
                    canvas.Children.Add(line);
                }
            }
        }

       public List<Point> ReadCoordinates(string filePath)
       {
            List<Point> coordinates = new List<Point>();

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] parts = line.Split(' ');
                        if (parts.Length >= 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                        {
                            coordinates.Add(new Point(x, y));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading file: " + e.Message);
            }

            return coordinates;
       }


        public interface IResolutionParser
        {
            (int width, int height) ParseResolution(string resolutionData);
        }

        public class ResolutionParser : IResolutionParser
        {
            public (int width, int height) ParseResolution(string resolutionData)
            {
                // 解析分辨率数据，这里假设分辨率数据格式为 "width x height"，如 "1920x1080"
                string[] parts = resolutionData.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                {
                    return (width, height);
                }
                else
                {
                    throw new ArgumentException("Invalid resolution data format.");
                }
            }
        }

        public class MainViewModel : INotifyPropertyChanged
        {
            private IResolutionParser _resolutionParser;

            public MainViewModel(IResolutionParser resolutionParser)
            {
                _resolutionParser = resolutionParser;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            //public ICommand DrawGridCommand
            //{
            //    get { return _drawGridCommand ?? (_drawGridCommand = new DrawGridCommand()); }
            //}

            private ICommand _drawGridCommand;

            public void ExecuteDrawGridCommand(string resolutionData)
            {
                // 解析传输的分辨率数据
                (int width, int height) = _resolutionParser.ParseResolution(resolutionData);

                // 计算位置和网格大小数据
                
            }

            
        }
    }
}
