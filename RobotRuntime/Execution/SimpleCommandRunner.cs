﻿using RobotRuntime.Abstractions;
using RobotRuntime.Commands;
using RobotRuntime.Utils;
using System;

namespace RobotRuntime.Execution
{
    [SupportedType(typeof(CommandDown))]
    [SupportedType(typeof(CommandMove))]
    [SupportedType(typeof(CommandPress))]
    [SupportedType(typeof(CommandRelease))]
    [SupportedType(typeof(CommandSleep))]
    public class SimpleCommandRunner : IRunner
    {
        private CommandRunningCallback m_Callback;

        private IRunnerFactory RunnerFactory;
        public SimpleCommandRunner()
        {
            
        }

        public void PassDependencies(IRunnerFactory RunnerFactory, LightScript TestFixture, CommandRunningCallback Callback, ValueWrapper<bool> ShouldCancelRun)
        {
            this.RunnerFactory = RunnerFactory;
            m_Callback = Callback;
        }

        public void Run(IRunnable runnable)
        {
            if (!RunnerFactory.DoesRunnerSupportType(this.GetType(), runnable.GetType()))
            {
                Logger.Log(LogType.Error, "This runner '" + this + "' is not compatible with this type: '" + runnable.GetType());
                return;
            }

            var command = runnable as Command;

            m_Callback?.Invoke(command);
            command.Run();
        }
    }
}
