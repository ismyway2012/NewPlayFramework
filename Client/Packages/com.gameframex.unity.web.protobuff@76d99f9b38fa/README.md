## HOMEPAGE

GameFrameX 的 Web ProtoBuff 请求组件

**Web ProtoBuff 组件 (Web ProtoBuff Component)** - 提供基于 Protocol Buffer 的 HTTP 网络请求功能，支持异步发送和接收 ProtoBuff 消息。

# 使用文档

`WebProtoBuffComponent` 类是一个游戏框架组件，专门用于处理基于 Protocol Buffer 的网络请求。它提供了一系列方法来发送 POST 请求，并自动处理 ProtoBuf 消息的序列化与反序列化。

## 功能概述

- **ProtoBuf 支持**: 原生支持 Protocol Buffer 消息格式。
- **异步操作**: 基于 `Task<T>` 的异步 API，方便使用 `async/await`。
- **超时控制**: 支持自定义请求超时时间。
- **跨平台**: 兼容 Unity WebGL 及其他原生平台。

## 环境要求

使用本组件需要在 Unity 的 **Player Settings** -> **Scripting Define Symbols** 中添加以下宏定义：

`ENABLE_GAME_FRAME_X_WEB_PROTOBUF_NETWORK`

## 方法说明

### Awake

初始化游戏框架组件。获取 `IWebProtoBuffManager` 模块并配置超时时间。

```csharp
protected override void Awake() { /* ... */ }
```

### Post<T>

发送 ProtoBuf 消息并等待响应。

```csharp
public Task<T> Post<T>(string url, GameFrameX.Network.Runtime.MessageObject message) 
    where T : GameFrameX.Network.Runtime.MessageObject, GameFrameX.Network.Runtime.IResponseMessage
```

- **参数**:
    - `url`: 目标服务器的 URL 地址。
    - `message`: 要发送的消息对象（必须继承自 `MessageObject`）。
- **返回值**:
    - 返回一个 `Task<T>`，任务完成后包含服务器响应的消息对象。
- **类型参数**:
    - `T`: 响应消息的类型（必须继承自 `MessageObject` 并实现 `IResponseMessage`）。

### Timeout

获取或设置请求超时时间（单位：秒）。

```csharp
public float Timeout { get; set; }
```

## 使用示例

### 1. 定义消息

首先定义请求和响应的 ProtoBuf 消息类。

```csharp
using ProtoBuf;
using GameFrameX.Network.Runtime;

[ProtoContract]
public class LoginRequest : MessageObject
{
    [ProtoMember(1)]
    public string Username { get; set; }
    
    [ProtoMember(2)]
    public string Password { get; set; }
}

[ProtoContract]
public class LoginResponse : MessageObject, IResponseMessage
{
    [ProtoMember(1)]
    public bool Success { get; set; }
    
    [ProtoMember(2)]
    public string Token { get; set; }
}
```

### 2. 发送请求

在组件中使用 `WebProtoBuffComponent` 发送请求。

```csharp
using UnityEngine;
using GameFrameX.Web.ProtoBuff.Runtime;

public class LoginController : MonoBehaviour
{
    public WebProtoBuffComponent WebComponent;

    private async void Start()
    {
        var request = new LoginRequest 
        { 
            Username = "user", 
            Password = "password" 
        };
        
        string url = "http://api.example.com/login";

        try
        {
            // 发送请求并等待结果
            LoginResponse response = await WebComponent.Post<LoginResponse>(url, request);
            
            if (response != null)
            {
                Debug.Log($"Login Result: {response.Success}, Token: {response.Token}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Request Failed: {e.Message}");
        }
    }
}
```

## 安装方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
      {"com.gameframex.unity.web.protobuff": "https://github.com/gameframex/com.gameframex.unity.web.protobuff.git"}
    ```
2. 在 Unity 的 `Packages Manager` 中使用 `Git URL` 的方式添加库,地址为：`https://github.com/gameframex/com.gameframex.unity.web.protobuff.git`

3. 直接下载仓库放置到 Unity 项目的 `Packages` 目录下。会自动加载识别
