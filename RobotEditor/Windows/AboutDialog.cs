using RobotEditor;
using System;
using System.Windows.Forms;

namespace RobotEditor
{
    public partial class AboutDialog : Form
    {
        public AboutDialog()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        private void AboutDialog_Load(object sender, EventArgs e)
        {
            labelAppVersion.Text = typeof(MainForm).Assembly.GetName().Version.ToString();
        }
    }
}