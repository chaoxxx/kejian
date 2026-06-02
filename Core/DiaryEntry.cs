using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KeJian.Core
{
    /// <summary>
    /// 单篇日记数据模型
    /// </summary>
    public class DiaryEntry
    {
        /// <summary>日记日期 (yyyy-MM-dd 格式)</summary>
        [JsonProperty("date")]
        public string Date { get; set; }

        /// <summary>日记标题</summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>日记 Markdown 正文</summary>
        [JsonProperty("content")]
        public string Content { get; set; }

        /// <summary>标签列表</summary>
        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>创建时间 (UTC)</summary>
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>最后修改时间 (UTC)</summary>
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>是否已标记删除（用于软删除）</summary>
        [JsonProperty("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [JsonIgnore]
        public DateTime DateParsed
        {
            get
            {
                if (DateTime.TryParseExact(Date, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var dt))
                    return dt;
                return DateTime.Today;
            }
        }

        /// <summary>获取纯文本（用于搜索，去除 Markdown 标记）</summary>
        [JsonIgnore]
        public string PlainText
        {
            get
            {
                if (string.IsNullOrEmpty(Content)) return string.Empty;
                // 简单去除 Markdown 标记用于搜索
                var text = Content;
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\#{1,6}\s+", "");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "$1");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"~~(.+?)~~", "$1");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"`{1,3}[^`]*`{1,3}", "");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"!\[([^\]]*)\]\([^)]+\)", "$1");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"^\s*[-*+]\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
                text = System.Text.RegularExpressions.Regex.Replace(text, @"^\s*\d+\.\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
                text = System.Text.RegularExpressions.Regex.Replace(text, @"^\s*>\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
                return text.Trim();
            }
        }

        /// <summary>字数量</summary>
        [JsonIgnore]
        public int WordCount => string.IsNullOrEmpty(Content) ? 0 : Content.Length;

        public DiaryEntry()
        {
            Date = DateTime.Today.ToString("yyyy-MM-dd");
        }

        public DiaryEntry(DateTime date) : this()
        {
            Date = date.ToString("yyyy-MM-dd");
        }
    }
}
