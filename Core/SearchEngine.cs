using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KeJian.Core
{
    /// <summary>
    /// 全文搜索引擎 - 遍历所有日记文件进行关键词匹配
    /// 支持标题搜索、内容搜索、标签搜索
    /// </summary>
    public class SearchEngine
    {
        private readonly DiaryStorage _storage;

        public SearchEngine(DiaryStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// 搜索结果项
        /// </summary>
        public class SearchResult
        {
            public string Date { get; set; }
            public string Title { get; set; }
            public string Snippet { get; set; }
            public List<string> Tags { get; set; }
            public double Score { get; set; }
        }

        /// <summary>
        /// 搜索日记（同步 + 异步双模式）
        /// </summary>
        public List<SearchResult> Search(string query)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            var keywords = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim().ToLower())
                .Where(k => k.Length > 0)
                .ToArray();

            if (keywords.Length == 0)
                return results;

            var allDates = _storage.GetAllDates();

            foreach (var date in allDates)
            {
                var entry = _storage.Load(date);
                if (entry == null || entry.IsDeleted)
                    continue;

                double score = 0;
                string snippet = "";

                // 标题匹配（权重最高）
                foreach (var kw in keywords)
                {
                    if (entry.Title?.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score += 10;
                        snippet = GetSnippet(entry.Title, kw);
                    }
                }

                // 正文内容匹配
                foreach (var kw in keywords)
                {
                    var content = entry.Content ?? "";
                    var idx = content.IndexOf(kw, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        score += 5;
                        snippet = GetSnippet(content, kw);
                    }
                }

                // 标签匹配
                foreach (var kw in keywords)
                {
                    if (entry.Tags.Any(t => t.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        score += 8;
                    }
                }

                if (score > 0)
                {
                    results.Add(new SearchResult
                    {
                        Date = entry.Date,
                        Title = entry.Title ?? "(无标题)",
                        Snippet = snippet,
                        Tags = entry.Tags,
                        Score = score
                    });
                }
            }

            // 按匹配度排序
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            return results;
        }

        /// <summary>
        /// 异步搜索（不阻塞 UI）
        /// </summary>
        public Task<List<SearchResult>> SearchAsync(string query)
        {
            return Task.Run(() => Search(query));
        }

        /// <summary>根据标签筛选日记</summary>
        public List<string> GetDatesByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return new List<string>();

            var result = new List<string>();
            tag = tag.ToLower();

            foreach (var date in _storage.GetAllDates())
            {
                var entry = _storage.Load(date);
                if (entry?.Tags?.Any(t => t.ToLower() == tag) == true)
                {
                    result.Add(date);
                }
            }

            return result;
        }

        /// <summary>获取所有已使用的标签</summary>
        public List<string> GetAllTags()
        {
            var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var date in _storage.GetAllDates())
            {
                var entry = _storage.Load(date);
                if (entry?.Tags != null)
                {
                    foreach (var tag in entry.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                            tagSet.Add(tag.Trim());
                    }
                }
            }

            return tagSet.OrderBy(t => t).ToList();
        }

        /// <summary>获取搜索摘要片段</summary>
        private string GetSnippet(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var idx = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return Truncate(text, 60);

            var start = Math.Max(0, idx - 30);
            var length = Math.Min(keyword.Length + 60, text.Length - start);
            if (start + length > text.Length)
                length = text.Length - start;

            var snippet = text.Substring(start, length);
            if (start > 0) snippet = "..." + snippet;
            if (start + length < text.Length) snippet += "...";

            return snippet;
        }

        private string Truncate(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLen)
                return text;
            return text.Substring(0, maxLen) + "...";
        }
    }
}
