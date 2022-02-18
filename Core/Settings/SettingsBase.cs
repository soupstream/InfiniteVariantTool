using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace InfiniteVariantTool.Core.Settings
{

    public abstract class SettingsBase
    {
        private string name;
        private const string settingsFilename = "settings.json";
        private Dictionary<string, object?> defaults;

        protected const string SettingsDirectoryVar = "$(SettingsDirectory)";

        public SettingsBase(string name)
        {
            this.name = name;
            defaults = new();
            SetDefaults();
            Load();
        }

        public object? Get(string key)
        {
            foreach (var prop in GetType().GetProperties())
            {
                if (prop.Name == key && prop.GetCustomAttributes(typeof(SettingAttribute), true).Length > 0)
                {
                    return prop.GetValue(this);
                }
            }
            return null;
        }

        public bool Set(string key, string value)
        {
            foreach (var prop in GetType().GetProperties())
            {
                if (prop.Name == key && prop.GetCustomAttributes(typeof(SettingAttribute), true).Length > 0)
                {
                    var converter = TypeDescriptor.GetConverter(prop.PropertyType);
                    object convertedValue;
                    try
                    {
                        convertedValue = converter.ConvertFrom(value) ?? throw new Exception();
                    }
                    catch
                    {
                        break;
                    }
                    prop.SetValue(this, convertedValue);
                    return true;
                }
            }
            return false;
        }

        public bool Reset(string key)
        {
            foreach (var prop in GetType().GetProperties())
            {
                if (prop.Name == key && defaults.ContainsKey(prop.Name) && prop.GetCustomAttributes(typeof(SettingAttribute), true).Length > 0)
                {
                    prop.SetValue(this, defaults[prop.Name]);
                    return true;
                }
            }
            return false;
        }

        private void SetDefaults()
        {
            foreach (var prop in GetType().GetProperties())
            {
                if (prop.GetCustomAttributes(typeof(SettingAttribute), true).Length > 0)
                {
                    object? value = prop.GetValue(this);
                    PreprocessDefault(prop, ref value);
                    defaults[prop.Name] = value;
                }
            }
        }

        private void PreprocessDefault(PropertyInfo prop, ref object? value)
        {
            if (value is string stringValue)
            {
                value = stringValue.Replace(SettingsDirectoryVar, SettingsDirectory);
                prop.SetValue(this, value);
            }
        }

        private string SettingsDirectory
        {
            get
            {
                string appdataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appdataDir, name);
            }
        }

        protected string SettingsFilePath => Path.Combine(SettingsDirectory, settingsFilename);

        protected bool CreateFileIfNotExists()
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }
            if (!File.Exists(SettingsFilePath))
            {
                Save();
                return true;
            }
            return false;
        }

        public void Save()
        {
            Dictionary<string, string> settings = new();
            foreach (var prop in GetType().GetProperties())
            {
                if (prop.GetCustomAttributes(typeof(SettingAttribute), true).Length > 0)
                {
                    string? value = prop.GetValue(this)?.ToString();
                    if (value != null)
                    {
                        settings[prop.Name] = value;
                    }
                }
            }
            File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(settings));
        }

        protected void Load()
        {
            if (CreateFileIfNotExists())
            {
                return;
            }
            var newSettings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(SettingsFilePath));
            if (newSettings != null)
            {
                foreach (var prop in GetType().GetProperties())
                {
                    if (prop.GetCustomAttributes(typeof(SettingAttribute), true).Length > 0)
                    {
                        if (newSettings.ContainsKey(prop.Name))
                        {
                            var converter = TypeDescriptor.GetConverter(prop.PropertyType);
                            object value;
                            try
                            {
                                value = converter.ConvertFrom(newSettings[prop.Name]) ?? throw new Exception();
                            }
                            catch
                            {
                                continue;
                            }

                            prop.SetValue(this, value);
                        }
                    }
                }
            }
        }



        // returns (property name, setting attribute, property value)
        public IEnumerable<(string, SettingAttribute, object)> GetSettingsInfo()
        {
            foreach (var prop in GetType().GetProperties())
            {
                foreach (var attr in prop.GetCustomAttributes(typeof(SettingAttribute), true))
                {
                    yield return (prop.Name, (SettingAttribute)attr, prop.GetValue(this)!);
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SettingAttribute : Attribute
    {
        public string Description { get; }
        public SettingAttribute(string description)
        {
            Description = description;
        }
    }
}
