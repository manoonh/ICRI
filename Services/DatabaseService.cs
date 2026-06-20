using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CanIRunIt.Models;
using Newtonsoft.Json;

namespace CanIRunIt.Services
{
    public class DatabaseService
    {
        private List<SoftwareRequirements> _software = new();
        private string _databasePath = string.Empty;

        public DatabaseService()
        {
            // للأجهزة المحمولة: دائماً ابحث عن الملف بجوار ملف التشغيل (.exe)
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string localDbPath = Path.Combine(exePath, "db.json");

            if (File.Exists(localDbPath))
            {
                _databasePath = localDbPath;
            }
            else
            {
                // محاولة البحث في المجلدات الأعلى (مفيد أثناء التطوير فقط)
                var possiblePaths = new[]
                {
                    Path.Combine(exePath, "..", "db.json"),
                    Path.Combine(exePath, "..", "..", "db.json"),
                    Path.Combine(exePath, "..", "..", "..", "db.json"),
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        _databasePath = Path.GetFullPath(path);
                        break;
                    }
                }
            }

            // إذا لم يتم العثور عليه، نتركه فارغاً وسيتم التعامل معه في LoadDatabase
        }

        public bool LoadDatabase()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    Console.WriteLine($"Database not found at: {_databasePath}");
                    return false;
                }

                string json = File.ReadAllText(_databasePath);
                _software = JsonConvert.DeserializeObject<List<SoftwareRequirements>>(json) ?? new List<SoftwareRequirements>();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading database: {ex.Message}");
                return false;
            }
        }

        public IEnumerable<SoftwareRequirements> GetAllSoftware()
        {
            return _software;
        }

        public IEnumerable<string> GetAllCategories()
        {
            return _software
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c);
        }

        public IEnumerable<SoftwareRequirements> SearchByName(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _software;

            return _software.Where(s => 
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<SoftwareRequirements> FilterByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || category == "الكل")
                return _software;

            return _software.Where(s => 
                s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<SoftwareRequirements> SearchAndFilter(string query, string category)
        {
            var results = _software.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                results = results.Where(s => 
                    s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "الكل")
            {
                results = results.Where(s => 
                    s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            return results;
        }

        public SoftwareRequirements? GetSoftwareByName(string name)
        {
            return _software.FirstOrDefault(s => 
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public int TotalCount => _software.Count;
        public string DatabasePath => _databasePath;
    }
}
