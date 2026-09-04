<div align="center">

<p>
  <a href="https://github.com/Ioveo/STranslate-HUD" target="_blank">
    <img align="center" alt="STranslate-HUD" width="160" src="./images/favicon.svg" />
  </a>
</p>

# STranslate-HUD

<p align="center">
  <strong>A Supercharged, Zero-Friction Desktop Translation Ecosystem for Windows</strong><br>
  Real-Time Follow & Click-Through HUD Overlay · Smart Cursor Hover Radar · Color-Adaptive Image Translation · Offline WinRT Multimedia Pipeline
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

**English** | [**简体中文**](./README_ZH.md)

</div>

---

> 💡 **Project Vision**: When you launch a foreign-language professional software or PC game and **cannot find any translation pack**, you no longer need tedious screenshots or phone cameras. **STranslate-HUD** works like AR smart glasses — translating the UI in-place while letting your **mouse click straight through to interact with the underlying app**!

---

## 🚀 Key Supercharged Features

### 1. 🥽 Live HUD In-Place Software Overlay (`Alt + Shift + H`)
- **Zero-Invasion, No Localization Packs Needed**: No patching binaries or modifying system files.
- **In-Place UI Element Badging**: Scans UI elements via Windows UI Automation (or full-window offline OCR fallback for DirectX/Canvas apps) and renders crisp translated badges right on top of original texts.
- **Full Click-Through**: Built with `WS_EX_TRANSPARENT` layered windows — you can directly click the underlying buttons and menus through the translated badges.
- **Real-Time Window Tracking**: Tracks target window movements and resizing at 50ms intervals. Press `Esc` anytime to dismiss.

### 2. 🎯 Smart Hover Translate Radar (`Alt + H`)
- **Zero Clicks, Zero Copying**: Move mouse over any English word, button, or menu and pause for 0.3s — an elegant pill tooltip pops up next to your cursor with source text, translation, and one-click copy.
- **Dual-Engine Detection**: Fast UIA extraction with micro-region screenshot OCR fallback for web canvas, games, and PDFs.
- **Distraction-Free**: Tooltip fades away automatically as soon as the mouse moves away.

### 3. 🖼️ In-Place Color-Adaptive Image Translation (`Alt + Shift + X`)
- **Smart Edge-Color Sampler (`ImageColorSampler`)**: Samples true edge background colors with median filtering, seamlessly blending translated badges with the original image.
- **Auto-Fit Typography**: Calculates dynamic font sizes and margins based on original text bounding boxes.
- **Auto-Fallback Engine Binding**: Automatically binds available translation and OCR engines without requiring tedious manual configuration.

### 4. ⚡ Native Offline WindowsMedia OCR & TTS
- **Native WindowsMedia OCR**: Powered by WinRT `Windows.Media.Ocr`. 100% offline, 0ms network latency, with precise multi-point bounding boxes.
- **Native WindowsMedia TTS**: Powered by WinRT `Windows.Media.SpeechSynthesis`. High-quality bilingual voice output without network connectivity.

### 5. 📝 Academic & Developer Text Preprocessing (`SmartTextPreprocessor`)
- **PDF Line Healing**: Automatically merges fragmented PDF lines and hyphenated (`-`) word splits.
- **Code Identifier Splitting**: Splits `camelCase` and `snake_case` variable names into natural phrases for accurate translation.
- **LaTeX & Markdown Shield**: Protects math formulas (`$...$`) and markdown tokens during translation and restores them verbatim.

### 6. 🧠 AI Reasoning Chain Filtering
- Automatically filters out internal `<think>...</think>` reasoning chains from models like DeepSeek-R1 / OpenAI o1/o3, delivering pure, concise translations.

### 7. 📚 Multi-Format Vocabulary Export
- One-click export of translation history into **Anki flashcards**, formatted **Markdown notes**, or standard **CSV tables**.

---

## ⌨️ Shortcut Cheatsheet

| Hotkey | Feature | Description |
| :--- | :--- | :--- |
| **`Alt + Shift + H`** | **Live HUD Software Overlay** | Activate target English window, press to translate in-place with click-through. Press `Esc` to exit |
| **`Alt + H`** | **Hover Translate Radar** | Hover cursor on any English UI element for 0.3s to view instant translation |
| **`Alt + Shift + X`** | **In-Place Image Translation** | Snip any screen region for in-place color-adaptive translated overlays |
| **`Alt + G`** | **Open Main Window** | Open standard input translation window |
| **`Alt + D`** | **Selection Translate** | Instant popup for highlighted text |
| **`Alt + S`** | **Screenshot Translate** | Capture screen region and view results in main window |
| **`Alt + Shift + S`** | **OCR Text Extraction** | Extract text from screen region to clipboard |

> All hotkeys can be customized in **Settings -> Hotkeys**.

---

## 🎯 Practical Use Cases

1. **Foreign Industrial & Specialized Engineering Software**
   - 3D modeling (Blender/Maya plugins), Audio DAWs (Cubase/Ableton VSTs), EDA (Altium/KiCad), and medical/lab instruments with no Chinese localization.
2. **PC / Steam Game Real-Time Translation**
   - Play English-only indie games, visual novels, or grand strategy games with real-time HUD overlays without breaking immersion.
3. **Immersive Academic PDF Paper Reading**
   - Read arXiv, IEEE, and Nature papers in flow without copy-pasting into web browsers.
4. **Developer Code & Stack Trace Reading**
   - Hover over compiler errors, complex identifiers, and GitHub issues with automatic camelCase splitting.

---

## 🛠️ Build & Run

Built with .NET 10.0 and C# 13:

```powershell
# 1. Clone the repository
git clone https://github.com/Ioveo/STranslate-HUD.git
cd STranslate-HUD

# 2. Build Release configuration
dotnet build src/STranslate.slnx -c Release

# 3. Run executable
start src/.artifacts/Release/STranslate.exe
```

---

## 🤝 Credits & License

- **Credits**: Forked and enhanced from [STranslate](https://github.com/STranslate/STranslate) by [@zggsong](https://github.com/zggsong). Heartfelt thanks to the original author and contributors!
- **Maintainer**: [Ioveo](https://github.com/Ioveo)
- **License**: Released under the [MIT License](./LICENSE).

---

<div align="center">

If **STranslate-HUD** makes your daily workflow better, give us a ⭐️ **Star** on GitHub!

</div>
