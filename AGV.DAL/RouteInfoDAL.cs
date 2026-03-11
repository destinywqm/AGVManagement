using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.DAL
{
    public class RouteInfoDAL
    {
        public DataTable RouteData(string Times)
        {
            return MySqlHelper.ExecuteDataTable("select * from route" + Times + "");
        }
    }
}
