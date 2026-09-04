<div align="center">

<p>
<a href="https://github.com/STranslate/STranslate" target="_blank">
<img align="center" alt="STranslate" width="200" src="./images/favicon.svg" />
</a>
</p>
<p>
<a href="https://github.com/STranslate/STranslate/blob/main/LICENSE" target="_self">
 <img alt="Latest GitHub release" src="https://img.shields.io/github/license/ZGGSONG/STranslate" />
</a>
<a href="https://github.com/STranslate/STranslate/releases/latest" target="_blank">
 <img alt="Latest GitHub release" src="https://img.shields.io/github/release/ZGGSONG/STranslate.svg" />
</a>
<a href="https://github.com/STranslate/STranslate/releases" target="_self">
 <img alt="Downloads" src="https://img.shields.io/github/downloads/ZGGSONG/STranslate/total" />
</a>
<a href="https://github.com/STranslate/STranslate/discussions" target="_self">
 <img alt="Discussions" src="https://img.shields.io/github/discussions/ZGGSONG/STranslate" />
</a>
</p>

<h1 align="center">STranslate-HUD</h1>

[**English**](./README.md) | **简体中文**

<p align="center">基于 STranslate 深度升级的<strong>全场景无感翻译工具</strong>：整窗跟随穿透贴面汉化、智能鼠标悬停雷达、有道级图片擦除翻译与纯离线生态。</p>

> **致敬原作者**：本项目基于优秀开源项目 [STranslate](https://github.com/STranslate/STranslate) (@zggsong) 深度二次开发，在此致以诚挚敬意。

### 🚀 STranslate-HUD 核心升级特性

1. **整窗实时跟随穿透 HUD 贴面汉化 (`Alt + Shift + H`)**：
   - 专为“无汉化包的全英文软件”打造。自动探测目标窗口所有英文控件树（自绘软件全窗离线 OCR 兜底），原地覆盖半透明中文徽标。
   - **鼠标完全穿透点击原软件**，像汉化版一样正常点击操作，窗口拖动缩放实时跟随。按 `Esc` 随时退出。
2. **智能鼠标悬停雷达 (`Alt + H`)**：
   - **免点击、免划词、免复制**：鼠标指针移动到英文单词、按钮或菜单上停留 0.3 秒，光标旁自动弹出中文释义胶囊气泡，移开鼠标自动消失。
   - UIA 控件树探测 + 微区域快照 OCR 双引擎，全场景通用。
3. **有道级图片原位擦除翻译 (`Alt + Shift + X`)**：
   - **智能边缘背景取色器 (`ImageColorSampler`)**：自动吸附周围真实背景底色无痕覆盖，自适应字号排版。
   - **Auto-Fallback 免配置自动绑定**：智能复用当前可用翻译与 OCR 引擎，开箱即用。
4. **Windows 原生离线 OCR & TTS 引擎**：
   - 基于 WinRT 原生 API，零网络依赖、毫秒级本地多点文字识别与高质量真人语音朗读。
5. **学术与编程智能文本清洗 (`SmartTextPreprocessor`)**：
   - PDF 跨行自动缝合与断词修复、代码驼峰/蛇形分词、LaTeX 数学公式与 Markdown 语法占位保护。
6. **现代推理模型思考过程剥离**：
   - 自动隐藏 DeepSeek-R1 / OpenAI 等推理模型的思维链（`<think>`），保持译文清爽。
7. **多格式生词本导出**：
   - 支持一键导出为 Anki 记忆卡片、Markdown 笔记与通用 CSV 表格。

</div>

## 访问

| 国外 | 国内 |
| :--: | :--: |
| **[Github](https://github.com/STranslate/STranslate)** | **[Gitee](https://gitee.com/zggsong/STranslate)** |


## 安装

下载最新 [Release](https://github.com/STranslate/STranslate/releases) 版本后解压即可使用

## 使用

[Document](https://stranslate.zggsong.com)

## 讨论

有疑问移步 [Discussions](https://github.com/STranslate/STranslate/discussions) 进行讨论

> 如果您想加入用户交流群，可以扫描下方二维码

<img src="./images/telegram_group.jpg" Width="160" />

## 感谢

<a href="https://jb.gg/OpenSourceSupport"><img src="./images/jb_beam.svg" /></a>

## 打赏

觉得不错的话可以请作者喝杯阔落

> 感谢打赏的朋友 [赞赏列表](Sponsor.md)

| 微信 | 支付宝 |
| :--: | :--: |
|![wehcatpay](./images/wechatpay.jpg) | ![alipay](./images/alipay.jpg) |

## 作者

**STranslate**

版权所有 © [zggsong](https://github.com/zggsong)

- 原始作者：[@zggsong](https://github.com/zggsong)
- 项目组织：[STranslate](https://github.com/STranslate)
- 许可证：[MIT](./LICENSE)

> [Website](https://stranslate.zggsong.com) [Blog](https://www.zggsong.com)

## 星标历史

[![Star History Chart](https://star-history.dera.page/svg?repos=ZGGSONG/STranslate&type=Date)](https://star-history.dera.page/#ZGGSONG/STranslate&Date)
