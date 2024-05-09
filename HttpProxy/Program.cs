using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace HttpProxy
{
    class Program
    {
        public static 配置文件 配置;
        public static readonly 日志器 日志 = new 日志器("Program");

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("Kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        private delegate bool HandlerRoutine(CtrlTypes ctrlType);

        private static readonly HandlerRoutine _控制台事件处理委托 = new HandlerRoutine(控制台事件处理);

        private static bool Ctrl加C = false;

        private enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        static bool 控制台事件处理(CtrlTypes ctrlType)
        {
            if (ctrlType == CtrlTypes.CTRL_CLOSE_EVENT)
            {
                if (!Ctrl加C)
                    关闭程序(中断: true);
                return true;
            }
            return false;
        }

        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(_控制台事件处理委托, true);
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                关闭程序();
            };

            Console.Title = "Http Proxy  （请按下 Ctrl + C 退出程序，不要按右上角的 ╳ 关闭按钮！！！  否则会无法关闭代理，导致无法上网！！！）";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("请按下 Ctrl + C 退出程序，不要按右上角的 ╳  关闭按钮！！！  否则会无法关闭代理，导致无法上网！！！");
            Console.WriteLine("请按下 Ctrl + C 退出程序，不要按右上角的 ╳  关闭按钮！！！  否则会无法关闭代理，导致无法上网！！！");
            Console.WriteLine("请按下 Ctrl + C 退出程序，不要按右上角的 ╳  关闭按钮！！！  否则会无法关闭代理，导致无法上网！！！\n");
            Console.ResetColor();

            加载配置();
            加载参数(args);
            二级代理初始设置();
            代理开关();
        }

        private static void 关闭程序(int 退出码 = 0, bool 中断 = false)
        {
            日志.信息("退出程序");
            代理开关(true);
            if (!中断)
            {
                Ctrl加C = true;
                卸载证书();
                Environment.Exit(退出码);
            } 
        }

        private static void 加载参数(string[] args)
        {
            var 缩写 = new Dictionary<string, string>
            {
                { "h", "help" },
                { "d", "destroy-cert" },
                { "p", "port" },
                { "s", "server-url" }
            };

            var 参数 = new 参数管理器(args, 缩写);

            if (参数.有("help"))
            {
                Console.WriteLine("Http Proxy Tools\n" +
                                  "usage: stream [-h] [-p <PORT>] [-s <SERVER_URL>] [--destroy-cert]\n\n" +
                                  "options:\n" +
                                  "  -h, --help                    显示此帮助消息并退出\n" +
                                  "  -d, --destroy-cert            卸载根证书.\n" +
                                  "  -s, --server-url <SERVER_URL> 设置服务器地址，默认为 `http://127.0.0.1:21000`（覆盖配置文件）。\n" +
                                  "  -p, --port <PORT>             设置端口，默认为 `8848` （覆盖配置文件）。\n");
                关闭程序();
            }

            if (参数.有("destroy-cert"))
            {
                卸载证书();
                关闭程序(中断: true);
            }

            if (参数.有("port"))
            {
                配置.监听端口 = 参数.取整数("port");
            }

            if (参数.有("server-url"))
            {
                配置.服务器地址 = 参数.取值("server-url");
            }
        }

        private static void 加载配置()
        {
            if (!File.Exists(配置文件.配置文件路径))
                配置文件.初始化配置();
            配置 = 配置文件.读取配置();
            if (配置 == null)
                关闭程序(1);
        }

        private static void 二级代理初始设置()
        {
            if (配置.启用二级代理)
            {
                if (配置.原始代理为二级代理)
                {
                    读取代理();
                }
                if (配置.HTTP二级代理 == $"127.0.0.1:{配置.监听端口}")
                {
                    配置.HTTP二级代理 = null;
                }
                else
                {
                    var 检测线程 = new Thread(检查二级代理);
                    检测线程.Start();
                }
            }
            else
            {
                配置.HTTP二级代理 = null;
            }
        }

        private static void 读取代理()
        {
            string 注册表路径 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings";
            string ip端口正则表达式 = @"^(\[([a-fA-F0-9:]+)\]|((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])):(6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[0-9]{1,4})$";
            var 正则 = new Regex(ip端口正则表达式);
            var 注册表键 = Registry.CurrentUser.OpenSubKey(注册表路径);

            try
            {
                if (注册表键 == null)
                {
                    日志.警告("未找到注册表项。");
                    return;
                }

                var 是否启用代理 = 注册表键.GetValue("ProxyEnable");
                var 当前代理服务器 = 注册表键.GetValue("ProxyServer");

                if (是否启用代理 != null && 当前代理服务器 != null)
                {
                    bool 代理状态 = Convert.ToInt32(是否启用代理) == 1;
                    string 代理服务器 = 当前代理服务器.ToString();

                    if (代理状态)
                    {
                        日志.调试($"原始代理：{代理服务器}");
                        var 匹配 = 正则.Match(代理服务器);

                        if (匹配.Success)
                        {
                            配置.HTTP二级代理 = 代理服务器;
                        }
                        else
                        {
                            日志.调试($"原始代理：{代理服务器}");
                            string FD代理格式正则表达式 = @"(?:http|https)=(\[(?:[a-fA-F0-9:]+)\]|(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])(?:\.(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])){3}):((6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[0-9]{1,4}))(?:;|$)";
                            var FD代理格式正则 = new Regex(FD代理格式正则表达式);
                            var FD代理格式匹配 = FD代理格式正则.Match(代理服务器);

                            if (FD代理格式匹配.Success)
                            {
                                string ip = FD代理格式匹配.Groups[1].Value;
                                string 端口 = FD代理格式匹配.Groups[2].Value;

                                日志.调试($"IP地址: {ip}");
                                日志.调试($"端口: {端口}");
                                配置.HTTP二级代理 = $"{ip}:{端口}";
                            }
                            else
                            {
                                日志.警告("无法识别代理设置字符串");
                                配置.HTTP二级代理 = null;
                            }
                        }
                    }
                    else
                    {
                        配置.HTTP二级代理 = null;
                    }
                }
                else
                {
                    日志.警告("未找到代理设置。");
                    return;
                }
            }
            catch (Exception ex)
            {
                日志.错误($"发生错误: {ex}");
            }
            finally
            {
                注册表键?.Close();
            }
        }

        private static void 检查二级代理()
        {
            while (true)
            {
                if (配置.HTTP二级代理 != null)
                {
                    var 解析 = 配置.HTTP二级代理.Split(':');
                    string 主机 = 解析[0];
                    string 端口 = 解析[1];

                    try
                    {
                        using (var 检测客户端 = new TcpClient())
                        {
                            检测客户端.Connect(主机, int.Parse(端口));

                            if (检测客户端.Connected)
                            {
                                日志.调试($"TCP Ping: {主机}:{端口} 连接成功。");
                            }
                            else
                            {
                                日志.调试($"TCP Ping: {主机}:{端口} 连接失败。");
                                配置.HTTP二级代理 = null;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        日志.调试($"发生异常：{ex.Message}");
                        配置.HTTP二级代理 = null;
                        break;
                    }
                }
                Thread.Sleep(3000);
            }
        }

        private static void 代理开关(bool 关闭 = false)
        {
            try
            {
                if (代理服务器.是否运行中() || 关闭)
                {
                    if (配置.启用代理守卫)
                    {
                        代理守卫.停止();
                    }
                    代理服务器.停止代理();
                }
                else
                {
                    if (配置.监听端口 > 65535 || 配置.监听端口 < 1)
                    {
                        日志.错误("监听端口不能大于 65535 或小于 1 。");
                        MessageBox(IntPtr.Zero, "监听端口能大于 65535 或小于 1 。", "错误：", 0x10);
                        关闭程序(1);
                    }

                    if (!代理服务器.检查并创建证书())
                    {
                        MessageBox(IntPtr.Zero, "未能安装根证书。", "错误：", 0x10);
                        日志.错误("未能安装根证书");
                        关闭程序(1);
                    }

                    代理服务器.启动代理(配置.服务器地址);
                    
                    if (配置.启用代理守卫)
                    {
                        代理守卫.启动(配置.代理守卫检查间隔);
                    }

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                日志.错误($"启动代理失败：{ex}");
                MessageBox(IntPtr.Zero, $"启动代理失败：\n{ex.Message}", "错误：", 0x10);
                关闭程序(1);
            }
        }

        private static void 卸载证书()
        {
            日志.信息("正在卸载根证书");
            bool 已删除证书 = 代理服务器.删除证书();
            if (已删除证书)
            {
                日志.信息("已卸载根证书");
            }
            else
            {
                日志.警告("未能卸载根证书");
            }
        }
    }
}
