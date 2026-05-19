using AGV.Models.Models;
using AGVDLL;
using AGVManagement.Mqtt;
using System;
using System.Threading;
using System.Windows;

namespace AGVManagement.Services
{
    /// <summary>
    /// 串口管理服务
    /// 提取自 Main.xaml.cs：OpenPort / AGVClear / ReadAgvStatus / PollAgvStatus
    /// 调用方（Main 窗口）通过回调获取状态刷新通知
    /// </summary>
    public class PortService
    {
        // ── 回调 ──────────────────────────────────────────────────────────
        /// <summary>每次轮询完一轮后触发（UI 线程外，需 Dispatcher.Invoke）</summary>
        public event Action PollTick;

        // ─────────────────────────────────────────────────────────────────
        //  打开串口
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 按 PortInfo 配置逐一打开串口，全部成功后启动回读 + 轮询线程
        /// </summary>
        /// <returns>全部成功返回 true</returns>
        public bool OpenPort()
        {
            Clear();
            bool allOk = true;

            for (int i = 0; i < PortInfo.AGVCom.Count; i++)
            {
                var dll = new AGVDLL.AGVDLL { dllName = i.ToString() };
                int groupNo = i + 1;
                var ptr = dll.openPort(groupNo, PortInfo.AGVCom[i], PortInfo.Baud[i],
                                          MainInfo.prity, MainInfo.stopBits);

                if (ptr.ToInt32() == 0)
                {
                    MessageBox.Show($"打开串口：COM{PortInfo.AGVCom[i]} 失败！");
                    allOk = false;
                    break;
                }

                MainInfo.listAgvDll.Add(dll);
                MainInfo.listPtr.Add(ptr);
                MainInfo.agvThState = true;

                var readThread = new Thread(new ParameterizedThreadStart(ReadAgvStatus))
                { IsBackground = true };
                readThread.Start(groupNo);
                MainInfo.GetThreads.Add(readThread);
            }

            if (!allOk) return false;

            MainInfo.AgvStaticMessg = true;
            var pollThread = new Thread(PollLoop) { IsBackground = true };
            pollThread.Start();
            MainInfo.GetThreads.Add(pollThread);
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  关闭串口
        // ─────────────────────────────────────────────────────────────────

        /// <summary>关闭所有已打开串口，停止所有线程</summary>
        public void ClosePort()
        {
            for (int i = 0; i < PortInfo.AGVCom.Count; i++)
            {
                if (MainInfo.listAgvDll[i].closePort(i + 1) == 1)
                    MainInfo.listPtr[i] = new IntPtr(0);
                else
                    MessageBox.Show($"关闭串口 COM{PortInfo.agv[i]} 失败！");
            }

            MainInfo.agvThState = false;
            MainInfo.AgvStaticMessg = false;
            Clear();
        }

        // ─────────────────────────────────────────────────────────────────
        //  AGV 状态回读（后台线程）
        // ─────────────────────────────────────────────────────────────────

        private void ReadAgvStatus(object groupNoObj)
        {
            int gn = Convert.ToInt32(groupNoObj);
            int idx = gn - 1;
            string[] comAgv = PortInfo.agv[idx].Split(',');

            foreach (string s in comAgv)
                MainInfo.carStatusList[Convert.ToInt32(s)] = new CarStatus();

            while (MainInfo.agvThState)
            {
                MainInfo.listAgvDll[idx].agvPortClearCache(gn);
                foreach (string s in comAgv)
                {
                    int key = Convert.ToInt32(s);
                    MainInfo.carStatusList[key] =
                        MainInfo.listAgvDll[idx].read(MainInfo.listPtr[idx], gn, key);
                }
                Thread.Sleep(200);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  轮询循环（后台线程）
        // ─────────────────────────────────────────────────────────────────

        private void PollLoop()
        {
            while (MainInfo.AgvStaticMessg)
            {
                PollTick?.Invoke();
                Thread.Sleep(200);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  清空
        // ─────────────────────────────────────────────────────────────────

        /// <summary>终止所有线程，清空全局状态集合</summary>
        public void Clear()
        {
            foreach (var t in MainInfo.GetThreads) try { t.Abort(); } catch { }
            MainInfo.GetThreads.Clear();
            MainInfo.listAgvDll.Clear();
            MainInfo.listPtr.Clear();
            MainInfo.carStatusList.Clear();
            MainInfo.agvNo.Clear();
        }

        /// <summary>查找指定 AGV 号的状态对象，不存在返回 null</summary>
        public static CarStatus FindCarStatus(int agvNum)
            => MainInfo.carStatusList.TryGetValue(agvNum, out var s) ? s : null;
    }
}