namespace STranslate.Plugin;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 文本处理辅助工具。
/// </summary>
public static partial class TextHelper
{
    /// <summary>
    /// 判断字符是否属于 CJK（中日韩）文字区域，包含 CJK 统一表意、扩展A、兼容、日文假名、韩文谚文。
    /// </summary>
    public static bool IsCjk(char ch) =>
        (ch >= '\u3400' && ch <= '\u9fff') ||
        (ch >= '\uf900' && ch <= '\ufaff') ||
        (ch >= '\u3040' && ch <= '\u30ff') ||
        (ch >= '\uac00' && ch <= '\ud7af');

    /// <summary>
    /// 拆分代码标识符（驼峰命名 camelCase, 帕斯卡命名 PascalCase, 蛇形命名 snake_case, 短横线命名 kebab-case）
    /// </summary>
    /// <param name="identifier">代码标识符字符串</param>
    /// <returns>拆分后的自然词汇句子</returns>
    public static string SplitIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return identifier;

        // 替换下划线和短横线为空格
        var text = identifier.Replace('_', ' ').Replace('-', ' ');

        // 在小写字母和大写字母之间插入空格 (如 getUserInfo -> get User Info)
        text = CamelCaseRegex().Replace(text, "$1 $2");

        // 在连续大写字母与后续小写字母之间插入空格 (如 XMLParser -> XML Parser)
        text = MultiUpperRegex().Replace(text, "$1 $2");

        // 在数字与字母交界处插入空格 (如 utf8Encoding -> utf 8 Encoding)
        text = LetterDigitRegex().Replace(text, "$1 $2");
        text = DigitLetterRegex().Replace(text, "$1 $2");

        // 清理多余连续空格
        return MultipleSpacesRegex().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"([a-z])([A-Z])")]
    private static partial Regex CamelCaseRegex();

    [GeneratedRegex(@"([A-Z]+)([A-Z][a-z])")]
    private static partial Regex MultiUpperRegex();

    [GeneratedRegex(@"([a-zA-Z])([0-9])")]
    private static partial Regex LetterDigitRegex();

    [GeneratedRegex(@"([0-9])([a-zA-Z])")]
    private static partial Regex DigitLetterRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();
}

