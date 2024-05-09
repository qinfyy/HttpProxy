using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Eavesdrop;
using Microsoft.Win32;

namespace HttpProxy
{
    internal sealed class 代理服务器
    {
        private static readonly 日志器 日志 = new 日志器("代理服务器");

        static 代理服务器()
        {
            Eavesdropper.Certifier = new Certifier("Cyt", "HttpProxy Root Certificate Authority")
            {
                NotBefore = DateTime.Now,
                NotAfter = DateTime.Now.AddMonths(1)
            };
        }

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        private const string 注册表路径 = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        private const string 键_启用代理 = "ProxyEnable";
        private const string 键_代理服务器 = "ProxyServer";
        private const string 键_代理例外 = "ProxyOverride";

        private const string 默认例外列表 =
            "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*";

        private static readonly string[] 目标域名 =
        {
            "mihoyo.com",
            "hoyoverse.com",
            "starrails.com",
            "bhsr.com",
            "yuanshen.com",
            "bh3.com",
            "honkaiimpact3.com"
        };

        private const string 代理例外模板 =
            "localhost;1*;" + //" 127.*;10.*;192.168.*;" +
            "*0;*1;*2;*3;*4;*5;*6;*7;*8;*9;" +
            "*q;*w;*e;*r;*t;*y;*u;*i;*o;*p;*a;*s;*d;*f;*g;*h;*j;*k;*l;*z;*x;*c;*v;*b;*n;" +
            "*1.com;*2.com;*4.com;*5.com;*6.com;*7.com;*8.com;*9.com;" +
            "*q.com;*w.com;*t.com;*y.com;*u.com;*i.com;*p.com;*a.com;*d.com;*f.com;*h.com;*j.com;*k.com;*l.com;*z.com;*x.com;*c.com;*v.com;*b.com;*m.com;" +
            //米哈游域名处理
            //"*e.com;*s.com;*r.com;*n.com;*o.com;*3.com;" +
            "*1e.com;*2e.com;*3e.com;*4e.com;*5e.com;*6e.com;*7e.com;*8e.com;*9e.com;*0e.com;*qe.com;*we.com;*ee.com;*re.com;*te.com;*ye.com;*ue.com;*ie.com;*oe.com;*pe.com;*ae.com;*de.com;*fe.com;*ge.com;*he.com;*je.com;*ke.com;*le.com;*ze.com;*xe.com;*ce.com;*ve.com;*be.com;*ne.com;*me.com;" +
            "*1o.com;*2o.com;*3o.com;*4o.com;*5o.com;*6o.com;*7o.com;*8o.com;*9o.com;*0o.com;*qo.com;*wo.com;*eo.com;*ro.com;*to.com;*uo.com;*io.com;*oo.com;*po.com;*ao.com;*so.com;*do.com;*fo.com;*go.com;*ho.com;*jo.com;*ko.com;*lo.com;*zo.com;*xo.com;*co.com;*vo.com;*bo.com;*no.com;*mo.com;" +
            "*1s.com;*2s.com;*3s.com;*4s.com;*5s.com;*6s.com;*7s.com;*8s.com;*9s.com;*0s.com;*qs.com;*ws.com;*es.com;*rs.com;*ts.com;*ys.com;*us.com;*is.com;*os.com;*ps.com;*as.com;*ss.com;*ds.com;*fs.com;*gs.com;*hs.com;*js.com;*ks.com;*zs.com;*xs.com;*cs.com;*vs.com;*bs.com;*ns.com;*ms.com;" +
            "*1r.com;*2r.com;*3r.com;*4r.com;*5r.com;*6r.com;*7r.com;*8r.com;*9r.com;*0r.com;*qr.com;*wr.com;*er.com;*rr.com;*tr.com;*yr.com;*ur.com;*iq.com;*oq.com;*pq.com;*aq.com;*dq.com;*fq.com;*gq.com;*hq.com;*jq.com;*kq.com;*lq.com;*zq.com;*xq.com;*cq.com;*vq.com;*bq.com;*nq.com;*mq.com;" +
            "*1n.com;*2n.com;*3n.com;*4n.com;*5n.com;*6n.com;*7n.com;*8n.com;*9n.com;*0n.com;*qn.com;*wn.com;*rn.com;*tn.com;*yn.com;*un.com;*in.com;*on.com;*pn.com;*an.com;*sn.com;*dn.com;*fn.com;*gn.com;*hn.com;*jn.com;*kn.com;*ln.com;*zn.com;*xn.com;*cn.com;*vn.com;*bn.com;*nn.com;*mn.com;" +
            "*13.com;*23.com;*33.com;*43.com;*53.com;*63.com;*73.com;*83.com;*93.com;*03.com;*q3.com;*w3.com;*e3.com;*r3.com;*y3.com;*u3.com;*i3.com;*o3.com;*p3.com;*a3.com;*s3.com;*d3.com;*f3.com;*g3.com;*j3.com;*k3.com;*l3.com;*z3.com;*x3.com;*c3.com;*v3.com;*b3.com;*n3.com;*m3.com;";
            //常用域名处理
            //"*bing*;*google*;*live.com;*office.com;*weibo*;*yahoo*;*taobao*;*go.com;*csdn.com;*msn.com;*aliyun.com;*cdn.com;" +
            //"*ttvnw*;*edge*;*microsoft*;*bing*;*google*;*discordapp*;*gstatic.com;*imgur.com;*hub.*;*gitlab.com;*googleapis.com;*facebook.com;*cloudfront.net;*gvt1.com;*jquery.com;*akamai.net;*ultra-rv.com;*youtube*;*ytimg*;*ggpht*;" +
            //"*baidu*;*qq*;*sohu*;*weibo*;*163*;*360*;*iqiyi*;*youku*;*bilibili*;*sogou*;*taobao*;*jd*;*zhihu*;*steam*;*ea.com;*csdn*;*.msn.*;*aliyun*;*cdn*;" +
            //"*twitter.com;*instagram.com;*wikipedia.org;*yahoo*;*xvideos.com;*whatsapp.com;*live.com;*netflix.com;*office.com;*tiktok.com;*reddit.com;*discord*;*twitch*;*duckduckgo.com";


