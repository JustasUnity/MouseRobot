using Emgu.CV;
using Emgu.CV.Structure;
using RobotRuntime.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
//using Tesseract;

namespace RobotRuntime
{
    public class RecognizeText
    {
        public static bool Run = false;
        // static TesseractEngine engine = null;

        public static IList<Rectangle> Boxes = new List<Rectangle>();
        public static IList<Point> Points = new List<Point>();

        public static object ImageLock = new object();

        private static Bitmap cloneOfScreen = null;

        /*public static Point[] GetListOfPoints(Bitmap image)
        {
            if (engine == null)
                engine = new TesseractEngine(Paths.PluginPath + "/tessdata", "eng", EngineMode.Default);

            try
            {
                using (var page = engine.Process(image, PageSegMode.Count))
                {
                    lock (Boxes)
                    {
                        Boxes.Clear();
                        Points.Clear();

                        using (var iter = page.GetIterator())
                        {
                            iter.Begin();
                            do
                            {
                                //var word = iter.GetText(PageIteratorLevel.Word);

                                // skip top menu
                                Rect box;
                                iter.TryGetBoundingBox(PageIteratorLevel.Word, out box);
                                /*if (box.Y1 < 200)
                                    continue;

                                // remove trash
                                if (!WordIsClean(word))
                                    word = CleanWord(word);

                                // skip short words
                                if (word.Length < 3)
                                    continue;*//*

                                Boxes.Add(box);
                                Point p = new Point((box.X1 + box.X2) / 2, (box.Y1 + box.Y2) / 2);
                                Points.Add(p);

                                //Console.WriteLine(word + " " + box.ToString() + p);

                            } while (iter.Next(PageIteratorLevel.Word));
                        }
                    } // lock
                } // using
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
            }
            return Points.ToArray();
        }*/

        static public List<Rectangle> detectLettersRectangles(Bitmap screen)
        {
            lock (ImageLock)
            {
                BitmapUtility.Clone32BPPBitmap(screen, cloneOfScreen);
            }

            Image<Bgr, Byte> img = cloneOfScreen.ToImage();
            /*
             1. Edge detection (sobel)
             2. Dilation (10,1)
             3. FindContours
             4. Geometrical Constrints
             */

            //sobel
            Image<Gray, byte> sobel = img.Convert<Gray, byte>().Sobel(1, 0, 3).AbsDiff(new Gray(0.0)).Convert<Gray, byte>().ThresholdBinary(new Gray(50), new Gray(255));
            Mat SE = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(10, 2), new Point(-1, -1));
            sobel = sobel.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Dilate, SE, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Reflect, new MCvScalar(255));
            Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            Mat m = new Mat();

            CvInvoke.FindContours(sobel, contours, m, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

            List<Rectangle> list = new List<Rectangle>();

            for (int i = 0; i < contours.Size; i++)
            {
                Rectangle brect = CvInvoke.BoundingRectangle(contours[i]);

                double ar = brect.Width / brect.Height;
                if (ar > 2 && brect.Width > 25 && brect.Height > 8 && brect.Height < 100)
                {
                    list.Add(brect);
                }
            }
            lock (Boxes)
            {
                Boxes = list.Select(r => new Rectangle(r.X, r.Y, r.Width, r.Height)).Where(p => p.Y > 70 && p.Y < 1000).ToArray();
                Points = list.Select(r => new Point(r.X + r.Width / 2, r.Y + r.Height / 2)).Where(p => p.Y > 70 && p.Y < 1000).ToArray();
            }
            return list;
        }

        static bool WordIsClean(string word)
        {
            if (Regex.IsMatch(word, @"^[a-zA-Z]+$"))
                return true;

            return false;
        }
        static string CleanWord(string word)
        {
            var match = Regex.Match(word, @"[a-zA-Z]{3, 20}");

            if (match.Success)
                return match.Groups[0].Value;

            return "";
        }

        public static void StartNewThreadForTextRecognition(Bitmap screen)
        {
            Run = true;

            if (cloneOfScreen == null)
                cloneOfScreen = new Bitmap(screen.Width, screen.Height);

            new Thread(() =>
            {
                while (Run)
                {
                    detectLettersRectangles(screen);
                    Thread.Sleep(10);
                }
            }).Start();
        }
    }
}
