using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace AGVManagement.Mqtt
{
    public class AgvInfoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _agv;
        public string AGV
        {
            get => _agv;
            set { _agv = value; OnPropertyChanged(nameof(AGV)); }
        }

        private string _network;
        public string 网络状态
        {
            get => _network;
            set { _network = value; OnPropertyChanged(nameof(网络状态)); }
        }

        private string _running;
        public string 运行状态
        {
            get => _running;
            set { _running = value; OnPropertyChanged(nameof(运行状态)); }
        }

        private string _x;
        public string 车辆坐标X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(nameof(车辆坐标X)); }
        }

        private string _y;
        public string 车辆坐标Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(nameof(车辆坐标Y)); }
        }

        private string _voltage;
        public string 电压
        {
            get => _voltage;
            set { _voltage = value; OnPropertyChanged(nameof(电压)); }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
