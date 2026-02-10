using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;
using UnityEngine.Scripting;

namespace GameFrameX.Localization.Runtime
{
    /// <summary>
    /// 本地化语言改变事件。
    /// </summary>
    [Preserve]
    public sealed class LocalizationLanguageChangeEventArgs : GameEventArgs
    {
        /// <summary>
        /// 本地化语言改变事件编号。
        /// </summary>
        [Preserve] public static readonly string EventId = typeof(LocalizationLanguageChangeEventArgs).FullName;

        /// <summary>
        /// 当前语言。
        /// </summary>
        [Preserve]
        public string Language { get; set; }

        /// <summary>
        /// 未知本地化
        /// </summary>
        const string UnknownLocalization = "zxx";

        /// <summary>
        /// 旧的语言。
        /// </summary>
        [Preserve]
        public string OldLanguage { get; set; }

        /// <summary>
        /// 初始化本地化语言改变事件的新实例。
        /// </summary>
        [Preserve]
        public LocalizationLanguageChangeEventArgs()
        {
            OldLanguage = UnknownLocalization;
            Language = UnknownLocalization;
        }

        /// <summary>
        /// 创建本地化语言改变事件。
        /// </summary>
        /// <param name="oldLanguage">旧的语言。</param>
        /// <param name="language">当前语言。</param>
        /// <returns>创建的本地化语言改变事件。</returns>
        [Preserve]
        public static LocalizationLanguageChangeEventArgs Create(string oldLanguage, string language)
        {
            LocalizationLanguageChangeEventArgs localizationLanguageChangeEventArgs = ReferencePool.Acquire<LocalizationLanguageChangeEventArgs>();
            localizationLanguageChangeEventArgs.OldLanguage = oldLanguage;
            localizationLanguageChangeEventArgs.Language = language;
            return localizationLanguageChangeEventArgs;
        }

        /// <summary>
        /// 清除事件参数。
        /// </summary>
        [Preserve]
        public override void Clear()
        {
            OldLanguage = UnknownLocalization;
            Language = UnknownLocalization;
        }

        /// <summary>
        /// 获取事件编号。
        /// </summary>
        /// <returns>事件编号。</returns>
        [Preserve]
        public override string Id
        {
            get { return EventId; }
        }
    }
}