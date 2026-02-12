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
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.Mono.Runtime
{
    /// <summary>
    /// Mono 管理器，用于管理 MonoBehaviour 的生命周期事件。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class MonoManager : GameFrameworkModule, IMonoManager
    {
        private static readonly object Lock = new object();

        private LinkedList<Action<float, float>> _updateQueue = new LinkedList<Action<float, float>>();

        private LinkedList<Action<float, float>> _fixedUpdate = new LinkedList<Action<float, float>>();

        private LinkedList<Action<float, float>> _lateUpdate = new LinkedList<Action<float, float>>();

        private LinkedList<Action> _destroy = new LinkedList<Action>();

        private LinkedList<Action<bool>> _onApplicationPause = new LinkedList<Action<bool>>();

        private LinkedList<Action<bool>> _onApplicationFocus = new LinkedList<Action<bool>>();


        /// <summary>
        /// 在固定的帧率下调用。
        /// </summary>
        public void FixedUpdate()
        {
            UnityEngine.Profiling.Profiler.BeginSample("FixedUpdate");
            QueueInvoking(this._fixedUpdate, Time.deltaTime, Time.fixedDeltaTime);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// 在所有 Update 函数调用后每帧调用。
        /// </summary>
        public void LateUpdate()
        {
            QueueInvoking(this._lateUpdate, Time.deltaTime, Time.fixedDeltaTime);
        }

        /// <summary>
        /// 当 MonoBehaviour 将被销毁时调用。
        /// </summary>
        public void OnDestroy()
        {
            QueueInvoking(this._destroy);
        }

        /// <summary>
        /// 当应用程序失去或获得焦点时调用。
        /// </summary>
        /// <param name="focusStatus">应用程序的焦点状态。true 表示获得焦点，false 表示失去焦点。</param>
        public void OnApplicationFocus(bool focusStatus)
        {
            QueueInvoking(this._onApplicationFocus, focusStatus);
        }

        /// <summary>
        /// 当应用程序暂停或恢复时调用。
        /// </summary>
        /// <param name="pauseStatus">应用程序的暂停状态。true 表示暂停，false 表示恢复。</param>
        public void OnApplicationPause(bool pauseStatus)
        {
            QueueInvoking(this._onApplicationPause, pauseStatus);
        }

        /// <summary>
        /// 添加一个在 LateUpdate 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数。第一个参数是流逝时间，第二个参数是固定流逝时间。</param>
        public void AddLateUpdateListener(Action<float, float> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _lateUpdate.AddLast(action);
            }
        }

        /// <summary>
        /// 从 LateUpdate 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数。</param>
        public void RemoveLateUpdateListener(Action<float, float> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _lateUpdate.Remove(action);
            }
        }

        /// <summary>
        /// 添加一个在 FixedUpdate 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数。第一个参数是流逝时间，第二个参数是固定流逝时间。</param>
        public void AddFixedUpdateListener(Action<float, float> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _fixedUpdate.AddLast(action);
            }
        }

        /// <summary>
        /// 从 FixedUpdate 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数。</param>
        public void RemoveFixedUpdateListener(Action<float, float> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _fixedUpdate.Remove(action);
            }
        }

        /// <summary>
        /// 添加一个在 Update 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数。第一个参数是流逝时间，第二个参数是真实流逝时间。</param>
        public void AddUpdateListener(Action<float, float> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _updateQueue.AddLast(action);
            }
        }

        /// <summary>
        /// 从 Update 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数。</param>
        public void RemoveUpdateListener(Action<float, float> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _updateQueue.Remove(action);
            }
        }

        /// <summary>
        /// 添加一个在 Destroy 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数。</param>
        public void AddDestroyListener(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _destroy.AddLast(action);
            }
        }

        /// <summary>
        /// 从 Destroy 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数。</param>
        public void RemoveDestroyListener(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _destroy.Remove(action);
            }
        }

        /// <summary>
        /// 添加一个在 OnApplicationPause 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数。参数为暂停状态。</param>
        public void AddOnApplicationPauseListener(Action<bool> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _onApplicationPause.AddLast(action);
            }
        }

        /// <summary>
        /// 从 OnApplicationPause 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数。</param>
        public void RemoveOnApplicationPauseListener(Action<bool> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _onApplicationPause.Remove(action);
            }
        }

        /// <summary>
        /// 添加一个在 OnApplicationFocus 期间调用的监听器。
        /// </summary>
        /// <param name="action">监听器函数。参数为焦点状态。</param>
        public void AddOnApplicationFocusListener(Action<bool> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _onApplicationFocus.AddLast(action);
            }
        }

        /// <summary>
        /// 从 OnApplicationFocus 中移除一个监听器。
        /// </summary>
        /// <param name="action">监听器函数。</param>
        public void RemoveOnApplicationFocusListener(Action<bool> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (Lock)
            {
                _onApplicationFocus.Remove(action);
            }
        }

        /// <summary>
        /// 释放管理器。
        /// </summary>
        public void Release()
        {
            this._updateQueue.Clear();
            this._destroy.Clear();
            this._fixedUpdate.Clear();
            this._lateUpdate.Clear();
            this._onApplicationFocus.Clear();
            this._onApplicationPause.Clear();
        }


        private static void QueueInvoking(LinkedList<Action> list)
        {
            lock (Lock)
            {
                UnityEngine.Profiling.Profiler.BeginSample("QueueInvoking-Invoke");

                foreach (var action in list)
                {
                    try
                    {
                        UnityEngine.Profiling.Profiler.BeginSample($"QueueInvoking-{action.Method.Name}-{action.Target}");
                        action.Invoke();
                        UnityEngine.Profiling.Profiler.EndSample();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        private static void QueueInvoking(LinkedList<Action<float>> list, float value)
        {
            lock (Lock)
            {
                foreach (var action in list)
                {
                    try
                    {
                        action.Invoke(value);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        private static void QueueInvoking(LinkedList<Action<float, float>> list, float elapseSeconds, float fixedElapseSeconds)
        {
            lock (Lock)
            {
                foreach (var action in list)
                {
                    try
                    {
                        action.Invoke(elapseSeconds, fixedElapseSeconds);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        private static void QueueInvoking(LinkedList<Action<bool>> list, bool value)
        {
            lock (Lock)
            {
                foreach (var action in list)
                {
                    try
                    {
                        action.Invoke(value);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            QueueInvoking(this._updateQueue, elapseSeconds, realElapseSeconds);
        }

        protected override void Shutdown()
        {
            this._updateQueue.Clear();
            this._fixedUpdate.Clear();
            this._lateUpdate.Clear();
            this._onApplicationFocus.Clear();
            this._onApplicationPause.Clear();
            this._destroy.Clear();
        }
    }
}