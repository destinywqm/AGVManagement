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
    /// RepositioningTag.xaml 的交互逻辑
    /// </summary>
    public partial class RepositioningTag : Window
    {
        public string dataResStatu { get; private set; }

        public string dataResAngel { get; private set; }

        public RepositioningTag()
        {
            InitializeComponent();
        }

        private void OnCompleteButtonClick(object sender, RoutedEventArgs e)
        {
            // 在这里验证输入并关闭窗口
            dataResStatu = dataMes.Text;
            if (dataMes3.SelectedItem is ComboBoxItem selectedItem)
            {
                dataResAngel = selectedItem.Content.ToString(); // 使用 Content 属性
            }
            DialogResult = true;  // 设置对话框结果为 true 表示用户确认
            Close();
        }

        private void dataMes3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 获取选中的值
            if (dataMes3.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedValue = selectedItem.Content.ToString();
            }
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
