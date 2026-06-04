using AGV.BLL;
using AGVManagement.MapPaint;
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

namespace AGVManagement
{
    /// <summary>
    /// TagLine.xaml 的交互逻辑
    /// </summary>
    public partial class TagLine : Window
    {
        private DataGrid Getgrid;
        private int TagN;
        private int GrIndex;
        private DataTable GetData;
        private string[] taglis;
        private LineMap lineMapMs;
        public long TimeMp;

        private readonly TagInfoBLL _tagInfoBLL = new TagInfoBLL();
        private string _currentQrCode = "";

        //新增弹窗单个逻辑
        private static TagLine _instance;  // 静态实例

        private Dictionary<int, Label> _isolatedValuePairs;
        private List<WirePointArray> _isolatedWirePointArrays;

        private void UseQrCode_Checked(object sender, RoutedEventArgs e)
    => QrCodeDisplay.Foreground = System.Windows.Media.Brushes.Black;

        private void UseQrCode_Unchecked(object sender, RoutedEventArgs e)
            => QrCodeDisplay.Foreground = System.Windows.Media.Brushes.Gray;


        public static void ShowWindow(List<object> list, long Time, bool type, int TagNum, DataGrid grid, int Indx, DataTable data, LineMap Mp, Dictionary<int, Label> isolatedVP = null,
    List<WirePointArray> isolatedWPA = null)
        {
            if (_instance == null || !_instance.IsLoaded)   // 没有窗口 或 已经关闭
            {
                _instance = new TagLine(list, Time, type, TagNum, grid, Indx, data, Mp);
                _instance.Show();
            }
            else
            {
                _instance.Activate(); // 已经有窗口 -> 激活到前台
                _instance.Focus();
            }
        }


        public TagLine(List<object> list, long Time, bool type, int TagNum, DataGrid grid, int Indx, DataTable data, LineMap Mp, Dictionary<int, Label> isolatedVP = null,
    List<WirePointArray> isolatedWPA = null)
        {
            InitializeComponent();
            Getgrid = grid;
            TagN = TagNum;
            GrIndex = Indx;
            GetData = data;
            lineMapMs = Mp;
            TimeMp = Time;

            _isolatedValuePairs = isolatedVP;
            _isolatedWirePointArrays = isolatedWPA;

            LoadLineTag(list, Time, type, TagNum);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _instance = null;// 窗口关闭后清除引用
            Getgrid.SelectedIndex = -1; 
        }

