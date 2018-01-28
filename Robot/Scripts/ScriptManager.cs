﻿using Robot.Abstractions;
using Robot.Scripts;
using RobotRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Robot
{
    public class ScriptManager : IEnumerable<Script>, IScriptManager
    {
        private readonly IList<Script> m_LoadedScripts;
        public IReadOnlyList<Script> LoadedScripts { get { return m_LoadedScripts.ToList().AsReadOnly(); } }

        private Script m_ActiveScript;
        public Script ActiveScript
        {
            set
            {
                if (m_ActiveScript != value)
                    ActiveScriptChanged?.Invoke(m_ActiveScript, value);

                m_ActiveScript = value;
            }
            get { return m_ActiveScript; }
        }

        public event Action<Script, Script> ActiveScriptChanged;
        public event Action<Script> ScriptSaved;

        public event Action<Script> ScriptLoaded;
        public event Action<Script> ScriptModified;
        public event Action<int> ScriptRemoved;
        public event Action ScriptPositioningChanged;

        public event Action<Script, Command, Command> CommandAddedToScript;
        public event Action<Script, Command, Command, int> CommandInsertedInScript;
        public event Action<Script, Command, int> CommandRemovedFromScript;
        public event Action<Script, Command, Command> CommandModifiedOnScript;

        private IAssetManager AssetManager;
        public ScriptManager(IAssetManager AssetManager)
        {
            this.AssetManager = AssetManager;
            m_LoadedScripts = new List<Script>();
        }

        public void InvokeCommandAddedToScript(Script script, Command parentCommand, Command command)
        {
            CommandAddedToScript?.Invoke(script, parentCommand, command);
        }

        public void InvokeCommandInsertedInScript(Script script, Command parentCommand, Command command, int index)
        {
            CommandInsertedInScript?.Invoke(script, parentCommand, command, index);
        }

        public void InvokeCommandRemovedFromScript(Script script, Command parentCommand, int index)
        {
            CommandRemovedFromScript?.Invoke(script, parentCommand, index);
        }

        public void InvokeCommandModifiedOnScript(Script script, Command oldCommand, Command newCommand)
        {
            CommandModifiedOnScript?.Invoke(script, oldCommand, newCommand);
        }

        public Script NewScript(Script clone = null)
        {
            Script script;

            if (clone == null)
                script = new Script();
            else
                script = (Script)clone.Clone();

            script.ScriptManager = this;

            m_LoadedScripts.Add(script);
            script.IsDirty = true;

            MakeSureActiveScriptExist();
            ScriptLoaded?.Invoke(script);
            return script;
        }

        public void RemoveScript(Script script)
        {
            var position = m_LoadedScripts.IndexOf(script);

            m_LoadedScripts.Remove(script);

            MakeSureActiveScriptExist();

            ScriptRemoved?.Invoke(position);
        }

        public void RemoveScript(int position)
        {
            m_LoadedScripts.RemoveAt(position);

            MakeSureActiveScriptExist();
            ScriptRemoved?.Invoke(position);
        }

        public Script LoadScript(string path)
        {
            var asset = AssetManager.GetAsset(path);
            if (asset == null)
                throw new ArgumentException("No such asset at path: " + path);

            // if hierarchy contains empty untitled script, remove it
            if (m_LoadedScripts.Count == 1 && m_LoadedScripts[0].Name == Script.DefaultScriptName && m_LoadedScripts[0].Commands.Count() == 0)
                RemoveScript(0);

            Script newScript = asset.Importer.ReloadAsset<Script>();
            newScript.ScriptManager = this;
            newScript.Path = asset.Path;

            // If script was already loaded, reload it to last saved state
            var oldScript = m_LoadedScripts.FirstOrDefault(s => s.Path.Equals(path));
            if (oldScript != default(Script))
            {
                // Reload Script
                var index = m_LoadedScripts.IndexOf(oldScript);
                m_LoadedScripts[index] = newScript;
                MakeSureActiveScriptExist();
                ScriptModified?.Invoke(newScript);
            }
            else
            {
                // Load New Script
                m_LoadedScripts.Add(newScript);
                MakeSureActiveScriptExist();
                ScriptLoaded?.Invoke(newScript);
            }

            System.Diagnostics.Debug.WriteLine("Script loaded: " + path);
            return newScript;
        }

        public void SaveScript(Script script, string path)
        {
            AssetManager.CreateAsset(script, path);
            script.Path = Commons.GetProjectRelativePath(path);

            ScriptSaved?.Invoke(script);
            Console.WriteLine("Script saved: " + path);
        }

        public void MoveCommandAfter(Command source, Command after, int scriptIndex, int destinationScriptIndex = -1) // script indices could be removed
        {
            if (scriptIndex == destinationScriptIndex || destinationScriptIndex == -1) // Same script
            {
                var script = m_LoadedScripts[scriptIndex];
                script.MoveCommandAfter(source, after);
            }
            else // Move between two different scripts
            {
                var destScript = m_LoadedScripts[destinationScriptIndex];
                var destParentNode = m_LoadedScripts[destinationScriptIndex].Commands.GetNodeFromValue(after).parent;
                var sourceNode = m_LoadedScripts[scriptIndex].Commands.GetNodeFromValue(source);

                m_LoadedScripts[scriptIndex].RemoveCommand(source);

                destParentNode.Join(sourceNode);
                destScript.Commands.MoveAfter(source, after);

                CommandInsertedInScript?.Invoke(destScript, destParentNode.value, source, source.GetIndex(this));

                destScript.IsDirty = true;
            }

            m_LoadedScripts[scriptIndex].IsDirty = true;
        }

        public void MoveCommandBefore(Command source, Command before, int scriptIndex, int destinationScriptIndex = -1) // script indices could be removed
        {
            if (scriptIndex == destinationScriptIndex || destinationScriptIndex == -1) // Same script
            {
                var script = m_LoadedScripts[scriptIndex];
                script.MoveCommandBefore(source, before);
            }
            else // Move between two different scripts
            {
                var destScript = m_LoadedScripts[destinationScriptIndex];
                var destParentNode = m_LoadedScripts[destinationScriptIndex].Commands.GetNodeFromValue(before).parent;
                var sourceNode = m_LoadedScripts[scriptIndex].Commands.GetNodeFromValue(source);

                m_LoadedScripts[scriptIndex].RemoveCommand(source);

                destParentNode.Join(sourceNode);
                destScript.Commands.MoveBefore(source, before);

                CommandInsertedInScript?.Invoke(destScript, destParentNode.value, source, source.GetIndex(this));

                destScript.IsDirty = true;
            }

            m_LoadedScripts[scriptIndex].IsDirty = true;
        }

        public void MoveScriptAfter(int index, int after)
        {
            m_LoadedScripts.MoveAfter(index, after);
            ScriptPositioningChanged?.Invoke();
        }

        public void MoveScriptBefore(int index, int before)
        {
            m_LoadedScripts.MoveBefore(index, before);
            ScriptPositioningChanged?.Invoke();
        }

        public Script GetScriptFromCommand(Command command)
        {
            return LoadedScripts.FirstOrDefault((s) => s.Commands.GetAllNodes().Select(n => n.value).Contains(command));
        }

        private void MakeSureActiveScriptExist()
        {
            if (!m_LoadedScripts.Contains(ActiveScript) || m_LoadedScripts.Count == 0)
                ActiveScript = null;

            if (m_LoadedScripts.Count == 1)
                ActiveScript = m_LoadedScripts[0];

            if (ActiveScript == null && m_LoadedScripts.Count > 0)
                ActiveScript = m_LoadedScripts[0];
        }

        public IEnumerator<Script> GetEnumerator()
        {
            return m_LoadedScripts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_LoadedScripts.GetEnumerator();
        }
    }
}
