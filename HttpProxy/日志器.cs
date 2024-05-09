using System;
using System.Diagnostics;

namespace HttpProxy
{
    public class 日志器
    {
        private readonly string _名称;
        private readonly bool _是否跟踪错误;
        private readonly ConsoleColor _颜色;
        private static readonly object _控制台锁 = new object();

        public 日志器(string 名称, ConsoleColor 颜色 = ConsoleColor.Green, bool 是否跟踪错误 = true)
        {
            _名称 = 名称;
            _颜色 = 颜色;
            _是否跟踪错误 = 是否跟踪错误;
        }

        private static bool 是调试模式()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        public void 信息(string 格式, params object[] 参数)
        {
            lock (_控制台锁)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(DateTime.Now.ToString("HH:mm:ss"));
                Console.ResetColor();
                Console.Write(" <");
                Console.ForegroundColor = _颜色;
                Console.Write(_名称);
                Console.ResetColor();
                Console.Write("> ");
                Console.WriteLine(string.Format(格式, 参数));
                Console.ResetColor();
            }
        }

        public void 警告(string 格式, params object[] 参数)
        {
            lock (_控制台锁)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(DateTime.Now.ToString("HH:mm:ss"));
                Console.ResetColor();
                Console.Write(" <");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(_名称);
                Console.ResetColor();
                Console.Write("> ");
                Console.WriteLine(string.Format(格式, 参数));
                Console.ResetColor();
            }
        }

        private void 追踪(string 消息)
        {
            lock (_控制台锁)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("\t└──" + 消息);
                Console.ResetColor();
            }
        }

        public void 错误(string 格式, params object[] 参数)
        {
            lock (_控制台锁)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(DateTime.Now.ToString("HH:mm:ss"));
                Console.ResetColor();
                Console.Write(" <");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(_名称);
                Console.ResetColor();
                Console.Write("> ");
                Console.ForegroundColor = ConsoleColor.White;
                if (_是否跟踪错误)
                {
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                }
                Console.WriteLine(string.Format(格式, 参数));
                Console.ResetColor();
                if (是调试模式())
                {
                    StackTrace 跟踪信息 = new StackTrace(true);
                    if (_是否跟踪错误)
                    {
                        追踪(跟踪信息.ToString());
                    }
                }
            }
        }

        public void 调试(string 格式, params object[] 参数)
        {
            if (是调试模式())
            {
                lock (_控制台锁)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(DateTime.Now.ToString("HH:mm:ss"));
                    Console.ResetColor();
                    Console.Write(" <");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(_名称);
                    Console.ResetColor();
                    Console.Write("> ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine(string.Format(格式, 参数));
                    Console.ResetColor();
                }
            }
        }
    }
}
