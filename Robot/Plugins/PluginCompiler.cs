﻿using Robot.Abstractions;
using RobotRuntime;
using RobotRuntime.Abstractions;
using System;
using System.CodeDom.Compiler;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System.Threading;

namespace Robot.Plugins
{
    /// <summary>
    /// PluginCompiler lives in base robot assemblies. Its purpose is to compile files in project folder into user dlls.
    /// Used by PLuginManager
    /// </summary>
    public class PluginCompiler : IPluginCompiler
    {
        public CSharpCodeProvider CodeProvider { get; private set; } = new CSharpCodeProvider();
        public CompilerParameters CompilerParams { get; private set; } = new CompilerParameters();

        public event Action ScriptsRecompiled;

        private bool m_IsCompiling = false;
        private bool m_ShouldRecompile = false;
        private string[] m_TempSources;

        private IProfiler Profiler;
        public PluginCompiler(IProfiler Profiler)
        {
            this.Profiler = Profiler;
            
            CompilerParams.GenerateExecutable = false;
            CompilerParams.GenerateInMemory = false;

            CompilerParams.ReferencedAssemblies.Add("System.dll");
            CompilerParams.ReferencedAssemblies.Add("System.Drawing.dll");
            CompilerParams.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            //CompilerParams.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
        }

        public void AddReferencedAssemblies(params string[] paths)
        {
            CompilerParams.ReferencedAssemblies.AddRange(paths);
        }

        public void CompileCode(params string[] sources)
        {
            // If already compiling, save sources for future compilation
            if (m_IsCompiling)
            {
                m_ShouldRecompile = true;
                m_TempSources = sources;
                return;
            }

            m_IsCompiling = true;

            new Thread(new ThreadStart(() => CompileCodeSync(sources))).Start();
        }  

        private bool CompileCodeSync(string[] sources)
        {
            Profiler.Start("PluginCompiler_CompileCode");
            var results = CodeProvider.CompileAssemblyFromSource(CompilerParams, sources);
            Profiler.Stop("PluginCompiler_CompileCode");

            m_IsCompiling = false;

            // This might happen if scripts are modified before compilation is finished
            if (m_ShouldRecompile)
            {
                m_ShouldRecompile = false;
                CompileCode(m_TempSources);
                return false;
            }

            if (results.Errors.HasErrors)
            {
                foreach (CompilerError error in results.Errors)
                    Logger.Log(LogType.Error,
                        string.Format("({0}): {1}", error.ErrorNumber, error.ErrorText),
                        string.Format("at {0} {1} : {2}", error.FileName, error.Line, error.Column));

                Logger.Log(LogType.Error, "Scripts have compilation errors.");
                ScriptsRecompiled?.Invoke();
                return false;
            }
            else
            {
                ScriptsRecompiled?.Invoke();
                Logger.Log(LogType.Log, "Scripts successfully compiled.");
                return true;
            }

        }

        public void SetOutputPath(string customAssemblyPath)
        {
            CompilerParams.OutputAssembly = customAssemblyPath;
        }
    }
}
