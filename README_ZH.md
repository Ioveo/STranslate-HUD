<div align="center">

<p>
  <a href="https://github.com/Ioveo/STranslate-HUD" target="_blank">
    <img align="center" alt="STranslate-HUD" width="160" src="./images/favicon.svg" />
  </a>
</p>

# STranslate-HUD

<p align="center">
  <strong>专为“全英文软件汉化荒”打造的无感桌面翻译神器</strong><br>
  实时整窗跟随穿透 HUD 贴面汉化 · 智能鼠标悬停雷达 · 有道级背景取色图片翻译 · 纯离线 WinRT 多媒体闭环
</p>

<p align="center">
  <a href="https://github.com/Ioveo/STranslate-HUD/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/Ioveo/STranslate-HUD?style=flat-square&color=blue" alt="License" />
  </a>
  <a href="https://github.com/Ioveo/STranslate-HUD/stargazers">
    <img src="https://img.shields.io/github/stars/Ioveo/STranslate-HUD?style=flat-square&logo=github" alt="Stars" />
  </a>
  <a href="https://github.com/Ioveo/STranslate-HUD/network/members">
    <img src="https://img.shields.io/github/forks/Ioveo/STranslate-HUD?style=flat-square&logo=github" alt="Forks" />
  </a>
  <img src="https://img.shields.io/badge/.NET-10.0-purple?style=flat-square&logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?style=flat-square&logo=windows" alt="Windows 10/11" />
</p>

[**English**](./README.md) | **简体中文**

</div>

---

> 💡 **项目愿景**：当你打开一个专业海外软件或游戏，满屏英文却**找不到汉化包**时，不再需要频繁截图或拍照。**STranslate-HUD** 像一副 AR 智能透视眼镜，原地将目标软件的界面变成中文，且**鼠标依然可以穿透点击原软件正常操作**！

---

## 🚀 核心升级特性

### 1. 🥽 整窗实时跟随穿透 HUD 贴面汉化 (`Alt + Shift + H`)
- **0 侵入、无须汉化包**：不需要替换任何软件文件，不报毒、不破坏原程序完整性。
- **元素级原地覆盖**：通过 Windows UI Automation 树遍历（自绘界面自动回退至全窗离线 OCR），原位计算绝对坐标，原地贴上半透明中文徽标。
- **真正鼠标穿透（Click-Through）**：采用 `WS_EX_TRANSPARENT` 穿透图层，您可以**直接点击中文下方的原软件按钮与菜单**进行交互。
- **硬件级动态跟随**：后台毫秒级监测宿主窗口物理位置，窗口拖动、缩放、移动时贴面毫秒级实时跟随。按 `Esc` 键随时退出。

### 2. 🎯 智能鼠标悬停雷达 (`Alt + H`)
- **免划词、免复制、免点击**：按 `Alt + H` 开启后，将鼠标光标移动到任意英文单词、按钮或菜单上**静止停留 0.3 秒**，光标旁自动弹出半透明胶囊气泡显示译文。
- **全场景双引擎探测**：支持 UIA 控件文本提取，遇到自绘界面、Canvas 网页或游戏文本时，自动触发光标局部微区域离线 OCR，指哪翻哪。
- **视线零干扰**：鼠标移开超过设定阈值，气泡自动淡出消失。

### 3. 🖼️ 有道级原位擦除图片翻译 (`Alt + Shift + X`)
- **智能边缘背景取色器 (`ImageColorSampler`)**：自动采样每个文本框边缘的真实像素颜色（支持中值滤波与众数分析），与原图底色浑然一体，告别生硬死板的纯黑纯白补丁。
- **高保真自适应排版**：根据原英文框宽高比动态计算中文字号、行高与间距，完美原位填充。
- **Auto-Fallback 免配置机制**：彻底消除了原版必须手动右键指定“图片翻译引擎/OCR引擎”的繁琐限制，开箱即用。

### 4. ⚡ Windows 原生离线 OCR & TTS 双引擎
- **原生 WindowsMedia OCR 插件**：基于 WinRT 原生 `Windows.Media.Ocr`，离线纯本地运行，0 毫秒网络延迟，支持高精度矩形坐标包围盒输出。
- **原生 WindowsMedia TTS 插件**：基于 WinRT 原生 `Windows.Media.SpeechSynthesis`，无需网络也能发出纯正中英双语语音。

