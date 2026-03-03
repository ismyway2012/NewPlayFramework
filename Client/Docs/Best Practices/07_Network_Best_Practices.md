# 网络系统（Network）最佳实践指南

## 目录
1. [系统概述](#系统概述)
2. [网络架构](#网络架构)
3. [最佳实践](#最佳实践)
4. [代码示例](#代码示例)
5. [性能优化](#性能优化)
6. [常见问题](#常见问题)

## 系统概述

网络系统（Network System）是GameFrameX框架用于处理游戏网络通信的核心系统。它支持TCP、UDP等多种传输协议，提供了连接管理、消息分发、错误处理等完整的网络解决方案。

### 主要特点
- **多协议支持**: TCP、UDP等多种传输协议
- **消息队列**: 异步消息处理机制
- **心跳检测**: 自动连接保活机制
- **重连机制**: 自动重连和连接恢复
- **加密传输**: 支持消息加密和压缩

## 网络架构

### 网络分层模型
```
┌──────────────────────────────┐
│   Game Logic Layer           │  游戏逻辑层
│   (Handlers, Managers)       │
├──────────────────────────────┤
│   Protocol Layer             │  协议层
│   (Serialization)            │
├──────────────────────────────┤
│   Network Channel Layer      │  网络通道层
│   (Connection, Message Queue)│
├──────────────────────────────┤
│   Transport Layer            │  传输层
│   (TCP, UDP, WebSocket)      │
└──────────────────────────────┘
```

### 消息流程
```
发送端: 业务逻辑 → 序列化 → 加密(可选) → 网络传输
       ↓
接收端: 网络接收 → 解密(可选) → 反序列化 → 消息分发 → 业务处理
```

## 最佳实践

### 1. 网络连接管理

#### 1.1 连接生命周期管理
```csharp
// 推荐：统一的网络管理器
public class NetworkManager : MonoBehaviour
{
    private NetworkClient m_Client;
    private enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }
    private ConnectionState m_ConnectionState = ConnectionState.Disconnected;
    
    public event Action<bool> OnConnectionStateChanged;
    
    public void Connect(string serverAddress, int port)
    {
        if (m_ConnectionState != ConnectionState.Disconnected)
        {
            Log.Warning("Already connected or connecting");
            return;
        }
        
        m_ConnectionState = ConnectionState.Connecting;
        m_Client = new NetworkClient();
        m_Client.OnConnected += OnConnected;
        m_Client.OnDisconnected += OnDisconnected;
        m_Client.OnError += OnNetworkError;
        
        m_Client.Connect(serverAddress, port);
    }
    
    private void OnConnected()
    {
        m_ConnectionState = ConnectionState.Connected;
        OnConnectionStateChanged?.Invoke(true);
        Log.Info("Connected to server");
    }
    
    private void OnDisconnected()
    {
        m_ConnectionState = ConnectionState.Disconnected;
        OnConnectionStateChanged?.Invoke(false);
        Log.Info("Disconnected from server");
    }
    
    private void OnNetworkError(string error)
    {
        Log.Error($"Network error: {error}");
        Disconnect();
    }
    
    public void Disconnect()
    {
        if (m_ConnectionState == ConnectionState.Disconnected)
            return;
        
        m_ConnectionState = ConnectionState.Disconnecting;
        
        if (m_Client != null)
        {
            m_Client.OnConnected -= OnConnected;
            m_Client.OnDisconnected -= OnDisconnected;
            m_Client.OnError -= OnNetworkError;
            m_Client.Disconnect();
            m_Client = null;
        }
    }
    
    public bool IsConnected => m_ConnectionState == ConnectionState.Connected;
}
```

#### 1.2 自动重连机制
```csharp
public class AutoReconnectManager
{
    private int m_ReconnectAttempts = 0;
    private float m_ReconnectDelay = 5f;
    private const int MAX_RECONNECT_ATTEMPTS = 5;
    private bool m_IsReconnecting = false;
    
    public void OnConnectionLost()
    {
        if (m_IsReconnecting || m_ReconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
        {
            Log.Error("Max reconnection attempts reached");
            return;
        }
        
        m_IsReconnecting = true;
        StartCoroutine(ReconnectCoroutine());
    }
    
    private IEnumerator ReconnectCoroutine()
    {
        yield return new WaitForSeconds(m_ReconnectDelay);
        
        m_ReconnectAttempts++;
        Log.Info($"Attempting to reconnect... ({m_ReconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})");
        
        var networkManager = GameEntry.GetComponent<NetworkManager>();
        networkManager.Connect("server.example.com", 8888);
        
        m_IsReconnecting = false;
        
        // 指数退避策略
        m_ReconnectDelay = Mathf.Min(m_ReconnectDelay * 2, 60f);
    }
    
    public void OnConnectionRestored()
    {
        m_ReconnectAttempts = 0;
        m_ReconnectDelay = 5f;
        Log.Info("Connection restored");
    }
}
```

### 2. 消息定义和序列化

#### 2.1 结构化消息定义
```csharp
// 推荐：使用Protocol Buffers或类似工具定义消息
[Serializable]
public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string DeviceId { get; set; }
}

[Serializable]
public class LoginResponse
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public PlayerData PlayerData { get; set; }
    public string ErrorMessage { get; set; }
}

// 消息类型枚举
public enum MessageType : ushort
{
    LOGIN_REQUEST = 1001,
    LOGIN_RESPONSE = 1002,
    PLAYER_MOVE = 2001,
    PLAYER_ATTACK = 2002,
    PLAYER_SYNC = 3001,
}
```

#### 1.2 消息工厂
```csharp
public class MessageFactory
{
    private Dictionary<MessageType, Type> m_MessageTypeMap = 
        new Dictionary<MessageType, Type>();
    
    public void Register<T>(MessageType messageType) where T : INetworkMessage
    {
        m_MessageTypeMap[messageType] = typeof(T);
    }
    
    public INetworkMessage Create(MessageType messageType)
    {
        if (m_MessageTypeMap.TryGetValue(messageType, out var type))
        {
            return Activator.CreateInstance(type) as INetworkMessage;
        }
        
        Log.Error($"Unknown message type: {messageType}");
        return null;
    }
    
    public void SerializeMessage<T>(T message, out byte[] data) where T : INetworkMessage
    {
        data = JsonUtility.ToJson(message).GetBytes();
    }
    
    public T DeserializeMessage<T>(byte[] data) where T : INetworkMessage
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        return JsonUtility.FromJson<T>(json);
    }
}
```

### 3. 消息处理

#### 3.1 消息处理器模式
```csharp
// 推荐：为每个消息类型创建独立的处理器
public interface IMessageHandler
{
    void Handle(INetworkMessage message);
}

public class LoginResponseHandler : IMessageHandler
{
    public void Handle(INetworkMessage message)
    {
        if (message is LoginResponse response)
        {
            if (response.Success)
            {
                OnLoginSuccess(response);
            }
            else
            {
                OnLoginFailed(response.ErrorMessage);
            }
        }
    }
    
    private void OnLoginSuccess(LoginResponse response)
    {
        // 保存用户数据
        GameEntry.GetData<UserData>().SetPlayerData(response.PlayerData);
        GameEntry.GetData<UserData>().SetToken(response.Token);
        
        // 发布登录成功事件
        var eventComponent = GameEntry.GetComponent<EventComponent>();
        eventComponent.Fire(this, new LoginSuccessEventArgs { PlayerData = response.PlayerData });
    }
    
    private void OnLoginFailed(string errorMessage)
    {
        Log.Error($"Login failed: {errorMessage}");
        var eventComponent = GameEntry.GetComponent<EventComponent>();
        eventComponent.Fire(this, new LoginFailedEventArgs { ErrorMessage = errorMessage });
    }
}

public class MessageHandlerRegistry
{
    private Dictionary<Type, IMessageHandler> m_Handlers = new Dictionary<Type, IMessageHandler>();
    
    public void Register<T>(IMessageHandler handler) where T : INetworkMessage
    {
        m_Handlers[typeof(T)] = handler;
    }
    
    public void HandleMessage(INetworkMessage message)
    {
        var messageType = message.GetType();
        if (m_Handlers.TryGetValue(messageType, out var handler))
        {
            handler.Handle(message);
        }
        else
        {
            Log.Warning($"No handler for message type: {messageType.Name}");
        }
    }
}
```

#### 3.2 消息队列处理
```csharp
public class MessageQueue
{
    private Queue<INetworkMessage> m_MessageQueue = new Queue<INetworkMessage>();
    private MessageHandlerRegistry m_HandlerRegistry;
    private bool m_IsProcessing = false;
    
    public void Enqueue(INetworkMessage message)
    {
        lock (m_MessageQueue)
        {
            m_MessageQueue.Enqueue(message);
        }
    }
    
    public void ProcessMessages()
    {
        if (m_IsProcessing) return;
        
        m_IsProcessing = true;
        
        while (m_MessageQueue.Count > 0)
        {
            INetworkMessage message;
            lock (m_MessageQueue)
            {
                if (m_MessageQueue.Count == 0) break;
                message = m_MessageQueue.Dequeue();
            }
            
            try
            {
                m_HandlerRegistry.HandleMessage(message);
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling message: {ex.Message}");
            }
        }
        
        m_IsProcessing = false;
    }
}
```

### 4. 网络安全

#### 4.1 消息加密
```csharp
public class EncryptedNetworkClient
{
    private byte[] m_EncryptionKey;
    private byte[] m_EncryptionIV;
    
    public void SendEncryptedMessage<T>(T message) where T : INetworkMessage
    {
        // 序列化消息
        var json = JsonUtility.ToJson(message);
        var data = System.Text.Encoding.UTF8.GetBytes(json);
        
        // 加密
        var encryptedData = EncryptData(data);
        
        // 发送
        SendRawData(encryptedData);
    }
    
    public T ReceiveDecryptedMessage<T>(byte[] data) where T : INetworkMessage
    {
        // 解密
        var decryptedData = DecryptData(data);
        
        // 反序列化
        var json = System.Text.Encoding.UTF8.GetString(decryptedData);
        return JsonUtility.FromJson<T>(json);
    }
    
    private byte[] EncryptData(byte[] data)
    {
        using (var aes = new System.Security.Cryptography.AesCryptoServiceProvider())
        {
            aes.Key = m_EncryptionKey;
            aes.IV = m_EncryptionIV;
            
            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new System.IO.MemoryStream())
            {
                using (var cs = new System.Security.Cryptography.CryptoStream(
                    ms, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }
    }
    
    private byte[] DecryptData(byte[] encryptedData)
    {
        using (var aes = new System.Security.Cryptography.AesCryptoServiceProvider())
        {
            aes.Key = m_EncryptionKey;
            aes.IV = m_EncryptionIV;
            
            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new System.IO.MemoryStream(encryptedData))
            using (var cs = new System.Security.Cryptography.CryptoStream(
                ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
            {
                using (var resultMs = new System.IO.MemoryStream())
                {
                    cs.CopyTo(resultMs);
                    return resultMs.ToArray();
                }
            }
        }
    }
}
```

#### 4.2 Token验证
```csharp
public class TokenManager
{
    private string m_Token;
    private long m_TokenExpirationTime;
    
    public void SetToken(string token, long expirationTime)
    {
        m_Token = token;
        m_TokenExpirationTime = expirationTime;
    }
    
    public string GetToken()
    {
        if (IsTokenExpired())
        {
            Log.Warning("Token expired");
            return null;
        }
        return m_Token;
    }
    
    public bool IsTokenExpired()
    {
        return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() > m_TokenExpirationTime;
    }
    
    public void ClearToken()
    {
        m_Token = null;
        m_TokenExpirationTime = 0;
    }
}
```

## 代码示例

### 示例1：完整的网络交互流程
```csharp
public class GameNetworkManager : MonoBehaviour
{
    private NetworkClient m_NetworkClient;
    private MessageQueue m_MessageQueue;
    private TokenManager m_TokenManager;
    
    private void Start()
    {
        InitializeNetwork();
    }
    
    private void InitializeNetwork()
    {
        m_NetworkClient = new NetworkClient();
        m_MessageQueue = new MessageQueue();
        m_TokenManager = new TokenManager();
        
        // 设置回调
        m_NetworkClient.OnMessageReceived += OnMessageReceived;
        m_NetworkClient.OnConnectionLost += OnConnectionLost;
    }
    
    public void Login(string username, string password)
    {
        if (m_NetworkClient == null || !m_NetworkClient.IsConnected)
        {
            Log.Error("Not connected to server");
            return;
        }
        
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password,
            DeviceId = SystemInfo.deviceUniqueIdentifier
        };
        
        m_NetworkClient.Send(loginRequest);
    }
    
    private void OnMessageReceived(byte[] data)
    {
        try
        {
            var messageType = ParseMessageType(data);
            var message = DeserializeMessage(messageType, data);
            m_MessageQueue.Enqueue(message);
        }
        catch (Exception ex)
        {
            Log.Error($"Error processing received message: {ex.Message}");
        }
    }
    
    private void OnConnectionLost()
    {
        Log.Warning("Connection to server lost");
        var autoReconnect = GetComponent<AutoReconnectManager>();
        autoReconnect?.OnConnectionLost();
    }
    
    private void Update()
    {
        m_MessageQueue?.ProcessMessages();
    }
}
```

### 示例2：心跳检测
```csharp
public class HeartbeatManager
{
    private float m_HeartbeatInterval = 30f;
    private float m_LastHeartbeatTime = 0f;
    private const float HEARTBEAT_TIMEOUT = 60f;
    private NetworkClient m_NetworkClient;
    
    public void Update()
    {
        float currentTime = Time.time;
        
        if (currentTime - m_LastHeartbeatTime >= m_HeartbeatInterval)
        {
            SendHeartbeat();
            m_LastHeartbeatTime = currentTime;
        }
    }
    
    private void SendHeartbeat()
    {
        var heartbeat = new HeartbeatMessage();
        m_NetworkClient.Send(heartbeat);
    }
    
    public void OnHeartbeatResponse()
    {
        // 收到心跳响应，连接正常
        Log.Debug("Heartbeat response received");
    }
}

[Serializable]
public class HeartbeatMessage : INetworkMessage
{
    public long Timestamp { get; set; } = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
```

### 示例3：网络数据同步
```csharp
public class PlayerNetworkSynchronizer
{
    private PlayerEntity m_LocalPlayer;
    private float m_SyncInterval = 0.1f;
    private float m_LastSyncTime = 0f;
    private NetworkClient m_NetworkClient;
    
    public void Update()
    {
        if (Time.time - m_LastSyncTime >= m_SyncInterval)
        {
            SyncPlayerPosition();
            m_LastSyncTime = Time.time;
        }
    }
    
    private void SyncPlayerPosition()
    {
        var syncMessage = new PlayerSyncMessage
        {
            PlayerId = m_LocalPlayer.Id,
            Position = m_LocalPlayer.transform.position,
            Rotation = m_LocalPlayer.transform.rotation,
            Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        m_NetworkClient.Send(syncMessage);
    }
    
    public void OnRemotePlayerSync(PlayerSyncMessage message)
    {
        var remotePlayer = FindRemotePlayer(message.PlayerId);
        if (remotePlayer != null)
        {
            remotePlayer.SetNetworkPosition(message.Position, message.Rotation);
        }
    }
}

[Serializable]
public class PlayerSyncMessage : INetworkMessage
{
    public int PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public long Timestamp { get; set; }
}
```

## 性能优化

### 1. 减少网络流量
```csharp
// 推荐：使用压缩和增量同步
public class CompressedMessage
{
    public byte[] CompressData(byte[] data)
    {
        using (var ms = new System.IO.MemoryStream())
        {
            using (var gzip = new System.IO.Compression.GZipStream(
                ms, System.IO.Compression.CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }
            return ms.ToArray();
        }
    }
}
```

### 2. 消息批处理
```csharp
public class MessageBatcher
{
    private List<INetworkMessage> m_PendingMessages = new List<INetworkMessage>();
    private float m_BatchInterval = 0.05f;
    private float m_LastBatchTime = 0f;
    
    public void AddMessage(INetworkMessage message)
    {
        m_PendingMessages.Add(message);
        
        if (Time.time - m_LastBatchTime >= m_BatchInterval)
        {
            SendBatch();
        }
    }
    
    private void SendBatch()
    {
        if (m_PendingMessages.Count == 0) return;
        
        var batch = new MessageBatch { Messages = m_PendingMessages.ToArray() };
        SendToServer(batch);
        
        m_PendingMessages.Clear();
        m_LastBatchTime = Time.time;
    }
}
```

## 常见问题

### Q1: 如何处理网络延迟？

**A:** 使用客户端预测和服务器验证的混合方法。

### Q2: 如何保证消息的可靠性？

**A:** 使用TCP协议或在UDP上实现重传机制。

### Q3: 如何处理网络中断？

**A:** 实现自动重连机制和本地数据缓存。

### Q4: 如何优化网络性能？

**A:** 使用消息压缩、批处理、增量同步等技术。

---

**最后更新时间**: 2025年
**适用版本**: GameFrameX 1.3.6+
**作者**: GameFrameX 开发团队
