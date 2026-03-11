using System.Data;
using System.Windows.Controls;

namespace AGVManagement
{
    internal static class MapListInitializer
    {
        public static bool Populate(ComboBox mapList, DataTable mapData, string defaultCreateTime)
        {
            mapList.Items.Clear();

            if (mapData == null)
            {
                mapList.Items.Add(CreatePromptItem());
                mapList.SelectedIndex = 0;
                return false;
            }

            int preferredIndex = 0;
            int rowIndex = 0;
            mapList.Items.Add(CreatePromptItem());

            foreach (DataRow data in mapData.Rows)
            {
                if (defaultCreateTime != null && defaultCreateTime.Equals(data["CreateTime"].ToString()))
                {
                    preferredIndex = rowIndex;
                }

                var item = new ComboBoxItem
                {
                    Content = data["Name"].ToString(),
                    Tag = data["Width"] + "," + data["Height"] + "," + data["CreateTime"]
                };
                mapList.Items.Add(item);
                rowIndex++;
            }

            mapList.SelectedIndex = preferredIndex + 1;
            return true;
        }

        private static ComboBoxItem CreatePromptItem()
        {
            return new ComboBoxItem { Content = "请选择" };
        }
    }
}
