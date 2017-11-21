﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using RobotRuntime.Perf;
using System;
using Robot.Utils;
using RobotRuntime.Utils;

namespace Tests
{
    [TestClass]
    public class LinkedTreeTests
    {
        [TestMethod]
        public void LinkedTree_GetChild_ReturnsCorrectChild()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);

            Assert.AreEqual(0, tree.GetChild(0).value);
            Assert.AreEqual(1, tree.GetChild(1).value);
            Assert.AreEqual(2, tree.GetChild(2).value);
        }

        [TestMethod]
        public void LinkedTree_InsertChild_InsertsInCorrectPositions()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(1);
            tree.AddChild(3);
            tree.AddChild(4);

            tree.Insert(0, 0);
            tree.Insert(2, 2);
            tree.Insert(5, 5);

            Assert.AreEqual(0, tree.GetChild(0).value);
            Assert.AreEqual(1, tree.GetChild(1).value);
            Assert.AreEqual(2, tree.GetChild(2).value);
            Assert.AreEqual(3, tree.GetChild(3).value);
            Assert.AreEqual(4, tree.GetChild(4).value);
            Assert.AreEqual(5, tree.GetChild(5).value);
        }

        [TestMethod]
        public void LinkedTree_IndexOf_InsertsInCorrectPositions()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);

            Assert.AreEqual(0, tree.IndexOf(0));
            Assert.AreEqual(1, tree.IndexOf(1));
            Assert.AreEqual(2, tree.IndexOf(2));
        }

        [TestMethod]
        public void LinkedTree_MoveAfter_MiddleWorksFine()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);

            tree.MoveAfter(0, 1);

            Assert.AreEqual(1, tree.GetChild(0).value);
            Assert.AreEqual(0, tree.GetChild(1).value);
            Assert.AreEqual(2, tree.GetChild(2).value);
        }

        [TestMethod]
        public void LinkedTree_MoveAfter_LastWorksFine()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);

            tree.MoveAfter(0, 2);

            Assert.AreEqual(1, tree.GetChild(0).value);
            Assert.AreEqual(2, tree.GetChild(1).value);
            Assert.AreEqual(0, tree.GetChild(2).value);
        }

        [TestMethod]
        public void LinkedTree_MoveAfter_FirstWorksFine()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);
            tree.AddChild(3);

            tree.MoveAfter(2, 0);

            Assert.AreEqual(0, tree.GetChild(0).value);
            Assert.AreEqual(2, tree.GetChild(1).value);
            Assert.AreEqual(1, tree.GetChild(2).value);
            Assert.AreEqual(3, tree.GetChild(3).value);
        }

        [TestMethod]
        public void LinkedTree_MoveBefore_MiddleWorksFine()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);

            tree.MoveBefore(2, 1);

            Assert.AreEqual(0, tree.GetChild(0).value);
            Assert.AreEqual(2, tree.GetChild(1).value);
            Assert.AreEqual(1, tree.GetChild(2).value);
        }

        [TestMethod]
        public void LinkedTree_MoveBefore_LastWorksFine()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);

            tree.MoveBefore(0, 2);

            Assert.AreEqual(1, tree.GetChild(0).value);
            Assert.AreEqual(0, tree.GetChild(1).value);
            Assert.AreEqual(2, tree.GetChild(2).value);
        }

        [TestMethod]
        public void LinkedTree_MoveBefore_FirstWorksFine()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);
            tree.AddChild(3);

            tree.MoveBefore(2, 0);

            Assert.AreEqual(2, tree.GetChild(0).value);
            Assert.AreEqual(0, tree.GetChild(1).value);
            Assert.AreEqual(1, tree.GetChild(2).value);
            Assert.AreEqual(3, tree.GetChild(3).value);
        }

        [TestMethod]
        public void LinkedTree_Clone_MakesDeepCopy()
        {
            var tree = new TreeNode<int>();

            tree.AddChild(0);
            tree.AddChild(1);
            tree.AddChild(2);

            var newTree = (TreeNode<int>)tree.Clone();

            tree.GetChild(0).value = 5;
            tree.RemoveAt(1);

            Assert.AreEqual(0, newTree.GetChild(0).value);
            Assert.AreEqual(1, newTree.GetChild(1).value);
            Assert.AreEqual(2, newTree.GetChild(2).value);
        }
    }
}