### 5. 📝 学术论文与代码智能预处理 (`SmartTextPreprocessor`)
- **PDF 跨行智能缝合**：自动识别并拼接从 PDF 复制时产生的断行与连字符（`-`）单词断裂。
- **程序员标识符拆分**：自动解析 `camelCase`（驼峰命名）与 `snake_case`（蛇形命名），如将 `getUserById` 智能拆为自然语言进行准确翻译。
- **LaTeX 公式与 Markdown 语法保护**：自动以特征占位符屏蔽 `$...$` 等公式，翻译完成后原位还原，不破坏学术排版。

### 6. 🧠 现代大模型推理链自动剥离
- 完美适配 DeepSeek-R1、OpenAI o1/o3 等推理大模型，自动识别并隐藏 `<think>...</think>` 思维链内容，仅在译文区呈现清晰明了的最终结果。

### 7. 📚 多格式高保真生词本导出
- 一键将历史翻译记录导出为适合记忆复习的 **Anki 卡片包**、带标签分类的 **Markdown 笔记** 或通用 **CSV 表格**。

---

## ⌨️ 快捷键一览

| 快捷键 | 功能名称 | 说明 |
| :--- | :--- | :--- |
| **`Alt + Shift + H`** | **整窗 HUD 贴面汉化** | 点击激活英文软件后按下，原位变成中文，鼠标可穿透点击，按 `Esc` 退出 |
| **`Alt + H`** | **智能鼠标悬停雷达** | 开启后，鼠标悬停在任意英文控件/单词上 0.3 秒即指即翻，移开即隐 |
| **`Alt + Shift + X`** | **有道级图片截屏翻译** | 截取任意屏幕区域，原地取色擦除覆盖翻译 |
| **`Alt + G`** | **打开主翻译窗口** | 呼出常规文本输入交互主界面 |
| **`Alt + D`** | **划词翻译** | 选中任意文本快速查询 |
| **`Alt + S`** | **截屏普通翻译** | 截取图片识别文字并在主窗口显示翻译结果 |
| **`Alt + Shift + S`** | **独立文字识别 (OCR)** | 仅提取屏幕选区中的文字到剪贴板 |

> 所有快捷键均可在软件的 **「设置 -> 快捷键」** 中按需自定义。

---

## 🎯 典型适用场景

1. **工业设计与垂直研发软件（无汉化包场景）**
   - 3D 建模（Blender/Maya 英文插件）、数字音频工作站（Cubase/Ableton DAW）、EDA 电子设计（Altium/KiCad）、医学影像与仪器控制面板。
2. **Steam / 海外单机游戏实时生肉汉化**
   - 玩没有官方中文的欧美独立游戏、文字冒险（AVG）或策略模拟游戏，HUD 贴面汉化配合鼠标穿透，边看中文边畅玩。
3. **学术论文（PDF）沉浸式心流精读**
   - 阅读 arXiv、Nature、IEEE 英文论文，开启鼠标雷达指哪看哪，无需繁琐划词复制，保持 100% 思考心流。
4. **程序员查阅海外文档与排查堆栈报错**
   - 浏览 GitHub Issue、StackOverflow、英文 API 规范，光标悬停即读懂报错与长变量命名。
5. **跨境电商与涉外办公**
   - 熟练操作 Amazon、Shopify 后台或英文报关系统，降低新手误操作风险。

---

## 🛠️ 编译与开发运行

本项目基于 .NET 10.0 与 C# 13 构建：

```powershell
# 1. 克隆代码仓库
git clone https://github.com/Ioveo/STranslate-HUD.git
cd STranslate-HUD

# 2. 编译发布版
dotnet build src/STranslate.slnx -c Release

# 3. 运行主程序
start src/.artifacts/Release/STranslate.exe
```

---

## 🤝 开源致敬与许可证

- **基于开源项目二次开发**：本项目基于 [@zggsong](https://github.com/zggsong) 开发的优秀开源翻译软件 [STranslate](https://github.com/STranslate/STranslate) 深度研发，特此向原作者及贡献者们致以崇高敬意！
- **项目组织与维护者**：[Ioveo](https://github.com/Ioveo)
- **许可证**：本项目采用 [MIT 许可证](./LICENSE)，允许自由使用、修改与分发。

---

<div align="center">

如果 **STranslate-HUD** 帮您解决了无汉化包软件的使用难题，欢迎在 GitHub 点亮右上角的 ⭐️ **Star** 支持我们！

</div>
