﻿using RobotRuntime;

namespace Robot
{
    public static class CommandExtension
    {
        public static int GetIndex(this Command command)
        {
            return ScriptManager.Instance.GetScriptFromCommand(command).Commands.IndexOf(command);
        }

        public static bool CanBeNested(this Command command)
        {
            return command.CommandType == CommandType.ForeachImage || command.CommandType == CommandType.ForImage;
        }
    }
}
