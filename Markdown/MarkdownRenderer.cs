using System;
using System.IO;
using System.Reflection;
using System.Text;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;

namespace KeJian.Markdown
{
    /// <summary>
    /// Markdown 渲染器 - 使用 Markdig 将 Markdown 转换为 HTML
    /// </summary>
    public class MarkdownRenderer
    {
        private readonly MarkdownPipeline _pipeline;
        private readonly string _cssContent;
        private static readonly string _htmlTemplate;

        static MarkdownRenderer()
        {
            // HTML 完整模板（主题框架）
            _htmlTemplate = @"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<style>
{STYLE}
</style>
</head>
<body>
<div class=""diary-content"">
{CONTENT}
</div>
<script>
// 点击图片放大
document.addEventListener('click', function(e) {{
    if (e.target.tagName === 'IMG') {{
        if (e.target.classList.contains('zoomed')) {{
            e.target.classList.remove('zoomed');
        }} else {{
            e.target.classList.add('zoomed');
        }}
    }}
}});
</script>
</body>
</html>";
        }

        public MarkdownRenderer()
        {
            // 配置 Markdig pipeline - 启用完整 GFM 功能
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()      // 启用所有扩展（表格、任务列表、脚注等）
                .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // 标题自动生成锚点
                .UseEmojiAndSmiley()          // 支持 :smile: 表情
                .UseSoftlineBreakAsHardlineBreak() // 软回车变 <br>
                .Build();

            // 加载内置 CSS 资源
            _cssContent = LoadCss();
        }

        /// <summary>将 Markdown 文本转换为完整 HTML 页面</summary>
        public string RenderToHtml(string markdown)
        {
            var body = RenderToBody(markdown);
            return _htmlTemplate
                .Replace("{STYLE}", _cssContent)
                .Replace("{CONTENT}", body);
        }

        /// <summary>仅渲染 Markdown 为 HTML body（无 HTML 骨架）</summary>
        public string RenderToBody(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return "<p style='color:#888; text-align:center; margin-top:40vh;'>✨ 开始写今天的日记吧...</p>";

            try
            {
                return Markdown.ToHtml(markdown, _pipeline);
            }
            catch (Exception ex)
            {
                return $"<p style='color:red'>渲染错误: {ex.Message}</p>";
            }
        }

        /// <summary>从嵌入资源加载 CSS 样式</summary>
        private string LoadCss()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "KeJian.Resources.markdown.css";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载 CSS 失败: {ex.Message}");
            }

            // 备用内联样式
            return @"
body { background:#fff; color:#333; font:16px/1.7 'Microsoft YaHei','Segoe UI',sans-serif; padding:20px; max-width:800px; margin:0 auto; }
.diary-content h1 { border-bottom:2px solid #eee; padding-bottom:8px; }
code { background:#f5f5f5; padding:2px 6px; border-radius:3px; font-size:0.9em; }
pre code { background:#f8f8f8; display:block; padding:12px; overflow-x:auto; border-radius:6px; }
table { border-collapse:collapse; width:100%; }
th, td { border:1px solid #ddd; padding:8px 12px; text-align:left; }
th { background:#f5f5f5; }
blockquote { border-left:4px solid #ddd; margin:0; padding:4px 16px; color:#666; }
img { max-width:100%; border-radius:4px; cursor:pointer; transition:transform .2s; }
img.zoomed { transform:scale(1.5); }
";
        }

        /// <summary>导出纯 HTML 文件</summary>
        public string ExportToHtmlFile(string markdown, string title)
        {
            var html = RenderToHtml(markdown);
            var styledTitle = string.IsNullOrEmpty(title) ? "日记" : title;

            // 替换标题
            html = html.Replace("<title></title>", $"<title>{styledTitle}</title>");

            return html;
        }
    }
}