        public void LoadLineTag(List<object> lst, long Times, bool typ, int TagUnm)
        {
            OperateDBBLL operate = new OperateDBBLL();
            if (!typ)
            {
                taglis = operate.SelectTagArr(Times, TagUnm.ToString()).Where(x => x != "N/A").ToArray();//查询关联Tag
                TagNum.ItemsSource = taglis;
                TagNum.Text = lst[0].ToString();
            }
            else
            {
                List<string> ls = new List<string>();

                // 优先用隔离集合（Circuitredact 传进来的局部集合）
                // 没有隔离集合时用全局静态集合（主窗口直接调用的情况）
                var sourceDict = (_isolatedValuePairs != null && _isolatedValuePairs.Count > 0)
                    ? _isolatedValuePairs
                    : MapInstrument.valuePairs;

                foreach (int item in sourceDict.Keys)
                    ls.Add(item.ToString());

                TagNum.ItemsSource = ls;

                string currentTag = lst[0].ToString().Trim();
                TagNum.SelectedItem = ls.FirstOrDefault(x => x == currentTag);
                if (TagNum.SelectedItem == null)
                    TagNum.Text = currentTag;
            }
            speed.ItemsSource = TagCompile.agvSpeed;
            PBS.ItemsSource = TagCompile.agvPbs;
            Turn.ItemsSource = TagCompile.agvTurn;
            Direction.ItemsSource = TagCompile.agvDire;
            Hook.ItemsSource = TagCompile.agvHook;
            Time.ItemsSource = TagCompile.agvTime;

            // === 批量速度下拉框初始化 ===
            BatchSpeed.ItemsSource = new List<string>
    {
        "default",
        "0.5", "0.6", "0.7", "0.8", "1.0", "1.2"
    };

            speed.Text = lst[1].ToString();
            PBS.Text = lst[2].ToString();
            Turn.Text = lst[3].ToString();
            Direction.Text = lst[4].ToString();
            Hook.Text = lst[5].ToString();
            Time.Text = lst[6].ToString();

            // 判断是否有批量速度列（避免旧数据报错）
            if (lst.Count > 8)
                BatchSpeed.Text = lst[8]?.ToString() ?? "default";
            else
                BatchSpeed.Text = "default";

            ChangeProgram.Text = lst[7].ToString();

            // ==== 显示映射 ====
            string changeProgramValue = lst[7].ToString();
            if (changeProgramValue == "default")
                ChangeProgram.Text = "缺省";
            else
                ChangeProgram.Text = changeProgramValue;

            // 读取当前信标的二维码ID
            int currentTagId = 0;
            try
            {
                // 优先从 lst[0] 读（TagLine 打开时传进来的当前行数据，第0列就是 Tag）
                currentTagId = Convert.ToInt32(lst[0].ToString());
            }
            catch
            {
                currentTagId = TagUnm; // 读不到时兜底
            }
            _currentQrCode = _tagInfoBLL.GetQrCode(TimeMp.ToString(), currentTagId);
            QrCodeDisplay.Text = string.IsNullOrEmpty(_currentQrCode)
                ? "（未绑定二维码）" : _currentQrCode;

            // DataTable 加 UseQrCode 列（如果还没有）
            if (!GetData.Columns.Contains("UseQrCode"))
                GetData.Columns.Add("UseQrCode", typeof(string));

            // lst[9] 是 UseQrCode（Tag=0,Speed=1,Pbs=2,Turn=3,Direction=4,Hook=5,Stop=6,ChangeProgram=7,BatchSpeed=8,UseQrCode=9）
            string existingUse = "0";
            try
            {
                if (lst.Count > 9 && lst[9] != null)
                    existingUse = lst[9].ToString();
                else
                    existingUse = ((DataRowView)Getgrid.Items[GrIndex])["UseQrCode"]?.ToString() ?? "0";
            }
            catch { existingUse = "0"; }
            UseQrCode.IsChecked = existingUse == "1";
        }


        /// <summary>
        /// 提交
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            // ── 第一步：确定选中的 Tag 值，起点和非起点分开处理 ──────────────
            string selectedTag;
            if (taglis == null)
            {
                // 起点（typ=true）：SelectedItem 优先，Text 兜底
                selectedTag = TagNum.SelectedItem?.ToString()
                           ?? TagNum.Text?.Trim() ?? "";
            }
            else
            {
                // 非起点：SelectedValue 优先
                selectedTag = TagNum.SelectedValue?.ToString()
                           ?? TagNum.SelectedItem?.ToString()
                           ?? TagNum.Text?.Trim() ?? "";
            }

