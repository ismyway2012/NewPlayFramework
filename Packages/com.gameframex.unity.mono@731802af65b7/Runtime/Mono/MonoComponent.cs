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
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.Mono.Runtime
{
    /// <summary>
    /// Mono 组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("GameFrameX/Mono")]
    public class MonoComponent : GameFrameworkComponent
    {
        private IMonoManager _monoManager;
        private EventComponent m_EventComponent;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType = typeof(IMonoManager);
            base.Awake();
            _monoManager = GameFrameworkEntry.GetModule<IMonoManager>();
            if (_monoManager == null)
            {
                Log.Fatal("Mono manager is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event manager is invalid.");
                return;
            }
        }

        /// <summary>
        /// 在固定的帧率下调用。
        /// </summary>
        private void FixedUpdate()
        {
            _monoManager.FixedUpdate();
        }

        /// <summary>
        /// 在所有 Update 函数调用后每帧调用。
        /// </summary>
        private void LateUpdate()
        {
            _monoManager.LateUpdate();
        }

        /// <summary>
        /// 当 MonoBehaviour 将被销毁时调用。
        /// </summary>
        private void OnDestroy()
        {
            _monoManager.OnDestroy();
        }

        /// <summary>
        /// 当应用程序失去或获得焦点时调用。
        /// </summary>
        /// <param name="focusStatus">应用程序的焦点状态。true 表示获得焦点，false 表示失去焦点。</param>
        private void OnApplicationFocus(bool focusStatus)
        {
            _monoManager.OnApplicationFocus(focusStatus);
            if (m_EventComponent != null)
            {
                m_EventComponent.Fire(this, OnApplicationFocusChangedEventArgs.Create(focusStatus));
            }
        }

        /// <summary>
        /// 当应用程序暂停或恢复时调用。
        /// </summary>
        /// <param name="pauseStatus">应用程序的暂停状态。true 表示暂停，false 表示恢复。</param>
        private void OnApplicationPause(bool pauseStatus)
        {
            _monoManager.OnApplicationPause(pauseStatus);
            if (m_EventComponent != null)
            {
                m_EventComponent.Fire(this, OnApplicationPauseChangedEventArgs.Create(pauseStatus));
            }
        }

        /// <summary>
        /// 添加 LateUpdate 监听器。
        /// </summary>
        /// <param name="fun">要添加的 LateUpdate 监听器回调函数。第一个参数是流逝时间，第二个参数是固定流逝时间。</param>
        public void AddLateUpdateListener(Action<float, float> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.AddLateUpdateListener(fun);
        }

        /// <summary>
        /// 移除 LateUpdate 监听器。
        /// </summary>
        /// <param name="fun">要移除的 LateUpdate 监听器回调函数。</param>
        public void RemoveLateUpdateListener(Action<float, float> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.RemoveLateUpdateListener(fun);
        }

        /// <summary>
        /// 添加 FixedUpdate 监听器。
        /// </summary>
        /// <param name="fun">要添加的 FixedUpdate 监听器回调函数。第一个参数是流逝时间，第二个参数是固定流逝时间。</param>
        public void AddFixedUpdateListener(Action<float, float> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.AddFixedUpdateListener(fun);
        }

        /// <summary>
        /// 移除 FixedUpdate 监听器。
        /// </summary>
        /// <param name="fun">要移除的 FixedUpdate 监听器回调函数。</param>
        public void RemoveFixedUpdateListener(Action<float, float> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.RemoveFixedUpdateListener(fun);
        }

        /// <summary>
        /// 添加 Update 监听器。
        /// </summary>
        /// <param name="fun">要添加的 Update 监听器回调函数。第一个参数是流逝时间，第二个参数是真实流逝时间。</param>
        public void AddUpdateListener(Action<float, float> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.AddUpdateListener(fun);
        }

        /// <summary>
        /// 移除 Update 监听器。
        /// </summary>
        /// <param name="fun">要移除的 Update 监听器回调函数。</param>
        public void RemoveUpdateListener(Action<float, float> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.RemoveUpdateListener(fun);
        }

        /// <summary>
        /// 添加 Destroy 监听器。
        /// </summary>
        /// <param name="fun">要添加的 Destroy 监听器回调函数。</param>
        public void AddDestroyListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.AddDestroyListener(fun);
        }

        /// <summary>
        /// 移除 Destroy 监听器。
        /// </summary>
        /// <param name="fun">要移除的 Destroy 监听器回调函数。</param>
        public void RemoveDestroyListener(Action fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.RemoveDestroyListener(fun);
        }

        /// <summary>
        /// 添加 OnApplicationPause 监听器。
        /// </summary>
        /// <param name="fun">要添加的 OnApplicationPause 监听器回调函数。参数为暂停状态。</param>
        public void AddOnApplicationPauseListener(Action<bool> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.AddOnApplicationPauseListener(fun);
        }

        /// <summary>
        /// 移除 OnApplicationPause 监听器。
        /// </summary>
        /// <param name="fun">要移除的 OnApplicationPause 监听器回调函数。</param>
        public void RemoveOnApplicationPauseListener(Action<bool> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.RemoveOnApplicationPauseListener(fun);
        }

        /// <summary>
        /// 添加 OnApplicationFocus 监听器。
        /// </summary>
        /// <param name="fun">要添加的 OnApplicationFocus 监听器回调函数。参数为焦点状态。</param>
        public void AddOnApplicationFocusListener(Action<bool> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.AddOnApplicationFocusListener(fun);
        }

        /// <summary>
        /// 移除 OnApplicationFocus 监听器。
        /// </summary>
        /// <param name="fun">要移除的 OnApplicationFocus 监听器回调函数。</param>
        public void RemoveOnApplicationFocusListener(Action<bool> fun)
        {
            if (fun == null)
            {
                Log.Fatal(nameof(fun) + "is invalid.");
                return;
            }

            _monoManager.RemoveOnApplicationFocusListener(fun);
        }
    }
}