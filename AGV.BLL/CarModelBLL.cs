using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AGV.DAL;

namespace AGV.BLL
{
    public class CarModelBLL
    {
        // 确保表存在
        public static void EnsureTable()
        {
            string sql = @"CREATE TABLE IF NOT EXISTS CarModel (
                                Id INT AUTO_INCREMENT PRIMARY KEY,
                                Name VARCHAR(100),
                                Type VARCHAR(50),
                                Length DOUBLE,
                                Width DOUBLE,
                                Load DOUBLE,
                                CommMode VARCHAR(50)
                            );";
            MySqlHelper.ExecuteNonQuery(sql);
        }

        // 新增车辆
        public static bool InsertCar(string name, string type, double length, double width, double load, string commMode)
        {
            EnsureTable();
            string sql = $@"INSERT INTO CarModel (Name, Type, Length, Width, Load, CommMode) 
                            VALUES ('{name}','{type}',{length},{width},{load},'{commMode}')";
            return MySqlHelper.ExecuteNonQuery(sql) > 0;
        }

        // 更新车辆
        public static bool UpdateCar(int id, string name, string type, double length, double width, double load, string commMode)
        {
            EnsureTable();
            string sql = $@"UPDATE CarModel SET 
                                Name='{name}', 
                                Type='{type}', 
                                Length={length}, 
                                Width={width}, 
                                Load={load}, 
                                CommMode='{commMode}'
                                WHERE Id={id}";
            return MySqlHelper.ExecuteNonQuery(sql) > 0;
        }

        // 获取单个车辆
        public static DataTable GetCarById(int id)
        {
            string sql = $"SELECT * FROM CarModel WHERE Id={id}";
            return MySqlHelper.ExecuteDataTable(sql);
        }

        // 获取所有车辆
        public static DataTable GetAllCars()
        {
            string sql = "SELECT * FROM CarModel";
            return MySqlHelper.ExecuteDataTable(sql);
        }
    }
}
