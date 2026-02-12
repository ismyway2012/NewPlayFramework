using Newtonsoft.Json;

namespace GameFrameX.Builder.Editor
{
    internal sealed class BuilderOptions
    {
        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string LogFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 执行方法
        /// </summary>
        public string ExecuteMethod { get; set; } = string.Empty;

        /// <summary>
        /// 构建号
        /// </summary>
        public string BuildNumber { get; set; } = string.Empty;

        /// <summary>
        /// 对象存储的Key
        /// </summary>
        public string ObjectStorageKey { get; set; } = string.Empty;

        /// <summary>
        /// 对象存储的秘钥
        /// </summary>
        public string ObjectStorageSecret { get; set; } = string.Empty;

        /// <summary>
        /// 对象存储桶的名称
        /// </summary>
        public string ObjectStorageBucketName { get; set; } = string.Empty;

        /// <summary>
        /// 对象存储桶的区域名称
        /// </summary>
        public string ObjectStorageEndPoint { get; set; } = string.Empty;

        /// <summary>
        /// 任务名称
        /// </summary>
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// 渠道名称,默认为default
        /// </summary>
        public string ChannelName { get; set; } = "default";

        /// <summary>
        /// 包名,默认值为空，如果为空则使用项目的包名
        /// </summary>
        public string BundleId { get; set; } = string.Empty;

        /// <summary>
        /// 程序版本号,默认值为空，如果为空则使用项目的程序版本号
        /// </summary>
        public string AppVersion { get; set; } = string.Empty;

        /// <summary>
        /// 是否上传日志文件
        /// </summary>
        public bool IsUploadLogFile { get; set; } = false;

        /// <summary>
        /// 资源包名称。只有调用BuildAsset 的时候有效
        /// </summary>
        public string PackageName { get; set; } = "DefaultPackage";

        /// <summary>
        /// 是否使用增量构建的方式构建。只有调用BuildAsset 的时候有效
        /// </summary>
        public bool IsIncrementalBuildPackage { get; set; } = false;

        /// <summary>
        /// 是否上传资源
        /// </summary>
        public bool IsUploadAsset { get; set; } = false;

        /// <summary>
        /// 是否更新资源包版本
        /// </summary>
        public bool IsUpdateAssetPackageVersion { get; set; } = false;

        /// <summary>
        /// 更新资源包版本的url
        /// </summary>
        public string UpdateAssetPackageVersionUrl { get; set; }

        /// <summary>
        /// 更新资源包版本的授权
        /// </summary>
        public string UpdateAssetPackageVersionAuthorization { get; set; }

        /// <summary>
        /// 语言
        /// </summary>
        public string Language { get; set; } = "default";

        /// <summary>
        /// 微信机器人key
        /// </summary>
        public string WeChatBotKey { get; set; }

        /// <summary>
        /// 是否上传APK,只有调用BuildApk 的时候有效
        /// </summary>
        public bool IsUploadApk { get; set; } = false;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}