using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AGV.DAL;

namespace AGVManagement
{
    public partial class CarEditorWindow : Window
    {
        private int? carId = null; // 编辑时使用

        public CarEditorWindow()
        {
            InitializeComponent();
        }

        public CarEditorWindow(int id) : this()
        {
            carId = id;
            LoadCar(id);
        }

        // 确保数据库表存在
        private void EnsureCarTableExists()
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS CarModel (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(100),
                Type VARCHAR(50),
                Length DOUBLE,
                Width DOUBLE,
                `Load` DOUBLE,
                CommMode VARCHAR(50)
            );";
            MySqlHelper.ExecuteNonQuery(sql);
        }

        // 加载车辆数据
        private void LoadCar(int id)
        {
            EnsureCarTableExists();
            var dt = MySqlHelper.ExecuteDataTable($"SELECT * FROM CarModel WHERE Id={id}");
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                NameTextBox.Text = row["Name"].ToString();
                TypeComboBox.Text = row["Type"].ToString();
                LengthTextBox.Text = row["Length"].ToString();
                WidthTextBox.Text = row["Width"].ToString();
                LoadTextBox.Text = row["Load"].ToString();
                CommComboBox.Text = row["CommMode"].ToString();
                UpdateCarPreview();
            }
        }

        // 输入框变化更新预览
        private void DimensionChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCarPreview();
        }

        // 窗口大小变化也更新
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCarPreview();
        }

        // 更新车辆模型矩形
        private void UpdateCarPreview()
        {
            if (!double.TryParse(LengthTextBox.Text, out double length)) length = 1;
            if (!double.TryParse(WidthTextBox.Text, out double width)) width = 1;

            double canvasW = CarCanvas.ActualWidth;
            double canvasH = CarCanvas.ActualHeight;
            if (canvasW <= 0 || canvasH <= 0) return; // 避免初始化报错

            double maxW = canvasW - 40;
            double maxH = canvasH - 40;

            double scale = Math.Min(maxW / 10.0, maxH / 10.0);
            double rectW = Math.Max(length * scale, 1);
            double rectH = Math.Max(width * scale, 1);

            Canvas.SetLeft(CarRectangle, Math.Max((canvasW - rectW) / 2, 0));
            Canvas.SetTop(CarRectangle, Math.Max((canvasH - rectH) / 2, 0));

            CarRectangle.Width = rectW;
            CarRectangle.Height = rectH;

            DrawScale();
        }

        // 绘制固定标尺
        private void DrawScale()
        {
            CarCanvas.Children.Clear();
            CarCanvas.Children.Add(CarRectangle);

            double margin = 20;

            // 横向标尺
            Line hLine = new Line
            {
                X1 = margin,
                Y1 = CarCanvas.ActualHeight - margin,
                X2 = CarCanvas.ActualWidth - margin,
                Y2 = CarCanvas.ActualHeight - margin,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            CarCanvas.Children.Add(hLine);

            // 纵向标尺
            Line vLine = new Line
            {
                X1 = margin,
                Y1 = margin,
                X2 = margin,
                Y2 = CarCanvas.ActualHeight - margin,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            CarCanvas.Children.Add(vLine);
        }

        // 保存按钮
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureCarTableExists();

            string name = NameTextBox.Text;
            string type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            double length = double.TryParse(LengthTextBox.Text, out var l) ? l : 0;
            double width = double.TryParse(WidthTextBox.Text, out var w) ? w : 0;
            double load = double.TryParse(LoadTextBox.Text, out var ld) ? ld : 0;
            string commMode = (CommComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";

            string sql;
            if (carId == null) // 新增
            {
                sql = $@"INSERT INTO CarModel (Name, Type, Length, Width, `Load`, CommMode)
                         VALUES ('{name}', '{type}', {length}, {width}, {load}, '{commMode}')";
            }
            else // 编辑
            {
                sql = $@"UPDATE CarModel SET 
                            Name='{name}',
                            Type='{type}',
                            Length={length},
                            Width={width},
                            `Load`={load},
                            CommMode='{commMode}'
                         WHERE Id={carId}";
            }

            MySqlHelper.ExecuteNonQuery(sql);

            MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }
    }
}
