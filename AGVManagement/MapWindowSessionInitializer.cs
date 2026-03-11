using AGVManagement.MapPaint;
using System.Windows.Controls;

namespace AGVManagement
{
    internal static class MapWindowSessionInitializer
    {
        public static void ResetMapDrawingState(Panel mainPanel)
        {
            mainPanel.Children.Clear();
            Painting.siseWin = 1;
            MapInstrument.keyValuePairs.Clear();
            MapInstrument.valuePairs.Clear();
            MapInstrument.wirePointArrays.Clear();
            MapInstrument.GetKeyValues.Clear();
        }
    }
}
