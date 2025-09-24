# 自定义通知窗口实现

## 概述

本项目实现了一个自定义通知窗口，用于替代标准的 MessageBox，确保与主界面设计系统的视觉一致性。

## 主要特性

### 设计一致性
- **颜色方案**: 使用与主界面相同的 MeidoThemeColor 主题色彩
- **字体**: 采用 HarmonyOS Sans SC 字体家族
- **圆角设计**: 12px 圆角边框，与主界面保持一致
- **阴影效果**: 8px 深度阴影，15px 模糊半径
- **动画效果**: 按钮悬停和点击状态动画

### 功能特性
- **多种按钮组合**: 支持 OK、OKCancel、YesNo、YesNoCancel 等标准按钮组合
- **图标支持**: 内置信息、警告、错误、问题等图标
- **键盘导航**: 支持 ESC 键取消，Enter 键确认
- **自动调整**: 根据内容自动调整窗口大小
- **模态显示**: 模态对话框，阻止父窗口交互

## 文件结构

```
OpenMeido/
├── CustomNotificationWindow.xaml          # 自定义通知窗口界面
├── CustomNotificationWindow.xaml.cs       # 自定义通知窗口逻辑
├── CustomNotificationTest.cs              # 测试类
├── SettingsWindow.xaml                    # 设置窗口（已更新）
├── SettingsWindow.xaml.cs                 # 设置窗口逻辑（已更新）
├── McpServerEditWindow.xaml               # MCP服务器编辑窗口
└── McpServerEditWindow.xaml.cs            # MCP服务器编辑窗口逻辑（已更新）
```

## 使用方法

### 基本用法

```csharp
// 显示简单信息
CustomNotificationWindow.Show("操作成功完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

// 显示确认对话框
var result = CustomNotificationWindow.Show("确定要删除吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

// 显示未保存更改提示（实际使用场景）
var result = CustomNotificationWindow.Show(
    "设置已更改但未保存，是否要保存更改？", 
    "未保存的更改", 
    MessageBoxButton.YesNoCancel, 
    MessageBoxImage.Question, 
    this); // 指定父窗口
```

### 处理用户选择

```csharp
var result = CustomNotificationWindow.Show("您有未保存的更改，确定要取消吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

switch (result)
{
    case MessageBoxResult.Yes:
        // 用户选择"是"
        break;
    case MessageBoxResult.No:
        // 用户选择"否"
        break;
    case MessageBoxResult.Cancel:
        // 用户选择"取消"
        break;
}
```

## 样式系统

### 样式系统
- 使用与主界面一致的配色方案（#E87475主题色）
- 样式定义在窗口内部资源中，避免AppDomain冲突
- 支持圆角边框和阴影效果
- 按钮具有悬停和点击状态效果

### 布局规范
- **内边距**: 24px（主容器），16px（内容区域）
- **按钮间距**: 12px
- **圆角半径**: 12px（窗口），6px（按钮）
- **阴影参数**: 8px 深度，15px 模糊半径，15% 透明度

## 更新内容

### 已替换的 MessageBox 调用

1. **SettingsWindow.xaml.cs**
   - `SettingsWindow_Closing` 事件中的未保存更改提示
   - 使用 `CustomNotificationWindow.Show` 替代 `MessageBox.Show`

2. **McpServerEditWindow.xaml.cs**
   - `CancelButton_Click` 事件中的未保存更改提示
   - 使用 `CustomNotificationWindow.Show` 替代 `MessageBox.Show`

### 测试功能

在设置窗口中添加了一个"测试通知"按钮，用于演示自定义通知窗口的功能：
- 显示标准的未保存更改提示
- 显示用户选择的结果

## 优势

1. **视觉一致性**: 与主界面设计系统完全匹配
2. **用户体验**: 提供更现代、美观的交互体验
3. **可扩展性**: 易于添加新的按钮类型和样式
4. **可维护性**: 集中管理通知窗口的样式和行为
5. **性能**: 轻量级实现，不影响应用性能

## 注意事项

1. **资源引用**: 确保正确引用 App.xaml 中的样式资源
2. **父窗口**: 建议指定父窗口参数以获得更好的模态体验
3. **异常处理**: 在关键操作中使用 try-catch 块处理可能的异常
4. **测试**: 在不同分辨率和 DPI 设置下测试显示效果

## 未来改进

1. **动画增强**: 添加更多进入/退出动画效果
2. **声音反馈**: 集成音效提示
3. **自定义图标**: 支持更丰富的图标类型
4. **多语言**: 添加多语言支持
5. **无障碍**: 增强屏幕阅读器支持