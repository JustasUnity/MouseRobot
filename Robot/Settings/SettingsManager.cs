﻿using RobotRuntime.IO;
using RobotRuntime.Settings;
using System;
using System.IO;
using System.Linq;

namespace Robot.Settings
{
    public class SettingsManager
    {
        public RecordingSettings RecordingSettings { get; private set; }
        public FeatureDetectionSettings FeatureDetectionSettings { get; private set; }

        private readonly string k_AppName = "\\MouseRobot\\";
        private readonly string k_RoamingAppdataPath;
        private readonly string k_LocalAppdataPath;


        static private SettingsManager m_Instance = new SettingsManager();
        static public SettingsManager Instance { get { return m_Instance; } }
        private SettingsManager()
        {
            k_RoamingAppdataPath = Environment.GetFolderPath(Environment.SpecialFolder.Applicat‌​ionData) + k_AppName;
            k_LocalAppdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + k_AppName;

            CreateIfNotExist(k_RoamingAppdataPath);
            CreateIfNotExist(k_LocalAppdataPath);

            RestoreDefaults();
            RestoreSettings();
        }

        ~SettingsManager()
        {
            SaveSettings();
        }

        public void SaveSettings()
        {
            WriteToSettingFile(RecordingSettings);
            WriteToSettingFile(FeatureDetectionSettings);
        }

        public void RestoreSettings()
        {
            RecordingSettings = RestoreSettingFromFile(RecordingSettings);
            FeatureDetectionSettings = RestoreSettingFromFile(FeatureDetectionSettings);
        }

        public void RestoreDefaults()
        {
            RecordingSettings = new RecordingSettings();
            FeatureDetectionSettings = new FeatureDetectionSettings();
        }

        private void WriteToSettingFile<T>(T settings) where T : BaseSettings
        {
            string filePath = RoamingAppdataPathFromType(settings);
            new YamlObjectIO().SaveObject(filePath, settings);
        }

        private T RestoreSettingFromFile<T>(T settings) where T : BaseSettings
        {
            string filePath = RoamingAppdataPathFromType(settings);
            if (File.Exists(filePath))
                return new YamlObjectIO().LoadObject<T>(filePath);
            else
                return settings;
        }

        private string RoamingAppdataPathFromType<T>(T settings) where T : BaseSettings
        {
            string fileName = FileNameFromType(settings);
            var filePath = k_RoamingAppdataPath + fileName;
            return filePath;
        }

        private static string FileNameFromType<T>(T type) where T : BaseSettings
        {
            return type.ToString().Split('.').Last() + ".config";
        }

        private static void CreateIfNotExist(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}