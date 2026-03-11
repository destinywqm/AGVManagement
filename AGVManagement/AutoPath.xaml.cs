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

namespace AGVManagement
{
    /// <summary>
    /// AutoPath.xaml 的交互逻辑
    /// </summary>
    public partial class AutoPath : Window
    {
        public string dataAutoStatu { get; private set; }
        public AutoPath()
        {
            InitializeComponent();
        }

        private void OnCompleteButtonClick(object sender, RoutedEventArgs e)
        {
            // 在这里验证输入并关闭窗口
            dataAutoStatu = dataAuto.Text;
            DialogResult = true;  // 设置对话框结果为 true 表示用户确认
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
