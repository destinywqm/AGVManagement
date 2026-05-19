using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace AGVManagement
{
    /// <summary>
    /// AddMap.xaml 的交互逻辑
    /// </summary>
    public partial class AddMap : Window
    {
        private string importedImagePath = string.Empty; // 存储导入的图片路径

        public AddMap()
        {
            InitializeComponent();
        }

        private void OFFMp_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #region 验证
        private bool Validator()
        {
            return !string.IsNullOrWhiteSpace(MapSizeN.Text) &&
                   !string.IsNullOrWhiteSpace(RawWidth.Text) &&
                   !string.IsNullOrWhiteSpace(RawHeight.Text) &&
                   !string.IsNullOrWhiteSpace(Resolution.Text) &&
                   !string.IsNullOrWhiteSpace(OriginX.Text) &&
                   !string.IsNullOrWhiteSpace(OriginY.Text) &&
                   !string.IsNullOrWhiteSpace(ScaleFactor.Text);
        }
        #endregion

        /// <summary>
        /// 表单提交
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        #region 提交（核心计算）
        private void MapSum_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator())
            {
                MessageBox.Show("请填写完整数据！");
                return;
            }
            this.Close();
            try
            {
                // ===== 原始输入 =====
                double width = Convert.ToDouble(RawWidth.Text.Trim());
                double height = Convert.ToDouble(RawHeight.Text.Trim());
                double resolution = Convert.ToDouble(Resolution.Text.Trim());
                double originX = Convert.ToDouble(OriginX.Text.Trim());
                double originY = Convert.ToDouble(OriginY.Text.Trim());
                double scale = Convert.ToDouble(ScaleFactor.Text.Trim());

                if (resolution == 0)
                {
                    MessageBox.Show("分辨率不能为0！");
                    return;
                }

                // 核心计算 
                double pixelPerMeter = 1.0 / resolution;  // 20

                //
                double finalW = (width / resolution) * 0.1 * scale;   // 9.35/0.05 * 0.1 * 7 = 130.9 ✅
                double finalH = (height / resolution) * 0.1 * scale;  // 12.35/0.05 * 0.1 * 7 = 172.9 ✅

                // 比例不变
                double finalScaleRatio = pixelPerMeter * scale;  

                double originX_ui = Math.Abs(originX) * pixelPerMeter * scale;
                

                double originY_ui = ((height / resolution) * scale) - Math.Abs(originY) * pixelPerMeter * scale;

                long gs = 0;

                MainWindow main = new MainWindow(
    gs,
    null,
    MapSizeN.Text,
    finalW,        // 130.9
    finalH,        // 172.9
    originX_ui,    // 434.6
    originY_ui,    // 464.07
    finalScaleRatio // 140
);

                if (!string.IsNullOrEmpty(importedImagePath))
                {
                    main.SetBackgroundImage(importedImagePath);
                }

                main.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        //图片传递
        private void ImportImageBtn_Click(object sender, RoutedEventArgs e)
        {
            // 打开文件对话框选择图片
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "选择图片",
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 获取选中文件路径
                    string sourceFilePath = openFileDialog.FileName;

                    // 确保目标文件夹存在
                    string destinationFolder = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Images");
                    if (!Directory.Exists(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }

                    // 构建目标文件路径，复制图片到 Images 文件夹
                    string destinationFileName = System.IO.Path.GetFileName(sourceFilePath);
                    string destinationFilePath = System.IO.Path.Combine(destinationFolder, destinationFileName);

                    File.Copy(sourceFilePath, destinationFilePath, true); // 覆盖已有文件
                    importedImagePath = $"Images/{destinationFileName}"; // 相对路径保存

                    MessageBox.Show("图片导入成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"图片导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
