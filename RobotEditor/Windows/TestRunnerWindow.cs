﻿#define ENABLE_UI_TESTING

using System;
using RobotEditor.Abstractions;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using RobotRuntime;
using Robot;
using RobotEditor.Scripts;
using BrightIdeasSoftware;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Robot.Scripts;
using RobotRuntime.Utils;
using System.Drawing;
using Robot.Abstractions;
using RobotRuntime.Abstractions;

namespace RobotEditor
{
    public partial class TestRunnerWindow : DockContent
    {
        public event Action<Command> OnCommandSelected;
        private List<HierarchyNode> m_Nodes = new List<HierarchyNode>();

        private HierarchyNode m_HighlightedNode;

        private IScriptManager ScriptManager;
        private ITestRunner TestRunner;
        private IAssetManager AssetManager;
        private IHierarchyNodeStringConverter HierarchyNodeStringConverter;
        public TestRunnerWindow(IScriptManager ScriptManager, ITestRunner TestRunner, IAssetManager AssetManager, IHierarchyNodeStringConverter HierarchyNodeStringConverter)
        {
            this.ScriptManager = ScriptManager;
            this.TestRunner = TestRunner;
            this.AssetManager = AssetManager;
            this.HierarchyNodeStringConverter = HierarchyNodeStringConverter;

            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;

            treeListView.Font = Fonts.Default;

            ScriptManager.CommandAddedToScript += OnCommandAddedToScript;
            ScriptManager.CommandRemovedFromScript += OnCommandRemovedFromScript;
            ScriptManager.CommandModifiedOnScript += OnCommandModifiedOnScript;
            ScriptManager.CommandInsertedInScript += OnCommandInsertedInScript;

            ScriptManager.ScriptLoaded += OnScriptLoaded;
            ScriptManager.ScriptModified += OnScriptModified;
            ScriptManager.ScriptRemoved += OnScriptRemoved;
            ScriptManager.ScriptPositioningChanged += OnScriptPositioningChanged;

            TestRunner.Finished += OnScriptsFinishedRunning;
            TestRunner.RunningCommandCallback += OnCommandRunning;

            CreateColumns();
            UpdateHierarchy();
        }

        private void CreateColumns()
        {
            treeListView.CanExpandGetter = x => (x as HierarchyNode).Children.Count > 0;
            treeListView.ChildrenGetter = x => (x as HierarchyNode).Children;

            var nameColumn = new OLVColumn("Name", "Name");
            nameColumn.AspectGetter = x => HierarchyNodeStringConverter.ToString(x as HierarchyNode);

            nameColumn.ImageGetter += delegate (object x)
            {
                var imageListIndex = (x as HierarchyNode).Level == 0 ? 0 : 1;
                return imageListIndex;
            };

            treeListView.UseCellFormatEvents = true;
            treeListView.FormatCell += UpdateFontsTreeListView;

            treeListView.IsSimpleDragSource = true;
            treeListView.IsSimpleDropSink = true;

            nameColumn.Width = treeListView.Width;
            treeListView.Columns.Add(nameColumn);
        }

        private void UpdateFontsTreeListView(object sender, FormatCellEventArgs e)
        {
            var node = e.Model as HierarchyNode;
            if (node == null)
                return;

            if (node.Script != null)
            {
                if (node.Script == ScriptManager.ActiveScript && node.Script.IsDirty)
                    e.SubItem.Font = Fonts.ActiveAndDirtyScript;//.AddFont(Fonts.ActiveScript);
                else if (node.Script == ScriptManager.ActiveScript)
                    e.SubItem.Font = Fonts.ActiveScript;
                else if (node.Script.IsDirty)
                    e.SubItem.Font = Fonts.DirtyScript;//.AddFont(Fonts.DirtyScript);
                else
                    e.SubItem.Font = Fonts.Default;
            }

            if (node.Command != null)
            {
                if (node == m_HighlightedNode)
                    e.SubItem.BackColor = SystemColors.Highlight;
            }
        }

        private void UpdateHierarchy()
        {
            m_Nodes.Clear();

            foreach (var s in ScriptManager.LoadedScripts)
                m_Nodes.Add(new HierarchyNode(s));

            RefreshTreeListView();
            treeListView.ExpandAll();
        }

