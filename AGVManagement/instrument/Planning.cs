using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data;

namespace AGVManagement.instrument
{
    public class Planning
    {
        // --- 数据模型 ---
        // --- 数据模型 ---
        public class PointData
        {
            public double x { get; set; }
            public double y { get; set; }
            public int id { get; set; }
            public List<int> neighbor_ids { get; set; } = new List<int>();
        }

        public class TaskData
        {
            public string agent_name { get; set; }
            public long timestamp { get; set; } = 0;
            public int start_id { get; set; }
            public int goal_id { get; set; }
            public int task_priority { get; set; }
        }

        public class Cluster
        {
            public string name { get; set; }
            public List<int> area { get; set; } = new List<int>();
            public List<int> parking_area { get; set; } = new List<int>();
            public List<int> temp_area { get; set; } = new List<int>();
            public Dictionary<string, List<int>> entry_area { get; set; } = new Dictionary<string, List<int>>();
            public Dictionary<string, List<int>> exit_area { get; set; } = new Dictionary<string, List<int>>();
            public int light { get; set; } = 0;
            public long time { get; set; } = 0;
            public long forward_green_start_time { get; set; } = 0;
            public long backward_green_start_time { get; set; } = 1000000000000;
            public int fix_per_cycle { get; set; } = 2;
            public List<int> direction { get; set; } = new List<int>();
        }

        public class ClusterWrapper
        {
            public string name { get; set; } = "clusters";
            public List<Cluster> clusters { get; set; } = new List<Cluster>();
        }

        public class MapJson
        {
            public List<PointData> Points { get; set; } = new List<PointData>();
            public List<TaskData> Tasks { get; set; } = new List<TaskData>();
            public ClusterWrapper Clusters { get; set; } = new ClusterWrapper();
        }

        // --- 工具方法: 将 DataTable 转为 PointData ---
        public static List<PointData> ConvertToPointList(DataTable table)
        {
            var result = new List<PointData>();
            foreach (DataRow row in table.Rows)
            {
                string tagName = row["TagName"].ToString();
                double x = Convert.ToDouble(row["X"]);
                double y = Convert.ToDouble(row["Y"]);
                string neighborsRaw = table.Columns.Contains("NeighborIds") ? row["NeighborIds"].ToString() : "";
                int id = ExtractId(tagName);

                var neighborList = new List<int>();
                if (!string.IsNullOrWhiteSpace(neighborsRaw))
                {
                    neighborList = neighborsRaw
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => ExtractId(n.Trim()))
                        .Where(v => v >= 0)
                        .ToList();
                }
                        
                result.Add(new PointData
                {
                    x = x,
                    y = y,
                    id = id,
                    neighbor_ids = neighborList
                });
            }
            return result;
        }

        private static int ExtractId(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return -1;
            string digits = new string(tag.Where(char.IsDigit).ToArray());

            return int.TryParse(digits, out int id) ? id : -1;
        }

        // --- 主方法: 构建新 JSON ---
        public static string BuildMapJson(DataTable pointTable, List<(string agent_name, int start_id, int goal_id, int task_priority, long? timestamp)> tasksInput, ClusterWrapper clusters = null)
        {
            var mapJson = new MapJson();

            // 1. Points
            mapJson.Points = ConvertToPointList(pointTable);

            // 2. Tasks
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var t in tasksInput)
            {
                mapJson.Tasks.Add(new TaskData
                {
                    agent_name = t.agent_name,
                    start_id = t.start_id,
                    goal_id = t.goal_id,
                    task_priority = t.task_priority,
                    timestamp = t.timestamp.HasValue && t.timestamp.Value != 0 ? t.timestamp.Value : nowMs
                });
            }

            // 3. Clusters
            mapJson.Clusters = clusters ?? new ClusterWrapper();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(mapJson, options);
        }

    }
}
