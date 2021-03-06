﻿using System.ComponentModel;
using System.Linq;
using Robot.Abstractions;
using RobotRuntime.Scripts;

namespace RobotEditor.PropertyUtils
{
    /// <summary>
    /// This will add a selection dropdown for Asset in property view
    /// </summary>
    public class ScriptGUIDStringConverter : StringConverter
    {
        private static IAssetManager AssetManager { get; set; }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(AssetManager.Assets.Where(a => a.HoldsTypeOf(typeof(Script))).Select(a => a.Name).ToList());
        }
    }
}
