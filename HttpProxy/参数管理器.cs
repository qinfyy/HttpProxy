using System;
using System.Collections.Generic;

namespace HttpProxy
{
    public class 参数管理器
    {
        private Dictionary<string, string> _参数;
        private Dictionary<string, string> _缩写;

        public 参数管理器(string[] 参数表, Dictionary<string, string> 缩写 = null)
        {
            _参数 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _缩写 = 缩写 ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            解析(参数表);
        }

        private void 解析(string[] 参数表)
        {
            for (int i = 0; i < 参数表.Length; i++)
            {
                if (参数表[i].StartsWith("--"))
                {
                    string 键 = 参数表[i].Substring(2);
                    string 值 = (i + 1 < 参数表.Length && !参数表[i + 1].StartsWith("--")) ? 参数表[i + 1] : null;
                    _参数[键] = 值;
                }
                else if (参数表[i].StartsWith("-") && 参数表[i].Length > 1)
                {
                    string 缩写键 = 参数表[i].Substring(1);
                    if (_缩写.TryGetValue(缩写键, out string 全名键))
                    {
                        string 值 = (i + 1 < 参数表.Length && !参数表[i + 1].StartsWith("-")) ? 参数表[i + 1] : null;
                        _参数[全名键] = 值;
                    }
                }
            }
        }

        public string 取值(string 键)
        {
            _参数.TryGetValue(键, out string value);
            return value;
        }

        public int 取整数(string 键)
        {
            string 值 = 取值(键);
            if (int.TryParse(值, out int 整数值))
            {
                return 整数值;
            }
            else
            {
                throw new InvalidOperationException($"{键} 参数必须为整数");
            }
        }

        public bool 有(string 键)
        {
            return _参数.ContainsKey(键);
        }
    }
}
