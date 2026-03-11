using AGV.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGV.BLL
{
    public class RouteInfoBLL
    {
        RouteInfoDAL RouteInfo = new RouteInfoDAL();
        public DataTable RoutelistArrer(string Times)
        {
            return RouteInfo.RouteData(Times);
        }
    }
}
