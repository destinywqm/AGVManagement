using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVManagement.MapPaint
{
    public static class GlobalStatus
    {
        // 静态变量用于存储状态
        private static string _status;

        // 提供一个公共属性来访问和更新状态
        public static string Status
        {
            get => _status;
            set
            {
                _status = value;
            }
        }
    }

}
