﻿using RobotEditor.Settings;
using RobotRuntime.Settings;

namespace RobotEditor.Abstractions
{
    public interface IPropertiesWindow
    {
        void ShowProperties<T>(T properties) where T : BaseProperties;
        void ShowSettings<T>(T settings) where T : BaseSettings;
    }
}