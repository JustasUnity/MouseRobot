using Robot.Settings;
using System;
using System.ComponentModel;
using System.Windows.Forms;
using RobotEditor.Utils;
using RobotRuntime.Settings;

namespace RobotEditor.Settings
{
    [Serializable]
    public class CompilerProperties : BaseProperties
    {
        [NonSerialized]
        private CompilerSettings m_Settings;

        [Browsable(false)]
        public override string Title { get { return "Recording Settings"; } }

        public CompilerProperties(BaseSettings settings)
        {
            this.m_Settings = (CompilerSettings)settings;
        }

        public override void HideProperties(ref DynamicTypeDescriptor dt)
        {

        }
    }
}
