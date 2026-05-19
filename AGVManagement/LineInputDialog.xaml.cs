using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace AGVManagement
{
    /// <summary>
    /// LineInputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class LineInputDialog : Window
    {
        // ── 属性 ──────────────────────────────────────────────────────────

        /// <summary>用户选中的起始站点，例如 "TA1"</summary>
        public string dataMesStartStatu { get; private set; }

        /// <summary>用户选中的终止站点，例如 "TA5"</summary>
        public string dataMesEndStatu { get; private set; }

        // ── 构造 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 构造函数：传入当前地图的站点表，自动填充起始点和终止点下拉框。
        /// </summary>
        /// <param name="tagTable">
        ///   由 TagInfoBLL.RataTable(Time.ToString()) 返回的 DataTable，
        ///   需包含 TagName 列（存储数字部分，如 "3" 对应 TA3）。
        /// </param>
        public LineInputDialog(DataTable tagTable)
        {
            InitializeComponent();
            LoadTagOptions(tagTable);
        }

        //
        private void LoadTagOptions(DataTable tagTable)
        {
            if (tagTable == null) return;

            foreach (DataRow row in tagTable.Rows)
            {
                string tagName = row["TagName"]?.ToString();
                if (!string.IsNullOrWhiteSpace(tagName))
                {
                    string item = "TA" + tagName;
                    dataMesStart.Items.Add(item);
                    dataMesEnd.Items.Add(item);
                }
            }

            // 默认选中第一项
            if (dataMesStart.Items.Count > 0)
            {
                dataMesStart.SelectedIndex = 0;
                dataMesEnd.SelectedIndex = 0;
            }
        }

        // ── 事件 ──────────────────────────────────────────────────────────

        private void OnCompleteButtonClick(object sender, RoutedEventArgs e)
        {
            dataMesStartStatu = dataMesStart.SelectedItem?.ToString();
            dataMesEndStatu = dataMesEnd.SelectedItem?.ToString();
            DialogResult = true;
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}