        private static string 代理服务器设置;

        private static bool 已设置系统代理;

        private static string 重定向地址;

        public static string 获取代理例外列表() => 默认例外列表;

        public static void 刷新系统设置()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        private static void 设置系统代理(int 代理端口)
        {
            代理服务器设置 = $"http=127.0.0.1:{代理端口};https=127.0.0.1:{代理端口}";

            using (RegistryKey 注册表 = Registry.CurrentUser.OpenSubKey(注册表路径, true))
            {
                注册表.SetValue(键_代理服务器, 代理服务器设置);
                注册表.SetValue(键_代理例外, 默认例外列表);
                注册表.SetValue(键_启用代理, 1);
            }
            已设置系统代理 = true;
            刷新系统设置();
        }

        public static string 获取系统代理()
        {
            if (已设置系统代理)
                return 代理服务器设置;
            else
                return null;
        }

        private static void 关闭系统代理()
        {
            if (!已设置系统代理) return;

            已设置系统代理 = false;
            using (RegistryKey 注册表 = Registry.CurrentUser.OpenSubKey(注册表路径, true))
            {
                if (Program.配置.HTTP二级代理 != null)
                {
                    注册表.SetValue(键_代理服务器, Program.配置.HTTP二级代理);
                    注册表.SetValue(键_启用代理, 1);
                }
                else
                {
                    注册表.SetValue(键_启用代理, 0);
                }
            }
            刷新系统设置();
        }

        private static void 启动代理服务器(int 端口)
        {
            if (Eavesdropper.IsRunning) return;

            Eavesdropper.Overrides.Clear();
            Eavesdropper.Overrides.AddRange(代理例外模板.Split(';'));

            Eavesdropper.RequestInterceptedAsync += 请求拦截处理;
            Eavesdropper.Initiate(端口);
        }

        private static Task 请求拦截处理(object sender, RequestInterceptedEventArgs e)
        {
            var 请求 = e.Request as HttpWebRequest;
            string 原始地址 = 请求.RequestUri.OriginalString;

            foreach (string 域名 in 目标域名)
            {
                int 位置 = 原始地址.IndexOf(域名, StringComparison.OrdinalIgnoreCase);
                if (位置 == -1) continue;

                int 路径位置 = 原始地址.IndexOf('/', 位置 + 域名.Length);
                string 新地址 = 路径位置 >= 0
                    ? 重定向地址 + 原始地址.Substring(路径位置)
                    : 重定向地址;

                e.Request = 重定向请求(请求, new Uri(新地址));
                日志.信息("重定向：{0} 到 {1}", 原始地址, e.Request.RequestUri);
                return Task.CompletedTask;
            }

            e.Request = 重定向请求(请求, 请求.RequestUri);
            日志.信息("直连：{0}", e.Request.RequestUri);
            return Task.CompletedTask;
        }

        private static HttpWebRequest 重定向请求(HttpWebRequest 原始请求, Uri 新地址)
        {
            var 新请求 = WebRequest.CreateHttp(新地址);
            新请求.ProtocolVersion = 原始请求.ProtocolVersion;
            新请求.CookieContainer = 原始请求.CookieContainer;
            新请求.AllowAutoRedirect = 原始请求.AllowAutoRedirect;
            新请求.KeepAlive = 原始请求.KeepAlive;
            新请求.Method = 原始请求.Method;

            新请求.Proxy = Program.配置.HTTP二级代理 != null
                ? new WebProxy($"http://{Program.配置.HTTP二级代理}", true)
                : 原始请求.Proxy;

            foreach (string 键 in 原始请求.Headers.AllKeys)
            {
                switch (键.ToLower())
                {
                    case "user-agent": 新请求.UserAgent = 原始请求.UserAgent; break;
                    case "accept": 新请求.Accept = 原始请求.Accept; break;
                    case "content-length": 新请求.ContentLength = 原始请求.ContentLength; break;
                    case "if-modified-since": 新请求.IfModifiedSince = 原始请求.IfModifiedSince; break;
                    case "date": 新请求.Date = 原始请求.Date; break;
                    case "host": 新请求.Host = 原始请求.Host; break;
                    case "content-type": 新请求.ContentType = 原始请求.ContentType; break;
                    case "referer": 新请求.Referer = 原始请求.Referer; break;
                    default: 新请求.Headers[键] = 原始请求.Headers[键]; break;
                }
            }

            return 新请求;
        }

        private static void 停止代理服务器()
        {
            Eavesdropper.Terminate();
        }

        public static void 启动代理(string 重定向目标)
        {
            日志.信息("监听端口：{0}", Program.配置.监听端口);
            重定向地址 = 重定向目标.TrimEnd('/');
            日志.信息("重定向到：{0}", 重定向地址);

            启动代理服务器(Program.配置.监听端口);
            设置系统代理(Program.配置.监听端口);

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;
        }

        public static void 停止代理()
        {
            日志.信息("停止代理");
            停止代理服务器();
            关闭系统代理();
        }

        public static bool 检查并创建证书() => Eavesdropper.Certifier.CreateTrustedRootCertificate();

        public static bool 删除证书() => Eavesdropper.Certifier.DestroyTrustedRootCertificate();

        public static bool 是否运行中() => Eavesdropper.IsRunning;
    }
}
