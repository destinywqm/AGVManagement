using System;
using System.Collections.Generic;
using System.Data;
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
    public partial class CarListWindow : Window
    {
        public CarListWindow()
        {
            InitializeComponent();
            LoadCars();
        }

        // 确保表存在
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
        private void LoadCars()
        {
            EnsureCarTableExists();

            var dt = MySqlHelper.ExecuteDataTable("SELECT * FROM CarModel");
            VehicleGrid.ItemsSource = dt.DefaultView;
        }

        // 新增车辆
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var editor = new CarEditorWindow();
            if (editor.ShowDialog() == true)
            {
                LoadCars(); // 刷新列表
            }
        }

        // 编辑车辆
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (VehicleGrid.SelectedItem is DataRowView row)
            {
                int id = (int)row["Id"];
                var editor = new CarEditorWindow(id);
                if (editor.ShowDialog() == true)
                {
                    LoadCars(); // 刷新列表
                }
            }
            else
            {
                MessageBox.Show("请先选择一辆车辆进行编辑！");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (VehicleGrid.SelectedItem is DataRowView row)
            {
                int id = (int)row["Id"];
                string name = row["Name"].ToString();

                // 确认提示
                var result = MessageBox.Show(
                    $"确定要删除车辆：{name} (ID={id}) 吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string sql = "DELETE FROM CarModel WHERE Id = @Id";
                        MySqlHelper.ExecuteNonQuery(sql, new MySql.Data.MySqlClient.MySqlParameter("@Id", id));

                        MessageBox.Show("车辆删除成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCars(); // 刷新列表
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择一辆车辆进行删除！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

