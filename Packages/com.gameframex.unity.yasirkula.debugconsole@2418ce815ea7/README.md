# In-game Debug Console

This asset helps you see debug messages (logs, warnings, errors, exceptions) runtime in a build (also assertions in editor) and execute commands using its built-in console. It also supports logging logcat messages to the console on Android platform.

User interface is created with uGUI and costs 1 SetPass call (and 6 to 10 batches) when Sprite Packing is enabled. It is possible to resize or hide the console window during the game. Once the console is hidden, a small popup will take its place (which can be dragged around). The popup will show the
number of logs that arrived since it had appeared. Console window will reappear after clicking the popup.

该库主要服务于 `https://github.com/AlianBlank/GameFrameX` 作为子库使用。

# 使用方式(三种方式)

1. 直接在 `manifest.json` 文件中添加以下内容
   ```json
      {"com.gameframex.unity.yasirkula.debugconsole": "https://github.com/AlianBlank/com.gameframex.unity.yasirkula.debugconsole.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.yasirkula.debugconsole.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别

# 改动功能

1. 增加 `Packages` 的支持

# Fork 来源

https://github.com/yasirkula/UnityIngameDebugConsole

# 以下为原内容

= In-game Debug Console (v1.6.7) =

Documentation: https://github.com/yasirkula/UnityIngameDebugConsole
FAQ: https://github.com/yasirkula/UnityIngameDebugConsole#faq
E-mail: yasirkula@gmail.com

You can simply place the IngameDebugConsole prefab to your scene. Hovering the cursor over its properties in the Inspector will reveal explanatory tooltips.