        private void RefreshTreeListView()
        {
            treeListView.Roots = m_Nodes;

            for (int i = 0; i < treeListView.Items.Count; ++i)
            {
                treeListView.Items[i].ImageIndex = 0;
            }
            treeListView.Refresh();
        }


        #region ScriptManager Callbacks

        private void OnScriptLoaded(Script script)
        {
            var node = new HierarchyNode(script);
            m_Nodes.Add(node);
            RefreshTreeListView();

            treeListView.SelectedObject = node;
            treeListView.Expand(node);

            ASSERT_TreeViewIsTheSameAsInScriptManager();
        }

        private void OnScriptModified(Script script)
        {
            var node = new HierarchyNode(script);
            var index = script.Index;
            m_Nodes[index] = node;
            RefreshTreeListView();

            ASSERT_TreeViewIsTheSameAsInScriptManager();
        }

        private void OnScriptRemoved(int index)
        {
            var oldSelectedObject = treeListView.SelectedObject;

            m_Nodes.RemoveAt(index);
            RefreshTreeListView();

            if (treeListView.SelectedObject != oldSelectedObject)
                OnCommandSelected?.Invoke(null);

            ASSERT_TreeViewIsTheSameAsInScriptManager();
        }

        private void OnScriptPositioningChanged()
        {
            foreach (var script in ScriptManager.LoadedScripts)
            {
                var index = m_Nodes.FindIndex(n => n.Script == script);
                m_Nodes.MoveBefore(index, script.Index);
            }

            RefreshTreeListView();
            ASSERT_TreeViewIsTheSameAsInScriptManager();
        }

        private void OnCommandAddedToScript(Script script, Command parentCommand, Command command)
        {
            var parentNode = script.Commands.GetNodeFromValue(command).parent;
            System.Diagnostics.Debug.Assert(parentNode.value == parentCommand, "parentCommand and parentNode missmatched");

            var scriptNode = m_Nodes.FirstOrDefault(n => n.Script == script);

            var parentHierarchyNode = parentCommand == null ? scriptNode : scriptNode.GetNodeFromValue(parentNode.value);
            AddCommandToParentRecursive(script, command, parentHierarchyNode);

            if (scriptNode.Children.Count == 1)
                treeListView.Expand(scriptNode);

            RefreshTreeListView();
        }

        private static HierarchyNode AddCommandToParentRecursive(Script script, Command command, HierarchyNode parentHierarchyNode, int pos = -1)
        {
            var nodeToAdd = new HierarchyNode(command, parentHierarchyNode);

            if (pos == -1)
                parentHierarchyNode.Children.Add(nodeToAdd);
            else
                parentHierarchyNode.Children.Insert(pos, nodeToAdd);

            var commandNode = script.Commands.GetNodeFromValue(command);
            foreach (var childNode in commandNode)
                AddCommandToParentRecursive(script, childNode.value, nodeToAdd);

            return nodeToAdd;
        }

        private void OnCommandRemovedFromScript(Script script, Command parentCommand, int commandIndex)
        {
            var scriptNode = m_Nodes.FirstOrDefault(node => node.Script == script);
            var parentNode = parentCommand == null ? scriptNode : scriptNode.GetNodeFromValue(parentCommand);

            parentNode.Children.RemoveAt(commandIndex);
            RefreshTreeListView();
        }

        private void OnCommandModifiedOnScript(Script script, Command oldCommand, Command newCommand)
        {
            var scriptNode = m_Nodes.FirstOrDefault(node => node.Script == script);
            var commandNode = scriptNode.GetNodeFromValue(oldCommand);

            commandNode.Update(newCommand);
            RefreshTreeListView();
        }

        // Will not work with multi dragging
        private void OnCommandInsertedInScript(Script script, Command parentCommand, Command command, int pos)
        {
            var scriptNode = m_Nodes.FirstOrDefault(n => n.Script == script);
            var parentNode = parentCommand == null ? scriptNode : scriptNode.GetNodeFromValue(parentCommand);

            var node = AddCommandToParentRecursive(script, command, parentNode, pos);

            RefreshTreeListView();
            treeListView.SelectedObject = node;
        }

        #endregion

        #region Context Menu Items
        private void setActiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNode = treeListView.SelectedObject as HierarchyNode;
            if (selectedNode == null || selectedNode.Script == null)
                return;

