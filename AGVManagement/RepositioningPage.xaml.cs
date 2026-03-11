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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AGVManagement
{
    /// <summary>
    /// RepositioningPage.xaml 的交互逻辑
    /// </summary>
    public partial class RepositioningPage : Window
    {

        public string dataResStatuX { get; private set; }
        public string dataResStatuY { get; private set; }
        public string dataResStatuZ { get; private set; }

        public RepositioningPage()
        {
            InitializeComponent();
        }

        private void OnCompleteButtonClick(object sender, RoutedEventArgs e)
        {
            // 在这里验证输入并关闭窗口
            dataResStatuX = dataMes1.Text;
            dataResStatuY = dataMes2.Text; 
            dataResStatuZ = dataMes3.Text;
            DialogResult = true;  // 设置对话框结果为 true 表示用户确认
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;  // 设置对话框结果为 false 表示用户取消
            Close();
        }

        private void dataMes_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
