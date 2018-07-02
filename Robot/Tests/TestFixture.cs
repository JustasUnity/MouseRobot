﻿using Robot.Abstractions;
using Robot.Scripts;
using RobotRuntime.Abstractions;
using RobotRuntime.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RobotRuntime.Tests
{
    public class TestFixture : BaseScriptManager
    {
        public const string DefaultTestFixtureName = "New Fixture";
        public const string DefaultTestName = "New Test";

        public const string k_Setup = "Setup";
        public const string k_TearDown = "TearDown";
        public const string k_OneTimeSetup = "OneTimeSetup";
        public const string k_OneTimeTeardown = "OneTimeTeardown";

        public Script Setup { get { return GetScriptWithName(k_Setup); } set { ReplaceScriptWithName(k_Setup, value); } }
        public Script TearDown { get { return GetScriptWithName(k_TearDown); } set { ReplaceScriptWithName(k_TearDown, value); } }
        public Script OneTimeSetup { get { return GetScriptWithName(k_OneTimeSetup); } set { ReplaceScriptWithName(k_OneTimeSetup, value); } }
        public Script OneTimeTeardown { get { return GetScriptWithName(k_OneTimeTeardown); } set { ReplaceScriptWithName(k_OneTimeTeardown, value); } }

        public IList<Script> Tests { get { return GetAllTests(); } }
        public IList<Script> Hooks { get { return GetAllHooks(); } }

        public string Name { get; set; } = DefaultTestFixtureName;
        public string Path { get; set; } = "";

        private bool m_IsDirty;
        public bool IsDirty
        {
            set
            {
                if (m_IsDirty != value)
                    DirtyChanged?.Invoke(this);

                m_IsDirty = value;

                if (m_IsDirty == false)
                {
                    foreach (var s in LoadedScripts)
                        s.IsDirty = false;
                }
            }
            get
            {
                return LoadedScripts.Any(s => s.IsDirty) || m_IsDirty;
            }
        }

        public event Action<TestFixture> DirtyChanged;

        private ILogger Logger;
        public TestFixture(IAssetManager AssetManager, ICommandFactory CommandFactory, IProfiler Profiler, ILogger Logger) : base(CommandFactory, Profiler)
        {
            this.Logger = Logger;

            AddScript(new Script() { Name = k_Setup });
            AddScript(new Script() { Name = k_TearDown });
            AddScript(new Script() { Name = k_OneTimeSetup });
            AddScript(new Script() { Name = k_OneTimeTeardown });
        }

        public override Script NewScript(Script clone = null)
        {
            var s = base.NewScript(clone);
            s.Name = DefaultTestName;
            return s;
        }

        private Script GetScriptWithName(string name)
        {
            return LoadedScripts.FirstOrDefault(s => s.Name == name);
        }

        private void ReplaceScriptWithName(string name, Script value)
        {
            var s = GetScriptWithName(name);
            if (s == null)
                Logger.Logi(LogType.Error, "Tried to replace script value with name '" + name + "' but it was not found.");

            var addedScriptIndex = LoadedScripts.Count - 1;
            var instertIntoIndex = LoadedScripts.IndexOf(s);
            RemoveScript(s);
            AddScript(s);
            MoveScriptBefore(addedScriptIndex, instertIntoIndex);
        }

        private IList<Script> GetAllTests()
        {
            return LoadedScripts.Where(s => s.Name != k_Setup && s.Name != k_TearDown && s.Name != k_OneTimeSetup && s.Name != k_OneTimeTeardown).ToList();
        }

        private IList<Script> GetAllHooks()
        {
            return LoadedScripts.Where(s => s.Name == k_Setup || s.Name == k_TearDown || s.Name == k_OneTimeSetup || s.Name == k_OneTimeTeardown).ToList();
        }

        // Inheritence

        public LightTestFixture ToLightTestFixture()
        {
            var t = new LightTestFixture();
            t.Tests = Tests;
            t.Setup = Setup;
            t.OneTimeSetup = OneTimeSetup;
            t.TearDown = TearDown;
            t.OneTimeTeardown = OneTimeTeardown;
            t.Name = Name;
            return t;
        }

        public TestFixture ApplyLightScriptValues(LightTestFixture t)
        {
            m_LoadedScripts.Clear();

            Name = t.Name;

            AddScript(t.Setup);
            AddScript(t.TearDown);
            AddScript(t.OneTimeSetup);
            AddScript(t.OneTimeTeardown);
            
            foreach (var test in t.Tests)
                AddScript(test);

            return this;
        }

        public override string ToString()
        {
            return IsDirty ? Name + "*" : Name;
        }
    }
}