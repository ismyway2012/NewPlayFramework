using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameFrameX.Runtime;
#if UNITY_WEBGL
using UnityEngine.Networking;
#endif

namespace GameFrameX.Web.ProtoBuff.Runtime
{
    /// <summary>
    /// Web请求管理器,实现HTTP GET和POST请求功能
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public partial class WebProtoBuffManager : GameFrameworkModule, IWebProtoBuffManager
    {
        // 用于构建URL的StringBuilder
        private readonly StringBuilder m_StringBuilder = new StringBuilder(256);

        // 用于存储请求和响应数据的内存流
        private readonly MemoryStream m_MemoryStream;

        // 超时时间(秒)
        private float m_Timeout = 5f;

        /// <summary>
        /// 构造函数
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public WebProtoBuffManager()
        {
            MaxConnectionPerServer = 8;
            m_MemoryStream = new MemoryStream();
            Timeout = 5f;
        }

        /// <summary>
        /// 获取或设置超时时间(秒)
        /// </summary>
        public float Timeout
        {
            get { return m_Timeout; }
            set
            {
                m_Timeout = value;
                RequestTimeout = TimeSpan.FromSeconds(value);
            }
        }

        /// <summary>
        /// 获取或设置每个服务器的最大连接数
        /// </summary>
        public int MaxConnectionPerServer { get; set; }

        /// <summary>
        /// 获取或设置请求超时时间
        /// </summary>
        public TimeSpan RequestTimeout { get; set; }

        /// <summary>
        /// 更新处理请求队列
        /// </summary>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            lock (m_StringBuilder)
            {
                UpdateProtoBuf(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭时清理资源
        /// </summary>
        protected override void Shutdown()
        {
            ShutdownProtoBuf();
            m_MemoryStream.Dispose();
        }

        /// <summary>
        /// URL 标准化
        /// </summary>
        /// <param name="url">原始URL</param>
        /// <param name="queryString">查询参数字典</param>
        /// <returns>标准化后的URL</returns>
        private string UrlHandler(string url, Dictionary<string, string> queryString)
        {
            m_StringBuilder.Clear();
            m_StringBuilder.Append((url));
            if (queryString != null && queryString.Count > 0)
            {
                if (!url.EndsWithFast("?"))
                {
                    m_StringBuilder.Append("?");
                }

                foreach (var kv in queryString)
                {
                    m_StringBuilder.AppendFormat("{0}={1}&", kv.Key, kv.Value);
                }

                url = m_StringBuilder.ToString(0, m_StringBuilder.Length - 1);
                m_StringBuilder.Clear();
            }

            return url;
        }
    }
}