using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AGVManagement.MapPaint
{
    public class DefaultToChineseConverter : IValueConverter
    {
        // 数据 -> 界面
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            string str = value.ToString();
            return str == "default" ? "缺省" : str;
        }

        // 界面 -> 数据（这里必须原样返回，不要替换回“缺省”）
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            string str = value.ToString();
            return str == "缺省" ? "default" : str;  // 界面改了也能写回 default
        }
    }
}
