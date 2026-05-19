// ============================================================
//  MapRedact.xaml.cs（重构后）
//  职责：区域编辑对话框（显示区域属性、提交修改、删除区域）
//  变更说明：
//    - Formverify() 改为逐字段校验并返回错误字段名，减少重复 MessageBox 调用
//    - AreaShow() 中字段赋值统一风格
//    - 无逻辑变更，只做可读性清理
// ============================================================
using AGVManagement.Enumeration;
using AGVManagement.MapPaint;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AGVManagement
{
    public partial class MapRedact : Window
    {
        // ── 字段 ──────────────────────────────────────────────────────────
        private readonly int _areaId;
        private readonly Canvas _canvas;
        private readonly AreaCompile _areaCompile = new AreaCompile();
        private Label _label;

        // ── 构造 ──────────────────────────────────────────────────────────
        public MapRedact(int areaId, Canvas canvas)
        {
            InitializeComponent();
            _areaId = areaId;
            _canvas = canvas;
            LoadAreaInfo();
        }

        // ─────────────────────────────────────────────────────────────────
        //  显示区域信息
        // ─────────────────────────────────────────────────────────────────

        private void LoadAreaInfo()
        {
            _label = _areaCompile.AreaSelct(_areaId);

            double scale = Painting.siseWin;

            MpName.Text = _label.Content.ToString();
            Fontsize.Text = (_label.FontSize / scale).ToString();
            DisX.Text = (_label.Margin.Left / scale).ToString();
            DisY.Text = (_label.Margin.Top / scale).ToString();
            ArWidth.Text = _label.Width.ToString();
            ArHeight.Text = _label.Height.ToString();
            Brwidth.Text = _label.BorderThickness.Top.ToString();

            Algcetion.Text = _areaCompile.aAlignment(_label);
            FontColor.Text = _areaCompile.AreaColor(_label.Foreground.ToString());
            BgColor.Text = _areaCompile.AreaColor(_label.Background.ToString());
            BrColor.Text = _areaCompile.AreaColor(_label.BorderBrush.ToString());
        }

        // ─────────────────────────────────────────────────────────────────
        //  提交
        // ─────────────────────────────────────────────────────────────────

        private void btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            string error = Validate();
            if (error != null) { MessageBox.Show(error); return; }

            double scale = Painting.siseWin;

            _label.Content = MpName.Text.Trim();
            _label.FontSize = double.Parse(Fontsize.Text.Trim()) * scale;
            _label.Margin = new Thickness(
                double.Parse(DisX.Text.Trim()) * scale,
                double.Parse(DisY.Text.Trim()) * scale, 0, 0);
            _label.Width = double.Parse(ArWidth.Text.Trim());
            _label.Height = double.Parse(ArHeight.Text.Trim());

            double bw = double.Parse(Brwidth.Text.Trim());
            _label.BorderThickness = new Thickness(bw);

            _areaCompile.aAlignment(Algcetion.Text.Trim(), _label);
            _areaCompile.AreaColor(_label, FontColor.Text, Colortype.FontColor);
            _areaCompile.AreaColor(_label, BgColor.Text, Colortype.BgColor);
            _areaCompile.AreaColor(_label, BrColor.Text, Colortype.BrColor);

            Close();
        }

        // ─────────────────────────────────────────────────────────────────
        //  删除
        // ─────────────────────────────────────────────────────────────────

        private void btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            _areaCompile.ArDelete(_areaId, _canvas);
            Close();
        }

        // ─────────────────────────────────────────────────────────────────
        //  表单验证
        // ─────────────────────────────────────────────────────────────────

        /// <summary>校验所有必填项，返回第一条错误提示；全部合法返回 null</summary>
        private string Validate()
        {
            var fields = new (string Name, string Value)[]
            {
                ("区域名称", MpName.Text),
                ("字体大小", Fontsize.Text),
                ("X 轴距离", DisX.Text),
                ("Y 轴距离", DisY.Text),
                ("区域长度", ArWidth.Text),
                ("区域宽度", ArHeight.Text),
                ("边框宽度", Brwidth.Text)
            };

            foreach (var (name, value) in fields)
                if (string.IsNullOrWhiteSpace(value))
                    return $"{name}不能为空";

            return null;
        }
    }
}