﻿using RobotRuntime.Utils.Win32;
using System;
using System.Drawing;

namespace RobotRuntime.Commands
{
    [Serializable]
    public class CommandMoveOnImage : Command
    {
        public AssetPointer Asset { get; set; }

        public CommandMoveOnImage(AssetPointer asset)
        {
            Asset = asset;
        }

        public override object Clone()
        {
            return new CommandMoveOnImage(Asset);
        }

        public override void Run()
        {
            int x1, y1;
            x1 = WinAPI.GetCursorPosition().X;
            y1 = WinAPI.GetCursorPosition().Y;

            for (int i = 1; i <= 50; i++)
            {
                // WinAPI.MouseMoveTo(x1 + ((X - x1) * i / 50), y1 + ((Y - y1) * i / 50));
            }
        }
    }
}