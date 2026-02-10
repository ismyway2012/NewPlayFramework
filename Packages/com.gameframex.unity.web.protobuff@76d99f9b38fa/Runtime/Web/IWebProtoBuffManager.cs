using System.Threading.Tasks;

namespace GameFrameX.Web.ProtoBuff.Runtime
{
    /// <summary>
    /// Web请求管理器接口，提供HTTP POST请求的补充功能，支持Protobuf消息序列化
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public interface IWebProtoBuffManager
    {
#if ENABLE_GAME_FRAME_X_WEB_PROTOBUF_NETWORK
        /// <summary>
        /// 发送Protobuf消息的Post请求，并接收指定类型的响应
        /// </summary>
        /// <param name="url">目标服务器的URL地址</param>
        /// <param name="message">要发送的Protobuf消息对象，必须继承自MessageObject</param>
        /// <typeparam name="T">返回的数据类型，必须继承自MessageObject并且实现IResponseMessage接口</typeparam>
        /// <returns>返回指定类型T的异步任务，该任务完成时将包含从服务器接收到的响应数据</returns>
        /// <remarks>
        /// 此方法用于向指定的URL发送POST请求，并接收响应。请求的消息体由参数message提供，而响应则会被解析为指定的泛型类型T。
        /// 仅在启用ENABLE_GAME_FRAME_X_WEB_PROTOBUF_NETWORK宏定义时可用。
        /// </remarks>
        Task<T> Post<T>(string url, GameFrameX.Network.Runtime.MessageObject message) where T : GameFrameX.Network.Runtime.MessageObject, GameFrameX.Network.Runtime.IResponseMessage;
#endif
        /// <summary>
        /// 获取或设置POST请求的超时时间（单位：秒）
        /// </summary>
        float Timeout { get; set; }
    }
}