            ScriptManager.ActiveScript = selectedNode.Script;
            RefreshTreeListView();
        }

        public void newScriptToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ScriptManager.NewScript();
            RefreshTreeListView();

            ASSERT_TreeViewIsTheSameAsInScriptManager();
        }

        private void showInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNode = treeListView.SelectedObject as HierarchyNode;
            if (selectedNode == null || selectedNode.Script == null)
                return;

            Process.Start("explorer.exe", "/select, " + selectedNode.Script.Path);
        }

        public void duplicateToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var selectedNode = treeListView.SelectedObject as HierarchyNode;
            if (selectedNode == null)
                return;

            if (selectedNode.Script != null)
            {
                ScriptManager.NewScript(selectedNode.Script);
                ScriptManager.MoveScriptAfter(ScriptManager.LoadedScripts.Count - 1, selectedNode.Script.Index);
            }
            else if (selectedNode.Command != null)
            {
                var script = selectedNode.TopLevelScriptNode.Script;
                var node = script.Commands.GetNodeFromValue(selectedNode.Command);
                var clone = (TreeNode<Command>)node.Clone();

                script.AddCommandNode(clone, node.parent.value);
                script.MoveCommandAfter(clone.value, selectedNode.Command);
                //selectedNode.TopLevelScriptNode.Script.InsertCommandAfter(clone, selectedNode.Command);
            }

            RefreshTreeListView();
            treeListView.Focus();

            ASSERT_TreeViewIsTheSameAsInScriptManager();
        }

        public void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var selectedNode = treeListView.SelectedObject as HierarchyNode;
            if (selectedNode == null)
                return;

            if (selectedNode.Script != null)
                ScriptManager.RemoveScript(selectedNode.Script);
            else if (selectedNode.Command != null)
                ScriptManager.GetScriptFromCommand(selectedNode.Command).RemoveCommand(selectedNode.Command);

            RefreshTreeListView();

            ASSERT_TreeViewIsTheSameAsInScriptManager();
        }
        #endregion

        #region Menu Items (save scripts from MainForm)
        public void SaveAllScripts()
        {
            foreach (var script in ScriptManager)
            {
                if (!script.IsDirty)
                    continue;

                if (script.Path != "")
                    ScriptManager.SaveScript(script, script.Path);
                else
                    SaveSelectedScriptWithDialog(script, updateUI: false);
            }

            RefreshTreeListView();
        }

        public void SaveSelectedScriptWithDialog(Script script, bool updateUI = true)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.InitialDirectory = Environment.CurrentDirectory + "\\" + Paths.ScriptFolder;
            saveDialog.Filter = string.Format("Mouse Robot File (*.{0})|*.{0}", FileExtensions.Script);
            saveDialog.Title = "Select a path for script to save.";
            saveDialog.FileName = script.Name + FileExtensions.ScriptD;
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                ScriptManager.SaveScript(script, saveDialog.FileName);
                if (updateUI)
                    RefreshTreeListView();
            }
        }
        #endregion

        #region Drag & Drop

        private void treeListView_ModelCanDrop(object sender, ModelDropEventArgs e)
        {
            var targetNode = e.TargetModel as HierarchyNode;
            var sourceNode = e.SourceModels[0] as HierarchyNode;

            e.DropSink.CanDropBetween = true;

            if (targetNode == null || sourceNode == null ||
                targetNode.Script == null && sourceNode.Command == null ||
                targetNode.Script != null && sourceNode.Script != null && e.DropTargetLocation == DropTargetLocation.Item)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.DropSink.CanDropOnItem = targetNode.Command.CanBeNested() || targetNode.Script != null;

            if (targetNode.Script != null && sourceNode.Command != null)
                e.DropSink.CanDropBetween = false;

            if (sourceNode.GetAllNodes().Contains(targetNode))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.Effect = DragDropEffects.Move;
        }

        private void treeListView_ModelDropped(object sender, ModelDropEventArgs e)
        {
            var targetNode = e.TargetModel as HierarchyNode;
            var sourceNode = e.SourceModels[0] as HierarchyNode;

            if (targetNode.Script != null && sourceNode.Script != null)
            {
                if (e.DropTargetLocation == DropTargetLocation.AboveItem)
                    ScriptManager.MoveScriptBefore(sourceNode.Script.Index, targetNode.Script.Index);
                if (e.DropTargetLocation == DropTargetLocation.BelowItem)
                    ScriptManager.MoveScriptAfter(sourceNode.Script.Index, targetNode.Script.Index);
            }

            if (targetNode.Command != null && sourceNode.Command != null)
            {
                var targetScript = ScriptManager.GetScriptFromCommand(targetNode.Command);
                var sourceScript = ScriptManager.GetScriptFromCommand(sourceNode.Command);

                if (e.DropTargetLocation == DropTargetLocation.AboveItem)
                    ScriptManager.MoveCommandBefore(sourceNode.Command, targetNode.Command, sourceScript.Index, targetScript.Index);
                if (e.DropTargetLocation == DropTargetLocation.BelowItem)
                    ScriptManager.MoveCommandAfter(sourceNode.Command, targetNode.Command, sourceScript.Index, targetScript.Index);

                if (e.DropTargetLocation == DropTargetLocation.Item && targetNode.Command.CanBeNested())
                {
                    var node = sourceScript.Commands.GetNodeFromValue(sourceNode.Command);
                    sourceScript.RemoveCommand(sourceNode.Command);
                    targetScript.AddCommandNode(node, targetNode.Command);
                }
            }

            if (targetNode.Script != null && sourceNode.Command != null)
            {
                var sourceScript = ScriptManager.GetScriptFromCommand(sourceNode.Command);

                var node = sourceScript.Commands.GetNodeFromValue(sourceNode.Command);
                sourceScript.RemoveCommand(sourceNode.Command);
                targetNode.Script.AddCommandNode(node);
            }
        }

        #endregion

        #region ScriptRunner Callbacks

        private void OnCommandRunning(Command command)
        {
            var script = ScriptManager.GetScriptFromCommand(command);
            if (script == null)
                return;

            var scriptNode = m_Nodes.FirstOrDefault(node => node.Script == script);
            if (scriptNode == null)
                return;

            var commandNode = scriptNode.GetNodeFromValue(command);
            m_HighlightedNode = commandNode;

            if (treeListView.Created)
                treeListView.Invoke(new Action(() => treeListView.Refresh()));
        }

        private void OnScriptsFinishedRunning()
        {
            m_HighlightedNode = null;

            if (treeListView.Created)
                treeListView.Invoke(new Action(() => treeListView.Refresh()));
        }

        #endregion

        #region ToolStrip Buttons

        private void ToolstripExpandAll_Click(object sender, EventArgs e)
        {
            treeListView.ExpandAll();
        }

        private void ToolstripExpandOne_Click(object sender, EventArgs e)
        {
            treeListView.CollapseAll();
            foreach (var node in m_Nodes)
                treeListView.Expand(node);
        }

        private void ToolstripCollapseAll_Click(object sender, EventArgs e)
        {
            treeListView.CollapseAll();
        }

        #endregion

        public ToolStrip ToolStrip { get { return toolStrip; } }

        private void treeListView_SelectionChanged(object sender, EventArgs e)
        {
            var node = treeListView.SelectedObject as HierarchyNode;

            if (node == null)
            {
                OnCommandSelected?.Invoke(null);
            }
            else if (node.Command != null)
            {
                OnCommandSelected?.Invoke(node.Command);
            }
        }

        private void ASSERT_TreeViewIsTheSameAsInScriptManager()
        {
#if ENABLE_UI_TESTING
            for (int i = 0; i < m_Nodes.Count; i++)
            {
                Debug.Assert(m_Nodes[i].Script == ScriptManager.LoadedScripts[i],
                    string.Format("Hierarchy script missmatch: {0}:{1}", i, m_Nodes[i].Value.ToString()));

                // Will not work in nested scenarios
                for (int j = 0; j < m_Nodes[i].Script.Commands.Count(); j++)
                {
                    Debug.Assert(m_Nodes[i].Children[j].Command == ScriptManager.LoadedScripts[i].Commands.GetChild(j).value,
                        string.Format("Hierarchy command missmatch: {0}:{1}, {2}:{3}",
                        i, m_Nodes[i].Value.ToString(), j, m_Nodes[i].Script.Commands.GetChild(j).value.ToString()));
                }
            }
#endif
        }
    }
}