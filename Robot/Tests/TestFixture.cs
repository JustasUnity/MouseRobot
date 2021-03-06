﻿using Robot.Abstractions;
using Robot.Scripts;
using RobotRuntime.Abstractions;
using RobotRuntime.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RobotRuntime.Tests
{
    public class TestFixture : BaseScriptManager, ISimilar, IHaveGuid
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

        public Guid Guid { get; protected set; }

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
        public TestFixture(IAssetManager AssetManager, ICommandFactory CommandFactory, IProfiler Profiler, ILogger Logger)
            : base(CommandFactory, Profiler, Logger)
        {
            this.Logger = Logger;

            AddScript(new Script() { Name = k_Setup });
            AddScript(new Script() { Name = k_TearDown });
            AddScript(new Script() { Name = k_OneTimeSetup });
            AddScript(new Script() { Name = k_OneTimeTeardown });

            Guid = Guid.NewGuid();
        }

        public override Script NewScript(Script clone = null)
        {
            var s = base.NewScript(clone);
            s.Name = GetUniqueTestName(DefaultTestName);
            m_IsDirty = true;
            return s;
        }

        private string GetUniqueTestName(string name)
        {
            var newName = name;
            var i = 0;

            while (LoadedScripts.Any(script => script.Name == newName))
                newName = name + ++i;

            return newName;
        }

        public override void RemoveScript(Script script)
        {
            base.RemoveScript(script);
            m_IsDirty = true;
        }

        public override void RemoveScript(int position)
        {
            base.RemoveScript(position);
            m_IsDirty = true;
        }

        public override Script AddScript(Script script, bool removeScriptWithSamePath = false)
        {
            m_IsDirty = true;
            return base.AddScript(script, removeScriptWithSamePath);
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

        public bool Similar(object obj)
        {
            var f = obj as TestFixture;
            if (f == null)
                return false;

            if (f.Name != Name)
                return false;

            return LoadedScripts.SequenceEqual(f.LoadedScripts, new SimilarEqualityComparer());
        }

        /// <summary>
        /// Implemented explicitly so it has less visibility, since most systems should not regenerate guids by themself.
        /// As of this time, only scripts need to regenerate guids for commands (2018.08.15)
        /// </summary>
        void IHaveGuid.RegenerateGuid()
        {
            Guid = Guid.NewGuid();
        }

        public override int GetHashCode()
        {
            int hash = Name.GetHashCode();
            hash += LoadedScripts.Select(script => script.GetHashCode()).Sum();
            return hash;
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
            return new LightTestFixture(Guid)
            {
                Tests = Tests,
                Setup = Setup,
                OneTimeSetup = OneTimeSetup,
                TearDown = TearDown,
                OneTimeTeardown = OneTimeTeardown,
                Name = Name
            };
        }

        public TestFixture ApplyLightFixtureValues(LightTestFixture t)
        {
            m_LoadedScripts.Clear();

            Name = t.Name;
            Guid = t.Guid == default(Guid) ? Guid : t.Guid;
            
            AddScript(t.Setup);
            AddScript(t.TearDown);
            AddScript(t.OneTimeSetup);
            AddScript(t.OneTimeTeardown);
            
            foreach (var test in t.Tests)
                AddScript(test);

            m_IsDirty = false;

            return this;
        }

        public override string ToString()
        {
            return IsDirty ? Name + "*" : Name;
        }
    }
}
