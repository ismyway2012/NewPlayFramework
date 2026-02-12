// ==========================================================================================
//  GameFrameX 组织及其衍生项目的版权、商标、专利及其他相关权利
//  GameFrameX organization and its derivative projects' copyrights, trademarks, patents, and related rights
//  均受中华人民共和国及相关国际法律法规保护。
//  are protected by the laws of the People's Republic of China and relevant international regulations.
// 
//  使用本项目须严格遵守相应法律法规及开源许可证之规定。
//  Usage of this project must strictly comply with applicable laws, regulations, and open-source licenses.
// 
//  本项目采用 MIT 许可证与 Apache License 2.0 双许可证分发，
//  This project is dual-licensed under the MIT License and Apache License 2.0,
//  完整许可证文本请参见源代码根目录下的 LICENSE 文件。
//  please refer to the LICENSE file in the root directory of the source code for the full license text.
// 
//  禁止利用本项目实施任何危害国家安全、破坏社会秩序、
//  It is prohibited to use this project to engage in any activities that endanger national security, disrupt social order,
//  侵犯他人合法权益等法律法规所禁止的行为！
//  or infringe upon the legitimate rights and interests of others, as prohibited by laws and regulations!
//  因基于本项目二次开发所产生的一切法律纠纷与责任，
//  Any legal disputes and liabilities arising from secondary development based on this project
//  本项目组织与贡献者概不承担。
//  shall be borne solely by the developer; the project organization and contributors assume no responsibility.
// 
//  GitHub 仓库：https://github.com/GameFrameX
//  GitHub Repository: https://github.com/GameFrameX
//  Gitee  仓库：https://gitee.com/GameFrameX
//  Gitee Repository:  https://gitee.com/GameFrameX
//  官方文档：https://gameframex.doc.alianblank.com/
//  Official Documentation: https://gameframex.doc.alianblank.com/
// ==========================================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.Web.ProtoBuff.Runtime
{
    /// <summary>
    /// Web 请求组件。
    /// 提供HTTP GET和POST请求功能的Unity组件。
    /// 支持字符串和字节数组格式的请求结果。
    /// 可以设置请求超时时间。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("GameFrameX/Web ProtoBuff")]
    [UnityEngine.Scripting.Preserve]
    [RequireComponent(typeof(GameFrameXWebProtoBuffCroppingHelper))]
    public sealed class WebProtoBuffComponent : GameFrameworkComponent
    {
        /// <summary>
        /// Web请求管理器实例
        /// </summary>
        private IWebProtoBuffManager m_WebProtoBuffManager;

        /// <summary>
        /// 请求超时时间配置
        /// </summary>
        [SerializeField] [Tooltip("超时时间.单位：秒")]
        private float m_Timeout = 5f;

        /// <summary>
        /// 获取或设置下载超时时长，以秒为单位。
        /// 当请求超过此时间未完成时会自动终止。
        /// </summary>
        public float Timeout
        {
            get { return m_WebProtoBuffManager.Timeout; }
            set { m_WebProtoBuffManager.Timeout = m_Timeout = value; }
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// 在此方法中初始化Web管理器并设置超时时间。
        /// </summary>
        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType = typeof(IWebProtoBuffManager);
            base.Awake();
            m_WebProtoBuffManager = GameFrameworkEntry.GetModule<IWebProtoBuffManager>();
            if (m_WebProtoBuffManager == null)
            {
                Log.Fatal("Web manager is invalid.");
                return;
            }

            m_WebProtoBuffManager.Timeout = m_Timeout;
        }

#if ENABLE_GAME_FRAME_X_WEB_PROTOBUF_NETWORK
        /// <summary>
        /// 发送Post请求，用于发送和接收Protocol Buffer消息。
        /// 此方法仅在启用ENABLE_GAME_FRAME_X_WEB_PROTOBUF_NETWORK宏定义时可用。
        /// </summary>
        /// <param name="url">目标服务器的URL地址</param>
        /// <param name="message">要发送的Protocol Buffer消息对象，必须继承自MessageObject</param>
        /// <typeparam name="T">返回的数据类型，必须继承自MessageObject并且实现IResponseMessage接口</typeparam>
        /// <returns>返回一个任务对象，该任务完成时将包含从服务器接收到的Protocol Buffer响应数据，数据类型为T</returns>
        /// <remarks>
        /// 此方法专门用于处理Protocol Buffer格式的请求和响应。
        /// 发送的消息和接收的响应都必须是Protocol Buffer消息类型。
        /// </remarks>
        public Task<T> Post<T>(string url, GameFrameX.Network.Runtime.MessageObject message) where T : GameFrameX.Network.Runtime.MessageObject, GameFrameX.Network.Runtime.IResponseMessage
        {
            return m_WebProtoBuffManager.Post<T>(url, message);
        }
#endif
    }
}