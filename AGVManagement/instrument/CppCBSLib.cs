using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AGVManagement.instrument
{
    public static class CppCBSLib
    {
        [DllImport("CppCBSLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr get_multi_agent_paths([MarshalAs(UnmanagedType.LPStr)]string input_json);

        [DllImport("CppCBSLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void free_chars(IntPtr out_string_c);

        /// <summary>
        /// 计算多 AGV 路径
        /// </summary>
        /// <param name="jsonInput">环境地图 JSON 字符串</param>
        /// <returns>返回 DLL 输出的 JSON 字符串</returns>
        public static string GetMultiAgentPaths(string jsonInput)
        {
            if (string.IsNullOrWhiteSpace(jsonInput))
                throw new ArgumentException("输入 JSON 不能为空", nameof(jsonInput));

            IntPtr ptr = IntPtr.Zero;
            //try
            //{
                // 调用 DLL
                ptr = get_multi_agent_paths(jsonInput);

                //if (ptr == IntPtr.Zero)
                //    throw new Exception("DLL 返回空指针");

                // Marshal 转字符串
                string result = Marshal.PtrToStringAnsi(ptr);
                return result;
            //}
            //finally
            //{
            //    // 释放 DLL 内存
            //    if (ptr != IntPtr.Zero)
            //        free_chars(ptr);
            //}
        }
    }
}
