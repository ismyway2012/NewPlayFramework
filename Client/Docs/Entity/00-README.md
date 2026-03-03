# GameFrameX 实体系统文档索引

欢迎使用 GameFrameX 实体系统！本文档集将帮助你快速掌握这一强大的游戏对象管理框架。

## ?? 文档结构

```
Docs/Entity/
├── 00-README.md (本文件)
├── 01-EntitySystemArchitecture.md     (★★★ 必读)
├── 02-EntityBestPractices.md          (★★★ 强烈推荐)
├── 03-EntityCodeExamples.md           (★★★ 强烈推荐)
└── 04-EntityFAQ.md                    (★★ 遇到问题时查看)
```

---

## ?? 快速导航

### 我是新员工，应该看什么？

#### 第 1 天：基础了解（30 分钟）
1. 阅读 **01-EntitySystemArchitecture.md** 的"系统概述"和"架构设计"部分
2. 了解核心概念：
   - Entity vs EntityLogic 的区别
   - 生命周期（Init → Show → Hide → Recycle）
   - 对象池机制

#### 第 2 天：快速上手（1 小时）
1. 阅读 **02-EntityBestPractices.md** 的"开发规范"部分
2. 阅读 **03-EntityCodeExamples.md** 的"基础示例"部分（1-3）
3. 尝试在项目中创建一个简单的 EntityLogic

#### 第 3 天：深入学习（2 小时）
1. 阅读 **02-EntityBestPractices.md** 的"常见陷阱"部分
2. 研究 **03-EntityCodeExamples.md** 的"进阶示例"部分
3. 学习实体的父子关系、通信等高级特性

#### 遇到问题时
1. 先查 **04-EntityFAQ.md** 的常见问题部分
2. 使用"故障排除"快速定位问题
3. 查看相关代码示例

---

## ?? 文档详细说明

### 01-EntitySystemArchitecture.md
**内容**: 系统架构、设计原理、优缺点分析

**适合场景**:
- ? 想理解系统如何设计的
- ? 需要向管理层讲解系统架构
- ? 考虑扩展或修改框架
- ? 参加技术评审

**核心章节**:
| 章节 | 学习目标 | 阅读时间 |
|------|---------|---------|
| 系统概述 | 了解框架用途 | 5 分钟 |
| 架构设计 | 理解层级关系 | 10 分钟 |
| 核心组件 | 掌握各类职责 | 20 分钟 |
| 工作流程 | 理解完整流程 | 15 分钟 |
| 优缺点分析 | 明确限制和优势 | 15 分钟 |

---

### 02-EntityBestPractices.md
**内容**: 开发规范、最佳实践、常见陷阱

**适合场景**:
- ? 正在编写 EntityLogic
- ? 想避免常见错误
- ? 需要编写高效代码
- ? 进行代码审查

**核心章节**:
| 章节 | 学习目标 | 阅读时间 |
|------|---------|---------|
| 开发规范 | 掌握编码标准 | 20 分钟 |
| 常见陷阱 | 了解常见错误 | 15 分钟 |
| 性能优化 | 学习优化技巧 | 15 分钟 |
| 测试策略 | 了解单元测试 | 10 分钟 |

---

### 03-EntityCodeExamples.md
**内容**: 从基础到完整的代码示例

**适合场景**:
- ? 学习实际编码技巧
- ? 需要参考代码模板
- ? 想看实战项目示例
- ? 进行代码 copy-paste 参考

**核心章节**:
| 章节 | 示例数量 | 复杂度 |
|------|---------|--------|
| 基础示例 | 3 个 | ? 初级 |
| 进阶示例 | 2 个 | ?? 中级 |
| 常见操作 | 3 个 | ?? 中级 |
| 完整项目 | 1 个 | ??? 高级 |

**推荐学习顺序**:
1. 示例 1-3（快速上手）
2. 示例 4（理解完整生命周期）
3. 示例 5（父子关系）
4. 操作 1-3（实战技巧）
5. 示例 6（项目参考）

---

### 04-EntityFAQ.md
**内容**: 常见问题解答、快速参考、故障排除

**适合场景**:
- ? 遇到具体问题需要快速解决
- ? 需要 API 快速参考
- ? 进行性能优化
- ? 调试和故障排除

**核心章节**:
| 章节 | 问题数量 | 解决时间 |
|------|---------|---------|
| 常见问题 | 9 个 | 2-5 分钟/问题 |
| 快速参考 | API + 速查表 | 1-2 分钟 |
| 故障排除 | 4 个常见故障 | 5 分钟 |
| 性能优化 | 3 个优化方案 | 10 分钟 |

---

## ?? 学习路线图

### 路线 1: 快速上手（第一周）

```
Day 1: 了解概念 (2小时)
  ├─ 01-Architecture 系统概述 (15分钟)
  ├─ 01-Architecture 核心组件 (20分钟)
  └─ 03-Examples 示例 1-2 (25分钟)

Day 2-3: 跟着做 (3小时)
  ├─ 02-BestPractices 开发规范 (20分钟)
  ├─ 03-Examples 示例 3-4 (30分钟)
  └─ 自己创建第一个 EntityLogic (70分钟)

Day 4-5: 深入学习 (3小时)
  ├─ 03-Examples 示例 5-6 (40分钟)
  ├─ 02-BestPractices 常见陷阱 (30分钟)
  ├─ 04-FAQ 常见问题 (30分钟)
  └─ 完善第一个 EntityLogic (60分钟)

Day 6-7: 预留 (2小时)
  ├─ 遇到问题时查询相关文档 (60分钟)
  └─ 复习和练习 (60分钟)
```

