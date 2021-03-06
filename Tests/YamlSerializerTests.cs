﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Robot.Scripts;
using RobotRuntime;
using RobotRuntime.Commands;
using RobotRuntime.IO;
using RobotRuntime.Scripts;
using RobotRuntime.Tests;
using RobotRuntime.Utils;
using RobotRuntime.Utils.Win32;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tests
{
    [TestClass]
    public class YamlSerializerTests
    {
        private const short level = 2;
        private const string PropertyName = "SomeInt";
        private const int PropertyValue = 157;
        private const string separator = ": ";

        private string YamlLine { get { return YamlObject.GetIndentation(level) + PropertyName + separator + PropertyValue; } }

        [TestMethod]
        public void YamlObject_ProducesCorrect_YamlString()
        {
            var obj = new YamlObject(level, PropertyName, PropertyValue);
            Assert.AreEqual(YamlLine, obj.ToString(), "Yaml Lines should be identical");
        }

        [TestMethod]
        public void YamlString_ProducesCorrect_YamlObject()
        {
            var obj = new YamlObject(YamlLine);

            Assert.AreEqual(level, obj.level, "Level missmatched");
            Assert.AreEqual(PropertyName, obj.property, "Property name missmatched");
            Assert.AreEqual(PropertyValue.ToString(), obj.value, "Property value missmatched");
        }

        [TestMethod]
        public void CommandWith_Enumerations_AreReadFine()
        {
            var command = new CommandPress(5, 5, false, MouseButton.Right);
            var yamlObj = YamlCommandIO.Serialize(command, 0);

            var newCommand = YamlCommandIO.Deserialize(yamlObj) as CommandPress;

            Assert.AreEqual(command.MouseButton, newCommand.MouseButton, "MouseButton property did not match on command");
        }


        private readonly static Guid guid = new Guid("12345678-9abc-def0-1234-567890123456");
        private readonly Command command = new CommandPress(50, 70, false, MouseButton.Left, guid);
        private const string serializedCommand = @"CommandPress: 
  <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
  <X>k__BackingField: 50
  <Y>k__BackingField: 70
  <DontMove>k__BackingField: False
  <MouseButton>k__BackingField: Left";

        [TestMethod]
        public void Command_ProducesCorrect_YamlString()
        {
            var yamlObj = YamlCommandIO.Serialize(command, 0);
            var yamlString = YamlSerializer.SerializeYamlTree(yamlObj);
            Assert.AreEqual(serializedCommand, yamlString, "Strings missmatched.");
        }

        [TestMethod]
        public void Command_ProducesCorrect_YamlObject()
        {
            var yamlObj = YamlCommandIO.Serialize(command, 0);

            var props = yamlObj.ToArray();
            Assert.AreEqual("CommandPress", yamlObj.value.property, "Command object had incorrect command type indicator.");
            Assert.AreEqual(5, props.Length, "Command object should have 3 properties.");
        }

        [TestMethod]
        public void YamlObject_ProducesCorrect_Command()
        {
            var yamlObj = YamlCommandIO.Serialize(command, 0);
            var newCommand = YamlCommandIO.Deserialize(yamlObj);
            Assert.AreEqual(command.ToString(), newCommand.ToString(), "Command strings should be equal.");
        }

        [TestMethod]
        public void YamlString_ProducesCorrect_CommandYamlObject()
        {
            var tree = YamlSerializer.DeserializeYamlTree(serializedCommand);
            var props = tree.ToArray();

            Assert.AreEqual("CommandPress", tree.value.property, "Command object had incorrect command type indicator.");
            Assert.AreEqual(5, props.Length, "Command object should have 5 properties.");
        }


        private Script Script
        {
            get
            {
                var s = new Script(guid);
                s.Name = "TestName";
                var imageCommand = new CommandForImage(new Guid(), 1850, guid);
                s.AddCommand(imageCommand);
                s.AddCommand(new CommandPress(55, 66, true, MouseButton.Left, guid), imageCommand);
                s.AddCommand(new CommandMove(10, 20, guid));
                return s;
            }
        }
        private const string serializedScript = @"LightScript: 
  <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
  <Name>k__BackingField: TestName
  CommandForImage: 
    <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
    <Asset>k__BackingField: 00000000-0000-0000-0000-000000000000
    <Timeout>k__BackingField: 1850
    CommandPress: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <X>k__BackingField: 55
      <Y>k__BackingField: 66
      <DontMove>k__BackingField: True
      <MouseButton>k__BackingField: Left
  CommandMove: 
    <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
    <X>k__BackingField: 10
    <Y>k__BackingField: 20";

        [TestMethod]
        public void Script_ProducesCorrect_YamlString()
        {
            var yamlObj = YamlScriptIO.Serialize(Script.ToLightScript());
            var yamlString = YamlSerializer.SerializeYamlTree(yamlObj);

            // Needed to ignore randomly generated guids
            var expected = Regex.Replace(serializedScript, RegexExpression.Guid, new Guid().ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var actual = Regex.Replace(yamlString, RegexExpression.Guid, new Guid().ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);

            StringAssert.Contains(actual, expected, "Strings missmatched.");
        }

        [TestMethod]
        public void Script_ProducesCorrect_YamlObj()
        {
            var yamlObj = YamlScriptIO.Serialize(Script.ToLightScript());
            var children = yamlObj.ToArray();
            Assert.AreEqual(4, children.Length, "only two root commands should be in the script and a name.");

            Assert.AreEqual(guid.ToString(), children[0].value.value, "Guid value was incorrect");
            Assert.AreEqual("TestName", children[1].value.value, "Test name value was incorrect");
            Assert.AreEqual(4, children[2].ToArray().Length, "Image command has also three childs, timeout CommandPress, and two guids");
            Assert.AreEqual(3, children[3].ToArray().Length, "Command move has also two childs, X and Y and guid.");

            var commandPress = children[2].ToArray()[3];
            Assert.AreEqual("CommandPress", commandPress.value.property, "CommandPress value of YamlObject was incorrect");
            Assert.AreEqual(5, commandPress.ToArray().Length, "CommandPress has 5 childs, X Y DontMove, guid, mouse buttons");
        }

        [TestMethod]
        public void YamlString_ProducesCorrect_ScriptYamlObject()
        {
            var yamlObj = YamlSerializer.DeserializeYamlTree(serializedScript);

            var children = yamlObj.ToArray();
            Assert.AreEqual(4, children.Length, "only two root commands should be in the script and a name.");

            Assert.AreEqual(guid.ToString(), children[0].value.value, "Guid value was incorrect");
            Assert.AreEqual("TestName", children[1].value.value, "Test name value was incorrect");
            Assert.AreEqual(4, children[2].ToArray().Length, "Image command has also three childs, timeout CommandPress, and two guids");
            Assert.AreEqual(3, children[3].ToArray().Length, "Command move has also two childs, X and Y and guid.");

            var commandPress = children[2].ToArray()[3];
            Assert.AreEqual("CommandPress", commandPress.value.property, "CommandPress value of YamlObject was incorrect");
            Assert.AreEqual(5, commandPress.ToArray().Length, "CommandPress has 5 childs, X Y DontMove, guid, mouse buttons");
        }

        [TestMethod]
        public void YamlObject_ProducesCorrect_Script()
        {
            var s = Script;
            var lightScript = s.ToLightScript();
            var yamlObj = YamlScriptIO.Serialize(s);
            var newScript = YamlScriptIO.Deserialize(yamlObj);

            Assert.AreEqual(lightScript.Commands.Count(), newScript.Commands.Count(), "Command count should be the same.");
        }


        private const string guidString = "2e3b9484-7b15-4511-91fb-c6f9f5aeb683";
        private readonly Guid guidObject = new Guid(guidString);
        private class ObjectWithGuid { public Guid Guid; }

        [TestMethod]
        public void GuidsInObject_ProducesCorrect_YamlObject()
        {
            var expectedYamlObject = new YamlObject(0, "Guid", guidObject);

            var obj = new ObjectWithGuid() { Guid = guidObject };
            var props = YamlSerializer.SerializeSimpleProperties(obj, 0);

            Assert.AreEqual(1, props.Count(), "Only one property should have been serialized.");
            Assert.AreEqual(expectedYamlObject.property, props.First().property, "Property names should be identical.");
            Assert.AreEqual(expectedYamlObject.value, props.First().value, "Guid values should be identical.");
        }

        [TestMethod]
        public void GuidsInYamlObject_ProducesCorrect_ClassObject()
        {
            var serializedYamlObject = new YamlObject(0, "Guid", guidObject);
            var objToWrite = new ObjectWithGuid();

            var tree = new TreeNode<YamlObject>();
            tree.AddChild(serializedYamlObject);

            YamlSerializer.DeserializeSimpleProperties(objToWrite, tree);

            Assert.AreEqual(guidObject, objToWrite.Guid, "Deserialized object should have correct guid value.");
        }

        private LightTestFixture Fixture
        {
            get
            {
                var f = new LightTestFixture(guid);
                f.Name = "TestName";
                f.Setup = Script;
                f.TearDown = Script;
                f.OneTimeSetup = Script;
                f.OneTimeTeardown = Script;
                f.Setup.Name = LightTestFixture.k_Setup;
                f.TearDown.Name = LightTestFixture.k_TearDown;
                f.OneTimeSetup.Name = LightTestFixture.k_OneTimeSetup;
                f.OneTimeTeardown.Name = LightTestFixture.k_OneTimeTeardown;
                f.Tests = new Script[] { Script }.ToList();
                return f;
            }
        }

        #region private const string serializedFixture = @"LightTestFixture: 
        private const string serializedFixture = @"LightTestFixture: 
  <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
  <Name>k__BackingField: TestName
  LightScript: 
    <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
    <Name>k__BackingField: Setup
    CommandForImage: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <Asset>k__BackingField: 00000000-0000-0000-0000-000000000000
      <Timeout>k__BackingField: 1850
      CommandPress: 
        <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
        <X>k__BackingField: 55
        <Y>k__BackingField: 66
        <DontMove>k__BackingField: True
        <MouseButton>k__BackingField: Left
    CommandMove: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <X>k__BackingField: 10
      <Y>k__BackingField: 20
  LightScript: 
    <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
    <Name>k__BackingField: TearDown
    CommandForImage: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <Asset>k__BackingField: 00000000-0000-0000-0000-000000000000
      <Timeout>k__BackingField: 1850
      CommandPress: 
        <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
        <X>k__BackingField: 55
        <Y>k__BackingField: 66
        <DontMove>k__BackingField: True
        <MouseButton>k__BackingField: Left
    CommandMove: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <X>k__BackingField: 10
      <Y>k__BackingField: 20
  LightScript: 
    <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
    <Name>k__BackingField: OneTimeSetup
    CommandForImage: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <Asset>k__BackingField: 00000000-0000-0000-0000-000000000000
      <Timeout>k__BackingField: 1850
      CommandPress: 
        <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
        <X>k__BackingField: 55
        <Y>k__BackingField: 66
        <DontMove>k__BackingField: True
        <MouseButton>k__BackingField: Left
    CommandMove: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <X>k__BackingField: 10
      <Y>k__BackingField: 20
  LightScript: 
    <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
    <Name>k__BackingField: OneTimeTeardown
    CommandForImage: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <Asset>k__BackingField: 00000000-0000-0000-0000-000000000000
      <Timeout>k__BackingField: 1850
      CommandPress: 
        <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
        <X>k__BackingField: 55
        <Y>k__BackingField: 66
        <DontMove>k__BackingField: True
        <MouseButton>k__BackingField: Left
    CommandMove: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <X>k__BackingField: 10
      <Y>k__BackingField: 20
  LightScript: 
    <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
    <Name>k__BackingField: TestName
    CommandForImage: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <Asset>k__BackingField: 00000000-0000-0000-0000-000000000000
      <Timeout>k__BackingField: 1850
      CommandPress: 
        <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
        <X>k__BackingField: 55
        <Y>k__BackingField: 66
        <DontMove>k__BackingField: True
        <MouseButton>k__BackingField: Left
    CommandMove: 
      <Guid>k__BackingField: 12345678-9abc-def0-1234-567890123456
      <X>k__BackingField: 10
      <Y>k__BackingField: 20";
        #endregion


        [TestMethod]
        public void Fixture_ProducesCorrect_YamlString()
        {
            var yamlObj = YamlTestFixtureIO.Serialize(Fixture);
            var yamlString = YamlSerializer.SerializeYamlTree(yamlObj);

            // Needed to ignore randomly generated guids
            var expected = Regex.Replace(serializedFixture, RegexExpression.Guid, new Guid().ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var actual = Regex.Replace(yamlString, RegexExpression.Guid, new Guid().ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);

            StringAssert.Contains(actual, expected, "Strings missmatched.");
        }

        [TestMethod]
        public void Fixture_ProducesCorrect_YamlObj()
        {
            var yamlObj = YamlTestFixtureIO.Serialize(Fixture);
            var children = yamlObj.ToArray();
            Assert.AreEqual(7, children.Length, "6 Childs. Name + 5 scripts");
            Assert.AreEqual(guid.ToString(), children[0].value.value, "Guids missmatched");
            Assert.AreEqual("TestName", children[1].value.value, "Names missmatched");
            Assert.AreEqual("LightScript", children[2].value.property, "Script type missmatched");
        }

        [TestMethod]
        public void YamlString_ProducesCorrect_FixtureYamlObject()
        {
            var yamlObj = YamlSerializer.DeserializeYamlTree(serializedFixture);

            var children = yamlObj.ToArray();
            Assert.AreEqual(7, children.Length, "7 Childs. Name + guid + 5 scripts");
            Assert.AreEqual(guid.ToString(), children[0].value.value, "Guids missmatched");
            Assert.AreEqual("TestName", children[1].value.value, "Names missmatched");
            Assert.AreEqual("LightScript", children[2].value.property, "Script type missmatched");
        }

        [TestMethod]
        public void YamlObject_ProducesCorrect_Fixture()
        {
            var f = Fixture;
            var yamlObj = YamlTestFixtureIO.Serialize(f);
            var newFixture = YamlTestFixtureIO.Deserialize(yamlObj);

            Assert.AreEqual(f.Name, newFixture.Name, "Fixture names missmatched.");
            Assert.AreEqual(f.Tests.Count(), newFixture.Tests.Count(), "Test count should be the same.");
            Assert.AreEqual(f.Setup.Name, newFixture.Setup.Name, "Setup names should be equal.");
            Assert.AreEqual(f.Setup.Commands.Count(), newFixture.Setup.Commands.Count(), "Setup command count should be the same.");

            var yamlObj2 = YamlTestFixtureIO.Serialize(newFixture);
            var text = YamlSerializer.SerializeYamlTree(yamlObj2);

            // Needed to ignore randomly generated guids
            var expected = Regex.Replace(text, RegexExpression.Guid, new Guid().ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var actual = Regex.Replace(serializedFixture, RegexExpression.Guid, new Guid().ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);

            StringAssert.Contains(actual, expected, "Strings missmatched.");
        }

        [TestMethod]
        public void YamlString_ProducesCorrect_Fixture()
        {
            var f = Fixture;
            var yamlObj = YamlTestFixtureIO.Serialize(f);
            var text = YamlSerializer.SerializeYamlTree(yamlObj);
            var newObj = YamlSerializer.DeserializeYamlTree(text);
            var newFixture = YamlTestFixtureIO.Deserialize(newObj);

            Assert.AreEqual(f.Name, newFixture.Name, "Fixture names missmatched.");
            Assert.AreEqual(f.Tests.Count(), newFixture.Tests.Count(), "Test count should be the same.");
            Assert.AreEqual(f.Setup.Name, newFixture.Setup.Name, "Setup names should be equal.");
            Assert.AreEqual(f.Setup.Commands.Count(), newFixture.Setup.Commands.Count(), "Setup command count should be the same.");
        }

        [TestInitialize]
        public void InitializeTest()
        {
            Logger.Instance = new FakeLogger();
        }
    }
}
