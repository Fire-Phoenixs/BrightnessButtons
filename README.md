# BrightnessButtons

一个面向 Windows 外接显示器的极简软件亮度控制器，适用于没有内置屏幕亮度接口的迷你主机、台式机和多显示器环境。

## 功能

- 两个按钮：调高亮度、调低亮度。
- 每次调整 5%。
- 软件亮度范围为 50%–100%。
- 启动时将当前显示器恢复到 100%，便于撤销之前的软件压暗。
- 一次控制当前 Windows 检测到的所有活动显示器。

## 原理与限制

程序通过 Windows GDI 的 `SetDeviceGammaRamp` 修改显卡输出的 RGB 映射表。它降低的是画面输出亮度，不是显示器背光，因此不会改变显示器 OSD 中的硬件亮度、功耗或黑屏状态。

部分显卡驱动、远程桌面、虚拟显示器或特殊显示链路可能拒绝 Gamma Ramp 调整；程序会在界面中报告对应的显示器名称。

## 使用

直接运行 [`dist/BrightnessButtons.exe`](dist/BrightnessButtons.exe)，无需安装。点击按钮即可同时调整所有活动显示器。

## 从源码构建

环境要求：Windows、.NET 8 SDK、Windows Desktop Runtime。

```powershell
dotnet publish BrightnessButtons.csproj -c Release -r win-x64 `
  --self-contained false -p:PublishSingleFile=true -p:DebugType=None -o dist
```

编译产物位于 `dist/BrightnessButtons.exe`。

## 许可证

本项目采用 MIT License，详见 [`LICENSE`](LICENSE)。
