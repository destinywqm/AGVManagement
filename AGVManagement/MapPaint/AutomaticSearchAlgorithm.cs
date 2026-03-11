using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace AGVManagement.MapPaint
{
    public class Node
    {
        public double[] axis;
        public double[] next_axis;
        public int LineOrArc;
        public int is_dir_change;
        public double[] arc_centre_axis;
        public double arc_radius;
        public Node next;
        public Node now_stadius;
        public Node last_dataStaticDatacenter;
    }

    public class Program
    {
        [DllImport("your_dll_name.dll")] // 替换 "your_dll_name.dll" 为实际的 DLL 文件名
        public static extern int RRT(string mapFileName, int offset_r, int offset_c, int q_start_r, int q_start_c,
                                      int q_goal_r, int q_goal_c, int bound_expond, double init_h,
                                      ref Node pathListHead, ref Node pathListHead2);

        public static void Alo()
        {
            int q_start_r, q_start_c, q_goal_r, q_goal_c;
            string mapFileName = "D:/vs2022/c/AGV_C_SourceCode_231217/house.pgm";
            int bound_expond = 5;
            q_start_r = 17; q_start_c = 255; q_goal_r = 275; q_goal_c = 50;

            Node pathListHead = new Node();
            Node pathListHead2 = new Node();
            pathListHead.next = null;
            pathListHead2.next = null;

            int offset_r = 100, offset_c = 100;
            double init_h = 10;
            int is_find = 0;

            try
            {
                is_find = RRT(mapFileName, offset_r, offset_c, q_start_r, q_start_c, q_goal_r, q_goal_c,
                               bound_expond, init_h, ref pathListHead, ref pathListHead2);

                if (is_find == 1)
                {
                    Node node = new Node();
                    node.next = null;
                    Node p = pathListHead2.next;
                    while (p != null)
                    {
                        Console.WriteLine($"axis: {p.axis[0]}, {p.axis[1]}\t next axis: {p.next_axis[0]}, {p.next_axis[1]}\t LineOrArc: {p.LineOrArc}");
                        Console.WriteLine($"is_dir_change: {p.is_dir_change}\t arc_centre_axis: {p.arc_centre_axis[0]}, {p.arc_centre_axis[1]}\t arc_radius: {p.arc_radius}\n");
                        p = p.next;
                    }
                }
            }   
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

        }
    }


    public class AutomaticSearchAlgorithm
    {
        const int AGV_NUM = 20;

        [DllImport("Dijkstra.dll")]
        private static extern int Dijkstra(string map_file_name, string pos_file_name, int start_ind, int end_ind, string path_save_file_name);

        [DllImport("E:/AGVCode/AGV_C_1104/AGV_C_1104/RRT.dll")]
        private static extern int RRT(string map_file_name, int q_start_r, int q_start_c, int q_goal_r, int q_goal_c, string path_save_file_name);

        [DllImport("E:/AGVCode/AGV_C_1104/AGV_C_1104/path_schedule.dll")]

        private static extern int path_schedule(string init_path_folder_name, int agvNum, string scheduled_path_folder_name);

        public void Rrt(int auto_StartX, int anto_StartY, int auto_EndX, int auto_EndY)
        {
            string folder1, folder2;
            folder1 = "E:/AGVCode/RTT/init_path/";
            folder2 = "E:/AGVCode/RTT/scheduled_path/";

            int q_start_r = 155, q_start_c = 100, q_goal_r = 954, q_goal_c = 995;
            int[] start_r = { auto_StartX, 806, 286, 156, 156, 546, 286, 676, 546, 175, 286, 175, 435, 955, 695, 825, 156, 565, 936, 156 };
            int[] start_c = { anto_StartY, 30, 940, 680, 550, 290, 550, 680, 810, 200, 160, 460, 330, 460, 330, 200, 290, 980, 30, 940 };
            int[] end_r = { auto_EndX, 286, 936, 955, 305, 955, 676, 416, 695, 806, 565, 565, 305, 955, 305, 936, 305, 806, 416, 416 };
            int[] end_c = { auto_EndY, 290, 420, 850, 590, 70, 160, 940, 70, 160, 200, 70, 70, 200, 330, 290, 590, 550, 550, 810 };

            int[] start_inds = { 1, 10, 100, 70, 56, 34, 58, 78, 90, 15, 16, 43, 33, 55, 37, 25, 28, 105, 12, 98 };
            int[] end_inds = { 140, 30, 54, 97, 59, 13, 22, 102, 9, 24, 21, 7, 3, 27, 31, 40, 59, 66, 60, 88 };
            int start_ind, end_ind;
            int is_find;

            for (int i = 0; i < AGV_NUM; i++)
            {
                Console.WriteLine(i + 1);
                string map_file_name = "E:/AGVCode/RTT/image.pgm";
                string str1 = folder1; // 初始路径保存文件夹地址 
                string str2 = "init_path";
                string str3 = ".txt";
                string path_save_file_name = $"{str1}{str2}{i + 1}{str3}";

                /*// Dijkstra函数执行
                start_ind = start_inds[i];
                end_ind = end_inds[i];
                string pos_file_name = "C:/Users/sungu/Desktop/AGV_C/key_area_pos.txt";
                is_find = Dijkstra(map_file_name, pos_file_name, start_ind, end_ind, path_save_file_name);*/

                // RRT函数执行
                q_start_r = start_r[i] - 1;
                q_start_c = start_c[i] - 1;
                q_goal_r = end_r[i] - 1;
                q_goal_c = end_c[i] - 1;

                is_find = RRT(map_file_name, q_start_r, q_start_c, q_goal_r, q_goal_c, path_save_file_name);
            }

            // 调度
            string init_path_folder_name, scheduled_path_folder_name;
            init_path_folder_name = folder1; // 基础路径保存文件夹 
            scheduled_path_folder_name = folder2; // 调度路径保存文件夹位置 

            // 时间规划函数
            int is_scheduled_succ = path_schedule(init_path_folder_name, AGV_NUM, scheduled_path_folder_name);
        }
    }
}
