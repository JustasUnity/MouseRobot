﻿using System.Drawing;
using System.Text.RegularExpressions;

namespace RobotRuntime
{
    public static class FileExtensions
    {
        public const string Script = "mrb";
        public const string ScriptD = ".mrb";

        public const string Image = "png";
        public const string ImageD = ".png";

        public const string Plugin = "cs";
        public const string PluginD = ".cs";

        public const string Timeline = "mrt";
        public const string TimelineD = ".mrt";
    }

    public static class RegexExpression
    {
        public const string GetNameWithDot = @"[/\\]{1}[\w\d ]+\.";
        public const string GetScriptNameFromPath = GetNameWithDot + FileExtensions.Script;
        public const string GetImageNameFromPath = GetNameWithDot + FileExtensions.Image;
        public const string GetPluginNameFromPath = GetNameWithDot + FileExtensions.Plugin;
        public const string GetTimelineNameFromPath = GetNameWithDot + FileExtensions.Timeline;
    }

    public static class Fonts
    {
        public static Font Default = new Font(FontFamily.GenericSansSerif, 8.25F, FontStyle.Regular);
        public static Font DirtyScript = new Font(FontFamily.GenericSansSerif, 8.25F, FontStyle.Italic);
        public static Font ActiveScript = new Font(FontFamily.GenericSansSerif, 8.25F, FontStyle.Bold);

        public static Font ActiveAndDirtyScript = DirtyScript.AddFont(ActiveScript);

        public static Font Big = new Font(FontFamily.GenericSansSerif, 20F, FontStyle.Bold);
        public static Font Normal = new Font(FontFamily.GenericSansSerif, 12F, FontStyle.Bold);

        public static Font AddFont(this Font main, Font newFont)
        {
            return new Font(
                (newFont.FontFamily == Default.FontFamily) ? main.FontFamily : newFont.FontFamily,
                (newFont.Size == Default.Size) ? main.Size : newFont.Size,
                main.Style | newFont.Style);
        }

        public static Font RemoveFont(this Font main, Font remove)
        {
            return new Font(
                (remove.FontFamily == main.FontFamily) ? Default.FontFamily : main.FontFamily,
                (remove.Size == main.Size) ? Default.Size : main.Size,
                main.Style & ~remove.Style);
        }
    }
}
