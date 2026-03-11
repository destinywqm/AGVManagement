using MQTTnet.Client;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client.Options;
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
using System.Net;
using System.Data;
using AGV.DAL;

namespace AGVManagement
{
    public partial class mqtt : Window
    {
        public string Address { get; private set; }
        public int Port { get; private set; }
        public double SelectedLength { get; private set; }
        public double SelectedWidth { get; private set; }

        public mqtt()
        {
            InitializeComponent();
            LoadCarModels();
        }

        private void LoadCarModels()
        {
            try
            {
                var dt = MySqlHelper.ExecuteDataTable("SELECT * FROM CarModel");

                // 默认车型
                var defaultRow = dt.NewRow();
                defaultRow["Name"] = "标准AGV (1.2×0.8)";
                defaultRow["Length"] = 1.2;
                defaultRow["Width"] = 0.8;
                dt.Rows.InsertAt(defaultRow, 0);

                CarModelComboBox.ItemsSource = dt.DefaultView;
                CarModelComboBox.SelectedIndex = 0;
            }
            catch
            {
                // 数据库异常时，兜底默认项
                CarModelComboBox.Items.Clear();
                CarModelComboBox.Items.Add(new { Name = "标准AGV (1.2×0.8)", Length = 1.2, Width = 0.8 });
                CarModelComboBox.SelectedIndex = 0;
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Address = IpAddressTextBox.Text;
            Port = int.TryParse(PortTextBox.Text, out int p) ? p : 0;

            if (CarModelComboBox.SelectedItem is DataRowView row)
            {
                SelectedLength = Convert.ToDouble(row["Length"]);
                SelectedWidth = Convert.ToDouble(row["Width"]);
            }
            else
            {
                SelectedLength = 1.2;
                SelectedWidth = 0.8;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
