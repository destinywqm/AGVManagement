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
    /// AddCar.xaml 的交互逻辑
    /// </summary>
    public partial class AddCar : Window
    {
        public AddCar()
        {
            InitializeComponent();
        }

        
        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OFFCar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 车辆数据提交调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CarSum_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator())
                return;
            this.Close();
            //long gs = 0;
            //MainWindow mainWindow = new MainWindow();
        }

        /// <summary>
        /// 验证函数
        /// </summary>
        /// <returns></returns>
        private bool Validator()
        {
            if (CarSizeN.Text.Trim() == "")
            {
                return false;
            }
            else if (CarSizeW.Text.Trim() == "")
            {
                return false;
            }
            else if (CarSizeH.Text.Trim() == "")
            {
                return false;
            }
            else if (CarSizeL.Text.Trim() == "")
            {
                return false;
            }
            return true;
        }
    }
}
