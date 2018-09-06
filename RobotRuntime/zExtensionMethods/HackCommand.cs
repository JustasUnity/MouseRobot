using RobotRuntime.Abstractions;
using RobotRuntime.Commands;
using RobotRuntime.Execution;
using RobotRuntime.Tests;
using RobotRuntime.Utils;
using RobotRuntime.Utils.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace RobotRuntime
{

    [Serializable]
    [RunnerType(typeof(HackCommandRunner))]
    //[PropertyDesignerType("CustomCommandDesigner")] // Optional
    public class HackCommand : Command
    {
        // This is what will appear in dropdown in inspector under Command Type
        public override string Name { get { return "Hack Command"; } }
        public override bool CanBeNested { get { return true; } }

        public string PathToDll { get; set; } = "";
        public string Namespace { get; set; } = "Namespace1";
        public string ClassName { get; set; } = "Class1";
        public string MethodName { get; set; } = "Method1";

        // having an empty constructor is a must, will not work otherwise
        public HackCommand() { }
        public HackCommand(int SomeInt)
        {
        }

        public override void Run(TestData TestData)
        {
        }

        public override string ToString()
        {
            return "Hach: " + Namespace + "." + ClassName + "." + MethodName;
        }
    }

    public class HackCommandRunner : IRunner
    {
        private IFeatureDetectorFactory FeatureDetectorFactory;
        private IScreenStateThread ScreenStateThread;
        public HackCommandRunner(IFeatureDetectorFactory FeatureDetectorFactory, IScreenStateThread ScreenStateThread)
        {
            // Constructor actually can ask for other managers if needed, like IAssetDatabase etc.
            this.FeatureDetectorFactory = FeatureDetectorFactory;
            this.ScreenStateThread = ScreenStateThread;
        }

        public TestData TestData { set; get; }

        public void Run(IRunnable runnable)
        {
            var command = runnable as HackCommand;
            var node = TestData.TestFixture.Commands.FirstOrDefault(n => n.value == command);

            var screen = new Bitmap(ScreenStateThread.Width, ScreenStateThread.Height, PixelFormat.Format32bppArgb);
            RecognizeText.StartNewThreadForTextRecognition(screen);
            var rand = new Random();

            int i = 1;
            while (true)
            {
                TestData.CommandRunningCallback?.Invoke(i++ % 2 == 0 ? node.value : null);

                // Clone image from screenshot
                lock (ScreenStateThread.ScreenBmpLock)
                {
                    lock (RecognizeText.ImageLock)
                        BitmapUtility.Clone32BPPBitmap(ScreenStateThread.ScreenBmp, screen);
                }
                //var points = (Point[])m.Invoke(null, new object[] { screen });

                Point[] points;
                lock (RecognizeText.Boxes)
                {
                    points = RecognizeText.Points.ToArray();
                }

                if (TestData.ShouldCancelRun)
                {
                    RecognizeText.Run = false;
                    return;
                }

                var ops = GenerateRandomOperation(rand, points);
                if (ops == null)
                    continue;

                if (points.Length < 8)
                    return;

                foreach (var c in ops)
                {
                    if (TestData.ShouldCancelRun)
                    {
                        RecognizeText.Run = false;
                        return;
                    }

                    c.Run(TestData);
                }
            }
        }

        private Command[] GenerateRandomOperation(Random rand, Point[] points)
        {
            Logger.Log(LogType.Log, "Lenght: " + points.Length);
            if (points.Length == 0)
                return null;

            var countOfOperations = 2;
            var op = rand.Next() % countOfOperations;
            // var randomLocation = rand.Next() % 2 == 0;

            var pointIndex = rand.Next() % points.Length;
            var pointIndexTo = rand.Next() % points.Length;

            var p = points[pointIndex];
            var pTo = points[pointIndexTo];
            // var pTo = randomLocation ? points[pointIndexTo] : points[pointIndexTo];
            op = 1;
            switch (op)
            {/*
                case 0:
                    return new[] { new CommandPress(p.X, p.Y, false, MouseButton.Left) };
                    */
                case 1:
                    return new Command[]
                    {
                        new CommandPress(p.X, p.Y, false, MouseButton.Left),
                        new CommandSleep(20),
                        new CommandDown(p.X, p.Y, false, MouseButton.Left),
                        new CommandSleep(45),
                       // new CommandMove(pTo.X, pTo.Y),
                        new CommandRelease(pTo.X, pTo.Y, false),
                        new CommandSleep(20),
                    };

                case 0:
                    return new Command[]
                    {
                        new CommandPress(p.X, p.Y, false, MouseButton.Left),
                        new CommandSleep(20),
                        new CommandPress(p.X, p.Y, false, MouseButton.Left)
                    };

                default:
                    return null;
            }
        }

        private static void OverrideCommandPropertiesIfExist(Command command, object value, string prop)
        {
            var destProp = command.GetType().GetProperty(prop);

            if (destProp != null)
                destProp.SetValue(command, value);
        }
    }
}