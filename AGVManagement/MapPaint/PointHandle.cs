using AGV.BLL;
using AGVManagement.Mqtt;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Windows;
using static AGVManagement.MainWindow;

namespace AGVManagement.MapPaint
{
    public class PointHandle
    {
        // 工具函数
        public List<newPointStraightWithAngle> ProcessPointsByRotate(List<Tuple<Point, int, double, double>> transformedPointsByRotate, double insertDistance)
        {
            List<newPointStraightWithAngle> result = new List<newPointStraightWithAngle>();
            int idCounter = 0;

            for (int i = 0; i < transformedPointsByRotate.Count - 1; i++)
            {
                Point p1 = transformedPointsByRotate[i].Item1;
                Point p2 = transformedPointsByRotate[i + 1].Item1;
                int value1 = transformedPointsByRotate[i].Item2;
                int value2 = transformedPointsByRotate[i + 1].Item2;

                double value3 = transformedPointsByRotate[i].Item3;
                double value5 = transformedPointsByRotate[i + 1].Item3;

                double value4 = transformedPointsByRotate[i].Item4;
                double value6 = transformedPointsByRotate[i + 1].Item4;

                // 原始点
                double angleToNext = CalculateAngle(p1.X, p1.Y, p2.X, p2.Y); // 假设该函数返回弧度
                result.Add(new newPointStraightWithAngle(idCounter++, p1.X, p1.Y, angleToNext, value3, value4, value1, true, 0));

                // 插入点
                double totalDistance = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                if (totalDistance > insertDistance)
                {
                    double ratio = insertDistance / totalDistance;
                    double newX = p2.X - ratio * (p2.X - p1.X);
                    double newY = p2.Y - ratio * (p2.Y - p1.Y);
                    angleToNext = CalculateAngle(newX, newY, p2.X, p2.Y);  // 返回弧度
                    result.Add(new newPointStraightWithAngle(idCounter++, newX, newY, angleToNext, value5, value6, 0, false, 0));
                }
            }

            // 添加最后一个原始点并处理延伸逻辑
            if (transformedPointsByRotate.Count > 0)
            {
                Point lastPoint = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item1;
                int lastValue = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item2;

                // 保持 v速度 -1
                double lastVSpide = -1;
                double lastASpide = transformedPointsByRotate[transformedPointsByRotate.Count - 1].Item4;

                // 如果列表中至少有两个点，则计算最后一个点的原始方向
                double angleToPrevious = 0;
                if (transformedPointsByRotate.Count > 1)
                {
                    Point secondLastPoint = transformedPointsByRotate[transformedPointsByRotate.Count - 2].Item1;
                    angleToPrevious = CalculateAngle(secondLastPoint.X, secondLastPoint.Y, lastPoint.X, lastPoint.Y);  // 弧度
                }

                // 将最后一个点的角度反转，并保持其原始方向，确保角度在正负180度（弧度制下即正负π）之间
                double reversedAngle = NormalizeAngle(angleToPrevious + Math.PI); // 反转180度 (π弧度)
                result.Add(new newPointStraightWithAngle(idCounter++, lastPoint.X, lastPoint.Y, reversedAngle, lastVSpide, lastASpide, lastValue, true, 0));

                // 沿着最后一个点的 **原始方向** 延伸0.3米计算新点
                double extendDistance = 0.3;
                double newLastX = lastPoint.X + extendDistance * Math.Cos(angleToPrevious); // 使用原始方向延伸 (弧度)
                double newLastY = lastPoint.Y + extendDistance * Math.Sin(angleToPrevious);

                // 处理延伸点和最后一个原始点之间的插入点逻辑
                double distanceBetweenLastAndExtended = Math.Sqrt(Math.Pow(newLastX - lastPoint.X, 2) + Math.Pow(newLastY - lastPoint.Y, 2));
                if (distanceBetweenLastAndExtended > insertDistance)
                {
                    double ratio = insertDistance / distanceBetweenLastAndExtended;
                    double newInsertX = newLastX - ratio * (newLastX - lastPoint.X);
                    double newInsertY = newLastY - ratio * (newLastY - lastPoint.Y);
                    result.Add(new newPointStraightWithAngle(idCounter++, newInsertX, newInsertY, reversedAngle, -1, lastASpide, 0, false, 1)); // 插入点
                }

                // 添加延伸的点，v速度保持为 -1，并且 isStraight 设置为 true
                result.Add(new newPointStraightWithAngle(idCounter++, newLastX, newLastY, reversedAngle, -1, lastASpide, 0, true, 1)); // 延伸点 vSpeed 设为 -1，isStraight 为 true
            }

            return result;
        }

        // 将角度规范化到 -π 到 π 范围内（弧度制）
        private double NormalizeAngle(double angle)
        {
            if (angle > Math.PI || Math.Abs(angle - Math.PI) < 0.00001) // 如果角度大于π或接近π，转换为-π
                return angle - 2*Math.PI;
            else if (angle < -Math.PI)
                return angle + 2 * Math.PI;
            else
                return angle;
        }