### 路线 2: 系统掌握（第二周）

```
基础知识 (第一周) ?
  ↓
进阶特性 (第二周)
  ├─ 父子关系处理
  ├─ 实体间通信
  ├─ 对象池优化
  ├─ 性能调优
  └─ 实战项目实现

Day 8-10: 进阶特性 (4小时)
  ├─ 02-BestPractices 进阶示例 (60分钟)
  ├─ 03-Examples 完整项目示例 (60分钟)
  └─ 02-BestPractices 性能优化 (60分钟)

Day 11-14: 实战项目 (8小时)
  ├─ 设计项目架构 (120分钟)
  ├─ 实现核心实体 (240分钟)
  ├─ 调试和优化 (120分钟)
  └─ 文档和 review (120分钟)
```

---

## ?? 知识体系结构

```
实体系统整体认知
│
├─ 核心概念 (03-Architecture)
│  ├─ Entity（容器）
│  ├─ EntityLogic（业务逻辑）
│  ├─ EntityManager（管理器）
│  └─ EntityGroup（分组）
│
├─ 工作流程 (03-Architecture)
│  ├─ 创建流程 (ShowEntity)
│  ├─ 隐藏流程 (HideEntity)
│  ├─ 回收流程 (OnRecycle)
│  └─ 生命周期钩子
│
├─ 开发实践 (02-BestPractices)
│  ├─ 规范编码
│  ├─ 避免陷阱
│  ├─ 性能优化
│  └─ 单元测试
│
├─ 代码示例 (03-CodeExamples)
│  ├─ 基础用法
│  ├─ 进阶特性
│  ├─ 实战案例
│  └─ 完整项目
│
└─ 问题解决 (04-FAQ)
   ├─ 概念澄清
   ├─ 故障诊断
   ├─ 性能调优
   └─ 快速参考
```

---

## ?? 学习检验清单

### 基础检验（第一周末）

- [ ] 能解释 Entity 和 EntityLogic 的区别
- [ ] 知道生命周期的四个阶段及其用途
- [ ] 能写出简单的 EntityLogic 类
- [ ] 了解对象池的基本工作原理
- [ ] 知道如何显示和隐藏实体

### 进阶检验（第二周末）

- [ ] 能实现父子实体关系
- [ ] 知道三种实体间通信方式及优缺点
- [ ] 能进行简单的性能优化
- [ ] 能编写单元测试
- [ ] 能解决常见的内存泄漏问题

### 精通检验（第三周）

- [ ] 能设计完整的实体系统架构
- [ ] 能快速定位和解决问题
- [ ] 能为团队进行技术分享
- [ ] 能根据场景选择优化策略
- [ ] 能扩展或改进框架

---

## ?? 常用参考速查

### API 快速参考

```csharp
// 显示实体
await entityManager.ShowEntityAsync("GroupName", "Assets/Prefabs/Entity.prefab", userData);

// 隐藏实体
entityManager.HideEntity(entity);

// 查询实体
IEntity entity = entityManager.GetEntity(entityId);
var entities = entityManager.GetEntitiesInGroup("GroupName");

// 配置对象池
var group = entityManager.GetEntityGroup("GroupName");
group.InstanceCapacity = 100;
group.InstanceAutoReleaseInterval = 300f;
```

### 生命周期快速参考

```csharp
OnInit()      → 一次性初始化（获取组件）
OnShow()      → 每次显示时重置状态
OnUpdate()    → 每帧更新逻辑
OnHide()      → 隐藏时停止行为
OnRecycle()   → 回收前清理资源
```

---

## ?? 获取帮助

### 问题排查步骤

1. **检查文档**
   - [ ] 查看 04-EntityFAQ.md 的常见问题
   - [ ] 搜索相关关键词在文档中

2. **查看示例**
   - [ ] 在 03-EntityCodeExamples.md 中找相似的例子
   - [ ] 运行示例代码验证

3. **调试代码**
   - [ ] 添加日志输出
   - [ ] 使用 Profiler 检查内存和性能
   - [ ] 单步调试关键函数

4. **提问方式**
   - [ ] 提供完整的错误日志
   - [ ] 说明期望的行为
   - [ ] 附带相关代码片段
   - [ ] 描述已尝试的解决方案

---

## ?? 学习进度跟踪

```
Week 1: Concepts & Basics (基础概念)
├─ Day 1-2: Theory (理论)      [████??????] 40%
├─ Day 3-4: Practice (实践)     [██████????] 60%
├─ Day 5-7: Consolidation (巩固) [████████??] 80%
└─ Status: In Progress ?

Week 2: Advanced Topics (进阶主题)
├─ Parent-Child Relations (父子关系)
├─ Communication Patterns (通信模式)
├─ Performance Optimization (性能优化)
└─ Status: Ready to Start

Week 3: Mastery (精通)
├─ Architecture Design (架构设计)
├─ Problem Solving (问题解决)
├─ Team Sharing (团队分享)
└─ Status: Scheduled
```

---

## ?? 文档贡献

如果你有改进建议或发现错误，欢迎：
1. 提交 Issue 或 Pull Request
2. 在文档评论区讨论
3. 分享你的实战经验

---

## ?? 结语

实体系统是 GameFrameX 的核心，掌握它将大大提高你的游戏开发效率。

**预计学习时间**: 3 周可以从入门到精通  
**推荐实践方式**: 边学边练，逐步深化  
**成功标志**: 能独立设计和实现复杂的实体系统

祝学习愉快！??

---

**最后更新**: 2024年  
**文档版本**: 1.0  
**适用框架**: GameFrameX  
**C# 版本**: 9.0+
