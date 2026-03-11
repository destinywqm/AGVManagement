using AGV.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVManagement.MapPaint
{

    public class DeleteMap
    {
        MapMessageBLL map = new MapMessageBLL();
        List<string> Sql = new List<string>();


        public bool deletetlas(List<string> sql)
        {
            return map.DeleteAGVMapBLL(Sql);
        }

    }
}
