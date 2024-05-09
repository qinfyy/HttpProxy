using System;
using System.Threading;
using Microsoft.Win32;

namespace HttpProxy
{
    internal sealed class 代理守卫
    {
        private static readonly 日志器 日志 = new 日志器("代理守卫");
        private static Thread 守卫线程;
        private static bool 正在运行;
        private static bool 启用守卫;
        private static int 检查间隔毫秒;

        // 保存原始的代理设置
        private static string 预期代理服务器;
        private static string 预期代理例外;

        private const string 注册表路径 = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        private const string 键_启用代理 = "ProxyEnable";
        private const string 键_代理服务器 = "ProxyServer";
        private const string 键_代理例外 = "ProxyOverride";

        public static void 启动(int 间隔毫秒)
        {
            if (正在运行) 
                return;

            检查间隔毫秒 = 间隔毫秒;

            预期代理服务器 = 代理服务器.获取系统代理();
            if (预期代理服务器 == null) 
                return;

            预期代理例外 = 代理服务器.获取代理例外列表();
            启用守卫 = true;
            正在运行 = true;

            守卫线程 = new Thread(守卫线程方法);
            守卫线程.Start();

            日志.信息("系统代理守卫已启动，检查间隔：{0} 毫秒", 间隔毫秒);
        }

        public static void 停止()
        {
            if (!正在运行) return;

            正在运行 = false;
            启用守卫 = false;

            if (守卫线程 != null && 守卫线程.IsAlive)
            {
                守卫线程.Join(1000);
                if (守卫线程.IsAlive)
                {
                    日志.警告("系统代理守卫线程未能在预期时间内结束");
                }
            }

            日志.信息("系统代理守卫已停止");
        }

        private static void 守卫线程方法()
        {
            日志.调试("系统代理守卫线程已启动");

            while (正在运行)
            {
                try
                {
                    if (启用守卫)
                    {
                        检查并恢复代理();
                    }
                }
                catch (Exception 异常)
                {
                    日志.错误("系统代理守卫检查时发生错误: {0}", 异常.Message);
                }

                Thread.Sleep(检查间隔毫秒);
            }

            日志.调试("系统代理守卫线程已结束");
        }

        private static void 检查并恢复代理()
        {
            bool 需要恢复 = false;

            using (RegistryKey 注册表 = Registry.CurrentUser.OpenSubKey(注册表路径, false))
            {
                if (注册表 == null) return;

                var 是否启用代理 = 注册表.GetValue(键_启用代理);
                if (是否启用代理 == null || Convert.ToInt32(是否启用代理) != 1)
                {
                    日志.警告("检测到系统代理被禁用，正在恢复...");
                    需要恢复 = true;
                }

                var 当前代理服务器 = 注册表.GetValue(键_代理服务器)?.ToString();
                if (当前代理服务器 == null || 当前代理服务器 != 预期代理服务器)
                {
                    日志.警告("检测到系统代理服务器被修改，正在恢复...");
                    日志.调试("当前: {0}, 预期: {1}", 当前代理服务器, 预期代理服务器);
                    需要恢复 = true;
                }

                var 当前代理例外 = 注册表.GetValue(键_代理例外)?.ToString();
                if (当前代理例外 == null || 当前代理例外 != 预期代理例外)
                {
                    日志.警告("检测到系统代理例外被修改，正在恢复...");
                    需要恢复 = true;
                }
            }

            if (需要恢复)
            {
                恢复代理设置();
            }
        }

        private static void 恢复代理设置()
        {
            try
            {
                using (RegistryKey 注册表 = Registry.CurrentUser.OpenSubKey(注册表路径, true))
                {
                    if (注册表 == null) return;

                    注册表.SetValue(键_代理服务器, 预期代理服务器);
                    注册表.SetValue(键_代理例外, 预期代理例外);
                    注册表.SetValue(键_启用代理, 1);
                }

                代理服务器.刷新系统设置();

                日志.信息("系统代理设置已恢复");
            }
            catch (Exception 异常)
            {
                日志.错误("恢复系统代理设置时发生错误: {0}", 异常.Message);
            }
        }
    }
}
