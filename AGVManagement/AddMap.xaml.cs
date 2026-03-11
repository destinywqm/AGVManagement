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
            if (MapSizeN.Text.Trim() == "")
            {
                return false;
            }
            else if (MapSizeW.Text.Trim() == "")
            {
                return false;
            }
            else if (MapSizeH.Text.Trim() == "")
            {
                return false;
            }
            return true;
        }
        #endregion

        /// <summary>
        /// 表单提交
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapSum_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator())
                return;
            this.Close();
            long gs = 0;
            MainWindow main = new MainWindow(gs, null, MapSizeN.Text,Convert.ToDouble(MapSizeW.Text.Trim()), Convert.ToDouble(MapSizeH.Text.Trim()), Convert.ToDouble(ActualLength.Text.Trim()), Convert.ToDouble(ActualWidth.Text.Trim()), Convert.ToDouble(ScaleRatio.Text.Trim()));
            if (!string.IsNullOrEmpty(importedImagePath))
            {
                main.SetBackgroundImage(importedImagePath); // 传递图片路径
            }
            main.ShowDialog();
            

        }

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
