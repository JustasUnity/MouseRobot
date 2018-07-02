﻿using System;
using RobotEditor.Abstractions;
using System.Windows.Forms;
using RobotRuntime;
using BrightIdeasSoftware;
using System.Collections.Generic;
using Robot.Abstractions;
using RobotEditor.Hierarchy;
using RobotRuntime.Scripts;

namespace RobotEditor.Utils
{
    public class HierarchyUtils
    {
        public static void CreateColumns(TreeListView treeListView, IHierarchyNodeStringConverter HierarchyNodeStringConverter)
        {
            treeListView.CanExpandGetter = x => (x as HierarchyNode).Children.Count > 0;
            treeListView.ChildrenGetter = x => (x as HierarchyNode).Children;

            var nameColumn = new OLVColumn("Name", "Name");
            nameColumn.AspectGetter = x => HierarchyNodeStringConverter.ToString(x as HierarchyNode);

            nameColumn.ImageGetter += delegate (object x)
            {
                var imageListIndex = -1;
                var node = (HierarchyNode)x;
                imageListIndex = node.Script != null ? 0 : imageListIndex;
                imageListIndex = node.Command != null ? 1 : imageListIndex;
                return imageListIndex;
            };

            treeListView.UseCellFormatEvents = true;

            treeListView.IsSimpleDragSource = true;
            treeListView.IsSimpleDropSink = true;

            nameColumn.Width = treeListView.Width;
            treeListView.Columns.Add(nameColumn);
        }

        public static void OnNewUserCommandsAppeared(ICommandFactory CommandFactory, ContextMenuStrip contextMenuStrip, int createMenuItemIndex, Action<string> onItemClick)
        {
            var createMenuItem = (ToolStripMenuItem)contextMenuStrip.Items[createMenuItemIndex];

            createMenuItem.DropDownItems.Clear();
            foreach (var name in CommandFactory.CommandNames)
            {
                var item = new ToolStripMenuItem(name);
                item.Click += (sender, eventArgs) => onItemClick.Invoke(name);
                createMenuItem.DropDownItems.Add(item);
            }
        }

        public static void OnCommandAddedToScript(List<HierarchyNode> nodes, Script script, Command parentCommand, Command command)
        {
            var parentNode = script.Commands.GetNodeFromValue(command).parent;
            System.Diagnostics.Debug.Assert(parentNode.value == parentCommand, "parentCommand and parentNode missmatched");

            var scriptNode = nodes.FindRecursively(script);

            var parentHierarchyNode = parentCommand == null ? scriptNode : scriptNode.GetNodeFromValue(parentNode.value);
            AddCommandToParentRecursive(script, command, parentHierarchyNode);
        }

        public static HierarchyNode AddCommandToParentRecursive(Script script, Command command, HierarchyNode parentHierarchyNode, int pos = -1)
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

        public static void OnCommandRemovedFromScript(List<HierarchyNode> nodes, Script script, Command parentCommand, int commandIndex)
        {
            var scriptNode = nodes.FindRecursively(script);
            var parentNode = parentCommand == null ? scriptNode : scriptNode.GetNodeFromValue(parentCommand);

            parentNode.Children.RemoveAt(commandIndex);
        }

        public static void OnCommandModifiedOnScript(List<HierarchyNode> nodes, Script script, Command oldCommand, Command newCommand)
        {
            var scriptNode = nodes.FindRecursively(script);
            var commandNode = scriptNode.GetNodeFromValue(oldCommand);

            commandNode.Update(newCommand);
        }

        // Will not work with multi dragging
        public static HierarchyNode OnCommandInsertedInScript(List<HierarchyNode> nodes, Script script, Command parentCommand, Command command, int pos)
        {
            var scriptNode = nodes.FindRecursively(script);
            var parentNode = parentCommand == null ? scriptNode : scriptNode.GetNodeFromValue(parentCommand);

            return AddCommandToParentRecursive(script, command, parentNode, pos);
        }
    }
}