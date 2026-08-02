# 📱 手机遥控器 Phone Remote

用安卓手机遥控 Windows 电脑：**触摸板模拟鼠标 + 手机键盘实时打字**。纯局域网直连，不依赖任何第三方服务器，无广告、无遥测，全 C# 实现。

## ✨ 功能

- 🖱️ **模拟鼠标**
  - 触控板：滑动 = 移动光标，轻点 = 左键，快速连点 = 双击，双指滑动 = 滚轮
  - 右侧半透明滚动条：上下滑动 = 滚轮（高度自适应触控板）
  - 底部三键（左键 / 中键=滚轮按下 / 右键），2:1:2 比例，半透明白玻璃 + 投影
- ⌨️ **手机键盘打字**
  - 无输入条：App 打开键盘自动弹出
  - 实时镜像：手机上逐字同步到电脑（含拼音过程），删字 = 退格，回车 = 回车（回车不收起键盘）
  - 防"跳字"：按键请求串行排队 + 失败重试 + 大段删除防误判
- 🎨 **触控板背景**：色点插值渐变（macOS 壁纸风），熔岩落日 / 弥散霓虹 / 赛博荧光 / 冰岛极光 / 经典彩虹 / 随机，设置页一键切换
- 🔍 **免配置**：UDP 广播自动发现电脑，无需输入 IP / PIN
- 🔊 **点击音效**：手机 + 电脑同步清脆"咔哒"声
- 🛡️ **防卡键**：按键状态看门狗自动释放；一键恢复输入（Win11 任务栏 Bug）
- 📱 **竖屏锁定、键盘高度自适应**（IME 实测），Windows 调试窗口 400×860

## 📦 目录结构

```
phone-remote/
├── PhoneRemoteApp/            # 安卓 App（.NET MAUI，C#）
│   └── Platforms/Android/     # 原生触摸跟踪 / IME 高度监听
├── PhoneRemoteServer/         # PC 服务端（ASP.NET Core 最小 API）
├── PhoneRemoteServer.Tests/   # 服务端单元测试（29 项）
├── server/                    # PC 服务端发布产物（双击启动）
├── release/                   # 打包产物
│   └── PhoneRemoteApp-Signed.apk
├── tools/                     # 构建 / 诊断脚本
└── 启动.bat                   # 一键启动 PC 服务端
```

## 🚀 快速开始

### PC 端
1. 双击 **`启动.bat`**（或 `dotnet run --project PhoneRemoteServer`）
2. 服务监听 `0.0.0.0:8766`，UDP 自动发现广播端口 `8767`
3. 手机和电脑连**同一个 WiFi**，首次启动防火墙弹窗选「允许访问」

### 安卓端
1. 安装 `release/PhoneRemoteApp-Signed.apk`（允许"安装未知来源"）
2. 打开 App 自动连接，键盘自动弹出即可使用

## 🔨 构建

### PC 服务端
```bash
cd PhoneRemoteServer
dotnet publish -c Release -o ../server
```

### 安卓 APK
```bash
cd PhoneRemoteApp
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormats=apk
# 产物：bin/Release/net10.0-android/publish/com.phoneremote.app-Signed.apk
```

## 🛠 技术栈

- **安卓端**：.NET MAUI (net10.0-android) · 原生手势跟踪（不经过 MAUI 手势框架）· SoundPool
- **PC 端**：ASP.NET Core 最小 API · Kestrel · P/Invoke `SendInput` / `SetCursorPos` / `PlaySound`
- **通信**：HTTP JSON（8766）+ UDP 自动发现广播（8767）

## ⚙️ 设置页

- 连接状态与重新扫描 · 光标灵敏度 · 点击音效开关
- **触控板背景配色**（5 套预设 + 随机，即时生效）
- 🔄 一键恢复输入（电脑任务栏 Bug 导致输入卡死时）

## 常见问题

**Q: 连不上（设置里红色）？**
- 确认服务端窗口开着、同一 WiFi；被防火墙拦时以管理员运行：
  ```
  netsh advfirewall firewall add rule name="PhoneRemoteServer" dir=in action=allow protocol=TCP localport=8766
  ```

**Q: 打字没反应？** 文字输入到电脑**当前聚焦的窗口**——先在电脑上点目标输入框获得焦点，再在手机上打字。

**Q: 电脑没声音？** 电脑端点击声随系统音量，与手机端音效开关相互独立。

## 安全说明

- 仅限局域网直连，数据不出网、无第三方服务器，电脑端服务源码完全开源可自行审查
- 仅供个人学习使用
