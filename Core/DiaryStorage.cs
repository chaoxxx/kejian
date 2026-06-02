using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace KeJian.Core
{
    /// <summary>
    /// 日记持久化引擎 - JSON 文件存储
    /// 每篇日记存为一个独立 JSON 文件: data/yyyy/MM/dd.json
    /// 日记数量达到一定规模时性能依旧稳定，因为每次只操作单个文件
    /// </summary>
    public class DiaryStorage
    {
        private readonly string _dataRoot;
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
        };

        // 内存缓存：最近访问的日记，避免频繁读盘
        private readonly Dictionary<string, DiaryEntry> _cache = new Dictionary<string, DiaryEntry>();
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private const int MaxCacheSize = 50;

        /// <summary>数据根目录（默认在 exe 同目录下）</summary>
        public string DataRoot => _dataRoot;

        public DiaryStorage()
        {
            // 数据目录默认在 exe 所在位置
            _dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            EnsureDataDir();
        }

        public DiaryStorage(string customPath)
        {
            _dataRoot = customPath;
            EnsureDataDir();
        }

        private void EnsureDataDir()
        {
            Directory.CreateDirectory(_dataRoot);
        }

        /// <summary>获取日记文件路径</summary>
        private string GetFilePath(string date)
        {
            var parts = date.Split('-');
            if (parts.Length != 3) return null;
            var dir = Path.Combine(_dataRoot, parts[0], parts[1]);
            return Path.Combine(dir, $"{date}.json");
        }

        /// <summary>加载指定日期的日记</summary>
        public DiaryEntry Load(string date)
        {
            // 尝试从缓存读取
            _cacheLock.EnterReadLock();
            try
            {
                if (_cache.TryGetValue(date, out var cached))
                    return cached;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            var path = GetFilePath(date);
            if (path == null || !File.Exists(path))
                return new DiaryEntry { Date = date };

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var entry = JsonConvert.DeserializeObject<DiaryEntry>(json, _jsonSettings);
                if (entry != null)
                {
                    entry.Date = date; // 确保日期正确
                    AddToCache(date, entry);
                    return entry;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载日记失败 [{date}]: {ex.Message}");
            }

            return new DiaryEntry { Date = date };
        }

        /// <summary>保存日记</summary>
        public void Save(DiaryEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Date))
                return;

            entry.UpdatedAt = DateTime.UtcNow;

            var path = GetFilePath(entry.Date);
            if (path == null) return;

            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(entry, _jsonSettings);
            // 使用临时文件 + 替换方式，避免写入中断导致文件损坏
            var tmpPath = path + ".tmp";
            File.WriteAllText(tmpPath, json, Encoding.UTF8);
            File.Delete(path); // 删原文件
            File.Move(tmpPath, path);

            AddToCache(entry.Date, entry);
        }

        /// <summary>删除指定日期的日记</summary>
        public bool Delete(string date)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cache.Remove(date);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            var path = GetFilePath(date);
            if (path != null && File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }

        /// <summary>获取所有日记日期列表（降序排列）</summary>
        public List<string> GetAllDates()
        {
            var dates = new List<string>();

            if (!Directory.Exists(_dataRoot))
                return dates;

            foreach (var yearDir in Directory.GetDirectories(_dataRoot))
            {
                foreach (var monthDir in Directory.GetDirectories(yearDir))
                {
                    foreach (var file in Directory.GetFiles(monthDir, "*.json"))
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        dates.Add(name);
                    }
                }
            }

            dates.Sort((a, b) => string.Compare(b, a, StringComparison.Ordinal)); // 降序
            return dates;
        }

        /// <summary>获取某年某月的所有日记日期</summary>
        public List<string> GetDatesInMonth(int year, int month)
        {
            var dir = Path.Combine(_dataRoot, year.ToString("D4"), month.ToString("D2"));
            if (!Directory.Exists(dir))
                return new List<string>();

            return Directory.GetFiles(dir, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderByDescending(d => d)
                .ToList();
        }

        /// <summary>获取某年某月有日记的天数</summary>
        public int GetDiaryCountInMonth(int year, int month)
        {
            return GetDatesInMonth(year, month).Count;
        }

        /// <summary>总日记数量</summary>
        public int TotalCount => GetAllDates().Count;

        /// <summary>写入缓存</summary>
        private void AddToCache(string date, DiaryEntry entry)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                if (_cache.Count >= MaxCacheSize)
                {
                    // 移除最早访问的一项
                    var first = _cache.Keys.FirstOrDefault();
                    if (first != null)
                        _cache.Remove(first);
                }
                _cache[date] = entry;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>清除缓存</summary>
        public void ClearCache()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cache.Clear();
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
    }
}
