﻿using System;
using System.Drawing;
using RobotRuntime.Settings;

namespace RobotRuntime.Abstractions
{
    public interface IFeatureDetectionThread : IStableRepeatingThread
    {
        object ObservedImageLock { get; }

        DetectionMode DetectionMode { get; set; }
        Point[][] LastKnownPositions { get; }
        Bitmap ObservedImage { get; }
        int TimeSinceLastFind { get; }
        bool WasImageFound { get; }
        bool WasLastCheckSuccess { get; }

        event Action<Point[][]> PositionFound;

        //void Init();
        void StartNewImageSearch(string path);
    }
}