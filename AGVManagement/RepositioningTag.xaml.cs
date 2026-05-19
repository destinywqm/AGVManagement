using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace AGVManagement
{
    /// <summary>
    /// RepositioningTag.xaml 的交互逻辑
    /// </summary>
    public partial class RepositioningTag : Window
    {
        // ── 属性 ──────────────────────────────────────────────────────────

        /// <summary>用户选中的站点名称，例如 "TA3"</summary>
        public string dataResStatu { get; private set; }

        /// <summary>用户选中的角度值</summary>
        public string dataResAngel { get; private set; }

        // ── 构造 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 构造函数：传入当前地图的站点表，自动填充站点下拉框。
        /// </summary>
        /// <param name="tagTable">
        ///   由 TagInfoBLL.RataTable(Time.ToString()) 返回的 DataTable，
        ///   需包含 TagName 列（存储数字部分，如 "3" 对应 TA3）。
        /// </param>
        public RepositioningTag(DataTable tagTable)
        {
            InitializeComponent();
            LoadTagOptions(tagTable);
        }

        // ── 私有方法 ──────────────────────────────────────────────────────

        /// <summary>从 DataTable 读取所有 TagName，拼成 "TA{n}" 后填入下拉框。</summary>
        private void LoadTagOptions(DataTable tagTable)
        {
            if (tagTable == null) return;

            foreach (DataRow row in tagTable.Rows)
            {
                string tagName = row["TagName"]?.ToString();
                if (!string.IsNullOrWhiteSpace(tagName))
                    dataMes.Items.Add("TA" + tagName);
            }

            // 默认选中第一项，方便操作
            if (dataMes.Items.Count > 0)
                dataMes.SelectedIndex = 0;
        }

        // ── 事件 ──────────────────────────────────────────────────────────

        private void OnCompleteButtonClick(object sender, RoutedEventArgs e)
        {
            // 站点：取下拉框选中值
            dataResStatu = dataMes.SelectedItem?.ToString();

            // 角度：取 ComboBox 选中的 ComboBoxItem
            if (dataMes3.SelectedItem is ComboBoxItem selectedItem)
                dataResAngel = selectedItem.Content.ToString();

            DialogResult = true;
            Close();
        }

        private void dataMes3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 保留原有事件钩子，如需联动逻辑可在此扩展
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}