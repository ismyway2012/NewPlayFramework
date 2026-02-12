using GameFrameX.Editor;
using GameFrameX.Web.ProtoBuff.Runtime;
using UnityEditor;

namespace GameFrameX.Web.ProtoBuff.Editor
{
    [CustomEditor(typeof(WebProtoBuffComponent))]
    internal sealed class WebProtoBuffComponentInspector : ComponentTypeComponentInspector
    {
        private SerializedProperty m_Timeout = null;

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IWebProtoBuffManager));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            WebProtoBuffComponent t = (WebProtoBuffComponent)target;
            float timeout = EditorGUILayout.Slider("Timeout", m_Timeout.floatValue, 0f, 120f);
            if (timeout != m_Timeout.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.Timeout = timeout;
                }
                else
                {
                    m_Timeout.floatValue = timeout;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void Enable()
        {
            base.Enable();

            m_Timeout = serializedObject.FindProperty("m_Timeout");
            serializedObject.ApplyModifiedProperties();
        }
    }
}