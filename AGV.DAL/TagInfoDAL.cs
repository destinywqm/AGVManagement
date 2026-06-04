using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AGVManagement.Models;
using System.Data;
using MySql.Data.MySqlClient;

namespace AGV.DAL
{
   public class TagInfoDAL
    {
        /// <summary>
        /// 查询所有Tag
        /// </summary>
        /// <param name="exls"></param>
        /// <returns></returns>
        public DataTable GetMapTags(string exls)
        {
            return MySqlHelper.ExecuteDataTable("select * from tag" + exls + " order by (TagName+0)");
        }

        /// <summary>
        /// 查询所有Tag
        /// </summary>
        /// <param name="exls"></param>
        /// <returns></returns>
        public MySqlDataReader GetMapTags(long exls)
        {
            return MySqlHelper.ExecuteReader("select * from tag" + exls + " order by (TagName+0)");
        }

        public string GetQrCode(string tableTime, int tagName)
        {
            object result = MySqlHelper.ExecuteScalar(
                $"SELECT QrCode FROM tag{tableTime} WHERE TagName = {tagName}");
            return result == null || result == DBNull.Value ? "" : result.ToString();
        }

        public bool UpdateQrCode(string tableTime, int tagName, string qrCode)
        {
            int rows = MySqlHelper.ExecuteNonQuery(
                $"UPDATE tag{tableTime} SET QrCode = @qr WHERE TagName = @tag",
                new MySqlParameter("@qr", qrCode),
                new MySqlParameter("@tag", tagName));
            return rows > 0;
        }
    }
}