        //索引数据行列
        public int FindRowByTags(DataTable dataTable, string dataMes, string dataEndMes)
        {
            try
            {
                // 提取 dataMes 和 dataEndMes 后面的数字
                string startTag = dataMes.Substring(2); // "Tag1" -> "1"
                string endTag = dataEndMes.Substring(2); // "Tag5" -> "5"

                // 遍历 DataTable 的每一行
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string tagColumn = dataTable.Rows[i]["Tag"].ToString(); // 获取 Tag 列的值
                    string[] tagValues = tagColumn.Split(','); // 用逗号分割成数组

                    if (tagValues.Length > 0)
                    {
                        // 获取第一个和最后一个 Tag 值
                        string firstTag = tagValues[0];
                        string lastTag = tagValues[tagValues.Length - 1];

                        // 检查是否与 dataMes 和 dataEndMes 提取的数字相匹配
                        if (firstTag == startTag && lastTag == endTag)
                        {
                            return i; // 返回行号（最小的行数）
                        }
                    }
                }

                // 如果没有找到匹配的行
                throw new Exception("未找到匹配的行");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // 显示错误消息
                return -1; // 返回 -1 表示未找到
            }
        }

        public int FindRowByTagsName(DataTable dataTable, string dataMes, string dataEndMes)
        {
            try
            {
                // 提取 dataMes 和 dataEndMes 后的数字
                string startTag = dataMes.Substring(2); // 例如 "Tag2" -> "2"
                string endTag = dataEndMes.Substring(2); // 例如 "Tag5" -> "5"

                // 组合查询的 Name 值，例如：2-5
                string searchName = $"{startTag}-{endTag}";

                // 遍历 DataTable 查找匹配的 Name 值
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string nameValue = dataTable.Rows[i]["Name"].ToString(); // 获取 Name 字段的值

                    if (nameValue == searchName)
                    {

                        return i; // 返回匹配的行索引
                    }
                }

                // 如果未找到匹配的行
                throw new Exception("未找到匹配的行");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // 显示错误消息
                return -1; // 返回 -1 表示未找到
            }
        }

        public int FindRowByTagsNameNew(DataTable dataTable, string dataMes, string dataEndMes, Point newPoint3, DataTable tagStation)
        {

            try
            {
                // 提取 dataMes 和 dataEndMes 后的数字
                string startTag = dataMes.Substring(2); // 例如 "Tag2" -> "2"
                string endTag = dataEndMes.Substring(2); // 例如 "Tag5" -> "5"

                // 组合查询的 Name 值，例如：2-5
                string searchName = $"{startTag}-{endTag}";

                // 特判：如果路线是1-8 且车辆离 Tag8 更近，则直接认为任务已完成
                if (searchName == "1-8")
                {
                    // 找出 Tag8 的坐标
                    var tag8Station = tagStation.AsEnumerable()
                        .FirstOrDefault(s => s["TagName"].ToString() == "Tag8");

                    if (tag8Station != null)
                    {
                        double tag8X = Convert.ToDouble(tag8Station["X"]);
                        double tag8Y = Convert.ToDouble(tag8Station["Y"]);

                        double distanceToTag8 = Math.Sqrt(Math.Pow(tag8X - newPoint3.X, 2) + Math.Pow(tag8Y - newPoint3.Y, 2));

                        // 如果距离足够近
                        if (distanceToTag8 < 1)
                        {
                            return -2; // 自定义返回值
                        }
                    }
                }

                // 存储符合条件的路线行
                List<DataRow> matchedRows = new List<DataRow>();

                // 遍历 DataTable 查找匹配的 Name 值
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string nameValue = dataTable.Rows[i]["Name"].ToString(); // 获取 Name 字段的值

                    if (nameValue == searchName)
                    {
                        matchedRows.Add(dataTable.Rows[i]);
                    }
                }

                // 如果没有找到匹配的路线，抛出异常
                if (matchedRows.Count == 0)
                {
                    throw new Exception("未找到匹配的路线");
                }

                // 计算每一条匹配路线的起点与车辆当前位置的距离
                DataRow closestRow = null;
                double closestDistance = double.MaxValue;

                foreach (var row in matchedRows)
                {
                    // 获取路线的起始站点 (Tag字段)
                    string startTagName = row["Tag"].ToString().Split(',')[0]; // 获取第一个 Tag 即起点

                    // 查找 TagStation 表中与起点相对应的站点
                    var station = tagStation.AsEnumerable()
                        .FirstOrDefault(s => s["TagName"].ToString() == startTagName);

                    if (station != null)
                    {
                        double tagX = Convert.ToDouble(station["X"]);
                        double tagY = Convert.ToDouble(station["Y"]);

                        // 计算车辆当前位置与站点的欧几里得距离
                        double distance = Math.Sqrt(Math.Pow(tagX - newPoint3.X, 2) + Math.Pow(tagY - newPoint3.Y, 2));

                        // 如果当前距离更近，则更新最近的路线
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestRow = row;
                        }
                    }
                }

                // 如果找到了最近的路线，则返回其行索引
                if (closestRow != null)
                {
                    return dataTable.Rows.IndexOf(closestRow);
                }
                else
                {
                    throw new Exception("没有找到匹配的起点站点");
                  }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // 显示错误消息
                return -1; // 返回 -1 表示未找到
            }
        }



        public List<Tuple<Point, int, double, double, double, double>> FindMesEnd(DataTable dataTable, string time, int arrange)
        {
            MapMessageBLL messageBLL = new MapMessageBLL();
            TagInfoBLL tagInfo = new TagInfoBLL();
            Circuitredact  circuitredact = new Circuitredact();
            Painting painting = new Painting();

            DataTable itemTag = tagInfo.RataTable(time);

            //获取列数据
            string newTag = dataTable.Rows[arrange]["Tag"].ToString();
            string[] newTagar = newTag.Split(',');

            string newSpeed = dataTable.Rows[arrange]["Speed"].ToString();
            string[] newSpeedar = newSpeed.Split(',');

            string newHook = dataTable.Rows[arrange]["Hook"].ToString();
            string[] newHookar = newHook.Split(',');

            string strChangeProgram = dataTable.Rows[arrange]["ChangeProgram"].ToString();
            string[] ChangeProgramar = strChangeProgram.Split(',');

            string newTurn = dataTable.Rows[arrange]["Turn"].ToString();
            string[] newTurnar = newTurn.Split(',');

            string obsAvoidance = dataTable.Rows[arrange]["Direction"].ToString();
            string[] obsAvoidancear = obsAvoidance.Split(',');

            DataTable newDt = new DataTable();
            newDt.Columns.Add(new DataColumn("Tag"));
            newDt.Columns.Add(new DataColumn("X"));
            newDt.Columns.Add(new DataColumn("Y"));
            newDt.Columns.Add(new DataColumn("Speed"));
            newDt.Columns.Add(new DataColumn("Hook"));
            newDt.Columns.Add(new DataColumn("AngleSpeed"));
            newDt.Columns.Add(new DataColumn("Turn"));
            newDt.Columns.Add(new DataColumn("ObsAvoidance"));


            for (int i = 0; i < newTagar.Length; i++)
            {
                newDt.Rows.Add(new object[] { newTagar[i], Convert.ToDouble(itemTag.Select($"TagName = '{newTagar[i]}'")[0]["X"]), Convert.ToDouble(itemTag.Select($"TagName = '{newTagar[i]}'")[0]["Y"]), TagCompile.agvSpeed[Convert.ToInt32(newSpeedar[i])], TagCompile.agvHook[Convert.ToInt32(newHookar[i])], ChangeProgramar[i], TagCompile.agvTurn[Convert.ToInt32(newTurnar[i])], TagCompile.agvDire[Convert.ToInt32(obsAvoidancear[i])] });
            }

            DataTable newDt2 = circuitredact.ProcessData(newDt);
            newDt2 = circuitredact.ProcessDataNow(newDt2);

            List<Tuple<Point, int, double, double, double, double>> newStagingTagPointRatate = new List<Tuple<Point, int, double, double, double,double>>();
            newStagingTagPointRatate = circuitredact.PopulateListFromDataTable(newDt2);

            List<Tuple<Point, int, double, double, double, double>> transformedPointsByRotate = painting.TransformCoordinatesByTurnRotate(newStagingTagPointRatate, MqttClientWrapper.ActualWidthNow, MqttClientWrapper.ActualHeightNow, MqttClientWrapper.ProportionNow);

            return transformedPointsByRotate;

        }


        public List<Tuple<Point, int, double, double, double, double>> CombineLists(List<Tuple<Point, int, double, double>> transformedPointsByRotate,List<Tuple<Point, int, double, double, double, double>> mesEndPoints)
        {
            // 删除 transformedPointsByRotate 的最后一个元素
            if (transformedPointsByRotate.Count > 0)
            {
                transformedPointsByRotate.RemoveAt(transformedPointsByRotate.Count - 1);
            }   

            // 补全 transformedPointsByRotate 中的 double 部分为 1
            var completedTransformedPoints = transformedPointsByRotate
                .ConvertAll(point => Tuple.Create(point.Item1, point.Item2, point.Item3, point.Item4, 1.0, 0.0));

            // 合并列表
            var result = new List<Tuple<Point, int, double, double, double, double>>(completedTransformedPoints);
            result.AddRange(mesEndPoints);

            return result;
        }



    }


}
