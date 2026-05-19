using AGV.BLL;
using AGV.DAL;
using AGVManagement.Helpers;
using AGVManagement.MapPaint;
using AGVManagement.Models;
using AGVManagement.Mqtt;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AGVManagement.Services
{
    /// <summary>
    /// MES 任务调度服务
    /// 提取自 MainWindow：ProcessTasksSequentially / ProcessTask /
    ///   FetchTasksFromApiAsync / CheckTaskCompletionStatus /
    ///   DeleteTaskFromDatabase / SendTaskCompletionReport
    /// 依赖：PathHelper、MqttPublishHelper、MqttConnectionManager
    /// </summary>
    public class TaskService
    {
        private static readonly HttpClient _http = new HttpClient();

        private readonly LineInfoBLL _lineBLL;
        private readonly RouteInfoBLL _routeBLL;
        private readonly TagInfoBLL _tagBLL;
        private readonly Painting _painting;
        private readonly PointHandle _pointHandle;
        private readonly Dispatcher _dispatcher;

        public TaskService(LineInfoBLL lineBLL, RouteInfoBLL routeBLL,
            TagInfoBLL tagBLL, Painting painting, PointHandle pointHandle,
            Dispatcher dispatcher)
        {
            _lineBLL = lineBLL;
            _routeBLL = routeBLL;
            _tagBLL = tagBLL;
            _painting = painting;
            _pointHandle = pointHandle;
            _dispatcher = dispatcher;
        }

        // ─────────────────────────────────────────────────────────────────
        //  主循环
        // ─────────────────────────────────────────────────────────────────

        /// <summary>顺序处理数据库中的 MES 任务（死循环，需在独立 Task 中运行）</summary>
        public async Task ProcessTasksSequentiallyAsync()
        {
            try
            {
                while (true)
                {
                    DataTable tasks = MySqlHelper.ExecuteDataTableEnableNull(
                        "SELECT taskId, origin, destination, startTime FROM tasks ORDER BY startTime ASC");

                    if (tasks.Rows.Count > 0)
                    {
                        DataRow row = tasks.Rows[0];
                        var task = new TaskModel
                        {
                            TaskId = Convert.ToInt64(row["TaskId"]),
                            Origin = row["Origin"].ToString(),
                            Destination = row["Destination"].ToString()
                        };

                        await ExecuteTaskAsync(task);

                        if (await WaitForTaskCompletionAsync(task.TaskId))
                        {
                            DeleteTask(task.TaskId);
                            if (task.Destination != "TA8")
                                await SendCompletionReportAsync(task.TaskId, "已完成");
                        }
                    }

                    await Task.Delay(5000);
                }
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                    MessageBox.Show($"MES 连接错误：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  单任务执行
        // ─────────────────────────────────────────────────────────────────

        private async Task ExecuteTaskAsync(TaskModel task)
        {
            try
            {
                DataTable routeStation = _routeBLL.RoutelistArrer(MainWindow.Time.ToString());
                DataTable tagStation = _tagBLL.RataTable(MainWindow.Time.ToString());

                // 反变换当前 AGV 坐标到地图坐标系
                var rawData = JsonSerializer.Deserialize<CarData>(MqttClientWrapper.payloadAll);
                var rawPt = new System.Windows.Point(rawData.X, rawData.Y);
                var worldPt = _painting.ReverseTransformCoordinate(
                                   rawPt,
                                   MqttClientWrapper.ActualWidthNow,
                                   MqttClientWrapper.ActualHeightNow,
                                   MqttClientWrapper.ProportionNow);
                var currentPos = new System.Windows.Point(worldPt.X / 10, worldPt.Y / 10);

                int rowIdx = _pointHandle.FindRowByTagsNameNew(
                    routeStation, task.Origin, task.Destination, currentPos, tagStation);

                if (rowIdx == -2) { GlobalStatus.Status = "已完成"; return; }

                var mesEndPoints = _pointHandle.FindMesEnd(
                    routeStation, MainWindow.Time.ToString(), rowIdx);

                var processed = PathHelper.ProcessPointsByTurnRotate(mesEndPoints, 0.001);
                var stationData = new NewStationData();
                stationData.Stations.AddRange(processed);

                string json = JsonSerializer.Serialize(stationData,
                                        new JsonSerializerOptions { WriteIndented = true });
                bool taskDone = false;
                var startTime = DateTime.Now;

                var client = MqttConnectionManager.LatestClient;
                if (client == null || !client.IsConnected)
                {
                    Console.WriteLine("MQTT 未连接，无法订阅任务完成消息！");
                    return;
                }

                await client.SubscribeAsync("AGV/Response/TaskDone");
                client.Client.UseApplicationMessageReceivedHandler(e =>
                {
                    if (e.ApplicationMessage.Topic == "AGV/Response/TaskDone"
                        && Encoding.UTF8.GetString(e.ApplicationMessage.Payload) == "已完成")
                        taskDone = true;
                });

                while (!taskDone)
                {
                    if (client.IsConnected)
                    {
                        await client.PublishAsync(BuildMsg("AGV/Carrier/MapLineMes", json));
                        await client.PublishAsync(BuildMsg("AGV/Response/TaskDistribution", "TaskDistribution"));
                    }

                    if ((DateTime.Now - startTime).TotalSeconds >= 30)
                    {
                        Console.WriteLine("任务下发超时（30 秒）");
                        break;
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"任务处理异常：{ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  辅助
        // ─────────────────────────────────────────────────────────────────

        private static MQTTnet.MqttApplicationMessage BuildMsg(string topic, string payload)
            => new MqttApplicationMessageBuilder()
                .WithTopic(topic).WithPayload(payload)
                .WithExactlyOnceQoS().WithRetainFlag(false)
                .Build();

        private static async Task<bool> WaitForTaskCompletionAsync(long taskId)
        {
            while (GlobalStatus.Status != "已完成")
                await Task.Delay(2000);
            return true;
        }

        private static void DeleteTask(long taskId)
            => MySqlHelper.ExecuteDataTableDelete(
                $"DELETE FROM tasks WHERE TaskId = {taskId}");

        private static async Task SendCompletionReportAsync(long taskId, string status)
        {
            var body = new { orderId = taskId.ToString(), status };
            var content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            try
            {
                var resp = await _http.PostAsync(
                    "http://192.168.1.100:8084/integration/agv/agvStatus", content);
                bool ok = resp.IsSuccessStatusCode;
                string errMsg = ok ? "" : await resp.Content.ReadAsStringAsync();
                _ = Task.Run(() => MySqlHelper.InsertTaskCompletionLog(taskId, status, ok, errMsg));
            }
            catch (Exception ex)
            {
                _ = Task.Run(() => MySqlHelper.InsertTaskCompletionLog(taskId, status, false, ex.Message));
            }
        }

        /// <summary>从 API 拉取任务列表（备用，当前主路径走数据库）</summary>
        public static async Task<List<TaskModel>> FetchTasksFromApiAsync()
        {
            try
            {
                var resp = await _http.GetAsync("http://192.168.137.99:8002/api/agvschedule");
                if (!resp.IsSuccessStatusCode)
                {
                    MessageBox.Show("获取任务失败: " + resp.StatusCode);
                    return new List<TaskModel>();
                }
                var json = await resp.Content.ReadAsStringAsync();
                var tasks = JsonSerializer.Deserialize<List<TaskModel>>(json);
                return tasks.OrderBy(t => t.TaskId).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("API 请求异常: " + ex.Message);
                return new List<TaskModel>();
            }
        }
    }
}