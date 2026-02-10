using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Web.ProtoBuff.Runtime
{
    [Preserve]
    public class GameFrameXWebProtoBuffCroppingHelper : MonoBehaviour
    {
        [Preserve]
        private void Start()
        {
            _ = typeof(WebProtoBuffComponent);
            _ = typeof(IWebProtoBuffManager);
            _ = typeof(WebProtoBuffManager);
        }
    }
}