using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ImageSplitter.Models
{
    public class LocalizationService
    {
        private readonly Dictionary<string, string> _currentTranslations = new();
        public List<string> AvailableLanguages { get; private set; } = new();

        public LocalizationService()
        {
            DiscoverEmbeddedLanguages();

            foreach (string lang in AvailableLanguages)
            {
                SetLanguage(lang);
            }

            if (AvailableLanguages.Contains("en-US"))
            {
                SetLanguage("en-US");
            }
            else if (AvailableLanguages.Count > 0)
            {
                SetLanguage(AvailableLanguages[0]);
            }
        }

        private void DiscoverEmbeddedLanguages()
        {
            AvailableLanguages.Clear();
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            string prefix = "ImageSplitter.Locales.";
            string suffix = ".json";

            foreach (string name in resourceNames)
            {
                if (name.StartsWith(prefix) && name.EndsWith(suffix))
                {
                    string langCode = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
                    AvailableLanguages.Add(langCode);
                }
            }

            if (AvailableLanguages.Count == 0)
            {
                AvailableLanguages.Add("en-US");
            }
        }

        public void SetLanguage(string langCode)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"ImageSplitter.Locales.{langCode}.json";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return;

                using (StreamReader reader = new StreamReader(stream))
                {
                    try
                    {
                        string jsonContent = reader.ReadToEnd();
                        var jsonResult = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                        if (jsonResult != null)
                        {
                            foreach (var kvp in jsonResult)
                            {
                                _currentTranslations[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public string Get(string key)
        {
            return _currentTranslations.TryGetValue(key, out var value) ? value : key;
        }
    }
}