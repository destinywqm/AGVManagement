using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace AGV.DAL
{
    public class AgvStateInterfaceSql
    {
        private static string connectionString;

        public static void LoadConfiguration()
        {
            try
            {
                connectionString = ConfigurationManager.ConnectionStrings["MySQLconn"].ConnectionString;
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载配置文件出错: " + ex.Message);
            }
        }

        public static DataTable GetAgvState()
        {
            DataTable dataTable = new DataTable();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT * FROM agv_state";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }
            }
            return dataTable;
        }

        public static int AddAgvState(int id,string agvName, double locationX, double locationY, int mapID, int battery, double angle, double speed, int bearload, int controlMode, int errorNum)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                string sql = "INSERT INTO agv_state (Id, AgvName, LocationX, LocationY, MapID, Battery, Angle, Speed, Bearload, ControlMode, ErrorNum) " +
                             "VALUES (@Id, @agvName, @locationX, @locationY, @mapID, @battery, @angle, @speed, @bearload, @controlMode, @errorNum)";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithValue("@agvName", agvName);
                    command.Parameters.AddWithValue("@locationX", locationX);
                    command.Parameters.AddWithValue("@locationY", locationY);
                    command.Parameters.AddWithValue("@mapID", mapID);
                    command.Parameters.AddWithValue("@battery", battery);
                    command.Parameters.AddWithValue("@angle", angle);
                    command.Parameters.AddWithValue("@speed", speed);
                    command.Parameters.AddWithValue("@bearload", bearload);
                    command.Parameters.AddWithValue("@controlMode", controlMode);
                    command.Parameters.AddWithValue("@errorNum", errorNum);
                    connection.Open();
                    return command.ExecuteNonQuery();
                }
            }
        }

        public static int UpdateAgvState(int id, string agvName, double locationX, double locationY, int mapID, int battery, double angle, double speed, int bearload, int controlMode, int errorNum)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                string sql = "UPDATE agv_state SET AgvName=@AgvName LocationX = @locationX, LocationY = @locationY, MapID = @mapID, Battery = @battery, " +
                             "Angle = @angle, Speed = @speed, Bearload = @bearload, ControlMode = @controlMode, ErrorNum = @errorNum " +
                             "WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@AgvName", agvName);
                    command.Parameters.AddWithValue("@locationX", locationX);
                    command.Parameters.AddWithValue("@locationY", locationY);
                    command.Parameters.AddWithValue("@mapID", mapID);
                    command.Parameters.AddWithValue("@battery", battery);
                    command.Parameters.AddWithValue("@angle", angle);
                    command.Parameters.AddWithValue("@speed", speed);
                    command.Parameters.AddWithValue("@bearload", bearload);
                    command.Parameters.AddWithValue("@controlMode", controlMode);
                    command.Parameters.AddWithValue("@errorNum", errorNum);
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    return command.ExecuteNonQuery();
                }
            }
        }

        public static int DeleteAgvState(int id)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                string sql = "DELETE FROM agv_state WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    return command.ExecuteNonQuery();
                }
            }
        }
    }
}
