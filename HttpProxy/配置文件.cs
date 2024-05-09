using System;
using System.IO;
using System.Xml.Serialization;

namespace HttpProxy
{
    [XmlRoot("root")]
    public class 配置文件
    {
        public string 服务器地址 { get; set; } = "http://127.0.0.1:21000";

        public int 监听端口 { get; set; } = 8848;

        public string HTTP二级代理 { get; set; } = "127.0.0.1:7879";

        public bool 启用二级代理 { get; set; } = true;

        public bool 原始代理为二级代理 { get; set; } = true;

        public bool 启用代理守卫 { get; set; } = false;

        public int 代理守卫检查间隔 { get; set; } = 3000;

        public readonly static string 配置文件路径 = "ProxyConfig.xml";

        public static void 初始化配置()
        {
            var configFile = new 配置文件();
            if (!保存配置(configFile))
            {
                Environment.Exit(1);
            }
        }

        public static 配置文件 读取配置()
        {
            var serializer = new XmlSerializer(typeof(配置文件));
            try
            {
                using (TextReader textReader = new StreamReader(配置文件路径))
                {
                    var configFile = (配置文件)serializer.Deserialize(textReader);
                    return configFile;
                }
            }
            catch (Exception ex)
            {
                Program.日志.错误($"打不开配置文件：{ex}");
                Program.MessageBox(IntPtr.Zero, $"打不开配置文件：\n{ex.Message}", "错误：", 0x10);
                return null;
            }
        }

        public static bool 保存配置(配置文件 configFile)
        {
            var serializer = new XmlSerializer(typeof(配置文件));
            try
            {
                using (TextWriter textWriter = new StreamWriter(配置文件路径))
                {
                    serializer.Serialize(textWriter, configFile);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Program.日志.错误($"写入配置文件时发生错误：{ex}");
                Program.MessageBox(IntPtr.Zero, $"写入配置文件时发生错误：\n{ex.Message}", "错误：", 0x10);
                return false;
            }
        }
    }
}
