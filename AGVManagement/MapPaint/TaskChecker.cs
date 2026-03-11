using System;
using System.Data;
using System.Timers;
using MySql.Data.MySqlClient;
using AGV.DAL;

namespace AGVManagement.MapPaint
{
    public class TaskChecker
    {
        AGV.DAL.MySqlHelper mySqlHelper = new AGV.DAL.MySqlHelper();
        private Timer _timer;
        public DataTable TasksChecks;

        public TaskChecker()
        {
            // 设置定时器每5秒触发一次
            _timer = new Timer(5000);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            // 查询任务并处理
            CheckForNewTasks();
        }
        public void CheckForNewTasks()
        {
            // 查询数据库中的任务
            string query = "SELECT taskId, origin, destination FROM tasks WHERE status = '任务中'";
            TasksChecks = AGV.DAL.MySqlHelper.ExecuteDataTable(query);
        }

    }
}
