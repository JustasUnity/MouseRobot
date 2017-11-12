﻿using RobotRuntime;
using RobotRuntime.Commands;

namespace Robot.Scripts
{
    public static class CommandFactory
    {
        public const string k_X = "X";
        public const string k_Y = "Y";
        public const string k_DontMove = "DontMove";
        public const string k_Smooth = "Smooth";
        public const string k_Asset = "Asset";
        public const string k_Time = "Time";
        public const string k_Timeout = "Timeout";

        public static Command Create(CommandType commandType)
        {
            switch (commandType)
            {
                case CommandType.Down:
                    return new CommandDown(0, 0, false);
                case CommandType.Move:
                    return new CommandMove(0, 0);
                case CommandType.Press:
                    return new CommandPress(0, 0, false);
                case CommandType.Release:
                    return new CommandRelease(0, 0, false);
                case CommandType.Sleep:
                    return new CommandSleep(0);
                case CommandType.MoveOnImage:
                    return new CommandMoveOnImage(default(AssetPointer), 0, false);
                default:
                    return default(Command);
            }
        }

        public static Command Create(CommandType commandType, Command oldCommand)
        {
            var command = Create(commandType);

            CopyPropertiesIfExist(ref command, oldCommand, k_X);
            CopyPropertiesIfExist(ref command, oldCommand, k_Y);
            CopyPropertiesIfExist(ref command, oldCommand, k_DontMove);
            CopyPropertiesIfExist(ref command, oldCommand, k_Smooth);
            CopyPropertiesIfExist(ref command, oldCommand, k_Asset);
            CopyPropertiesIfExist(ref command, oldCommand, k_Time);
            CopyPropertiesIfExist(ref command, oldCommand, k_Timeout);

            return command;
        }

        private static void CopyPropertiesIfExist(ref Command dest, Command source, string name)
        {
            var destProp = dest.GetType().GetProperty(name);
            var sourceProp = source.GetType().GetProperty(name);

            if (destProp != null && sourceProp != null)
            {
                destProp.SetValue(dest, sourceProp.GetValue(source));
            }
        }
    }
}