            if (string.IsNullOrEmpty(selectedTag))
            {
                MessageBox.Show("请选择有效的信标", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ── 第二步：确保 UseQrCode 列存在 ────────────────────────────────
            if (!GetData.Columns.Contains("UseQrCode"))
                GetData.Columns.Add("UseQrCode", typeof(string));

            // ── 第三步：批量速度处理 ──────────────────────────────────────────
            string batchSpeedText = BatchSpeed.SelectedValue?.ToString() ?? "default";
            if (!batchSpeedText.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                foreach (DataRow row in GetData.Rows)
                    row["Speed"] = batchSpeedText;
            }
            else
            {
                ((DataRowView)Getgrid.Items[GrIndex])["Speed"] =
                    speed.SelectedValue?.ToString() ?? speed.Text;
            }

    // ── 第四步：其他字段写入 ──────────────────────────────────────────
    ((DataRowView)Getgrid.Items[GrIndex])["Pbs"] = PBS.SelectedValue?.ToString() ?? PBS.Text;
            ((DataRowView)Getgrid.Items[GrIndex])["Turn"] = Turn.SelectedValue?.ToString() ?? Turn.Text;
            ((DataRowView)Getgrid.Items[GrIndex])["Direction"] = Direction.SelectedValue?.ToString() ?? Direction.Text;
            ((DataRowView)Getgrid.Items[GrIndex])["Hook"] = Hook.SelectedValue?.ToString() ?? Hook.Text;
            ((DataRowView)Getgrid.Items[GrIndex])["Stop"] = Time.SelectedValue?.ToString() ?? Time.Text;

            string changeProgramText = ChangeProgram.Text.Trim();
            if (changeProgramText == "缺省") changeProgramText = "default";
            ((DataRowView)Getgrid.Items[GrIndex])["ChangeProgram"] = changeProgramText;

            // ── 第五步：UseQrCode 写入 ────────────────────────────────────────
            ((DataRowView)Getgrid.Items[GrIndex])["UseQrCode"] =
                (UseQrCode.IsChecked == true && !string.IsNullOrEmpty(_currentQrCode)) ? "1" : "0";

            // ── 第六步：左/右转联动 ───────────────────────────────────────────
            if ((Turn.SelectedValue?.ToString() ?? "").Equals("左转") ||
                (Turn.SelectedValue?.ToString() ?? "").Equals("右转"))
            {
                if (GrIndex != Getgrid.Items.Count - 1)
                    ((DataRowView)Getgrid.Items[GrIndex + 1])["Turn"] = "取消转弯";
            }

            // ── 第七步：Tag 改动处理 ──────────────────────────────────────────
            string currentTag = ((DataRowView)Getgrid.Items[GrIndex])["Tag"].ToString();
            if (currentTag != selectedTag)
            {
                if (taglis != null)
                {
                    int a = Getgrid.Items.Count;
                    for (int i = 0; i < a; i++)
                    {
                        if (i > GrIndex)
                            GetData.Rows.Remove(GetData.Rows[GrIndex + 1]);
                    }
                }
                else
                {
                    int a = Getgrid.Items.Count;
                    for (int i = 0; i < a; i++)
                    {
                        if (i != 0)
                            GetData.Rows[0].Delete();
                    }
                }

                ((DataRowView)Getgrid.Items[GrIndex])["Tag"] = selectedTag;
                Getgrid.ItemsSource = GetData.DefaultView;
                Getgrid.AutoGenerateColumns = false;
                LineRest();
                lineMapMs.GetTags = null;
                lineMapMs.TagClick(TimeMp, Convert.ToInt32(selectedTag),
                    Getgrid, GetData, false,
                    _isolatedValuePairs, _isolatedWirePointArrays);
            }

    ((DataRowView)Getgrid.Items[GrIndex])["Tag"] = selectedTag;
            Getgrid.SelectedIndex = GrIndex;

            this.Close();
        }

        /// <summary>
        /// 线路还原
        /// </summary>
        public void LineRest()
        {
            MapInstrument map = new MapInstrument();
            map.TagFormer();//所有Tag还原为原色
            foreach (var item in MapInstrument.wirePointArrays)
            {
                if (item.GetPath != null)
                {
                    item.GetPath.Stroke = Brushes.Black;
                    item.GetPath.StrokeThickness = 1;
                }
                List<Path> paths = item.Paths;
                if (paths != null)
                {
                    foreach (Path it in paths)
                    {
                        it.Stroke = Brushes.Black;
                        it.StrokeThickness = 1;
                    }
                }
            }
        }

        private void Turn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TagNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Hook_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void PBS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Direction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ChangeProgram_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Time_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
