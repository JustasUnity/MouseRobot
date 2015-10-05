﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MouseRobot
{
    public partial class MouseRobotImpl : IMouseRobot
    {
        public void StartScript(int repeatTimes)
        {
            if (list.Count <= 0)
            {
                throw new EmptyScriptException("Script is empty");
            }

            new Thread(delegate()
            {
                for (int i = 1; i <= repeatTimes; i++)
                {
                    Console.WriteLine(i + " - Script start");
                    foreach (var v in list)
                    {
                        Console.WriteLine(v.Text);
                        v.Run();

                        if (BreakEvent != null)
                        {
                            BreakEvent.Invoke(this, null);
                            BreakEvent -= new EventHandler(OnBreakEvent);
                            return;
                        }
                    }
                }
                Console.WriteLine("End script.");
            }).Start();
        }

        public void AddCommandSleep(int time)
        {
            list.Add(new Command(() =>
            {
                Thread.Sleep(time);
                if (CheckIfPointerOffScreen()) BreakEvent += new EventHandler(OnBreakEvent);
            }, "Sleep for " + time + " ms.", CommandCode.G, time));
        }

        public void AddCommandRelease()
        {
            list.Add(new Command( () => 
            {
                MouseAction(WinAPI.MouseEventFlags.LeftUp);
                if (CheckIfPointerOffScreen()) BreakEvent += new EventHandler(OnBreakEvent);
            }, "Release", CommandCode.K));
        }

        public void AddCommandPress(int x, int y)
        {
            list.Add(new Command(delegate()
            {
                MouseMoveTo(x, y);
                MouseAction(WinAPI.MouseEventFlags.LeftDown);
                MouseAction(WinAPI.MouseEventFlags.LeftUp);
                if (CheckIfPointerOffScreen()) BreakEvent += new EventHandler(OnBreakEvent);
            }, "Press on: (" + x + ", " + y + ")", CommandCode.S, x, y));
        }

        public void AddCommandMove(int x, int y)
        {
            list.Add(new Command(delegate()
            {
                int x1, y1;
                x1 = WinAPI.GetCursorPosition().X;
                y1 = WinAPI.GetCursorPosition().Y;

                for (int i = 1; i <= 50; i++)
                {
                    MouseMoveTo(x1 + ((x - x1) * i / 50), y1 + ((y - y1) * i / 50));
                }
                if (CheckIfPointerOffScreen()) BreakEvent += new EventHandler(OnBreakEvent);
            }, "Move to: (" + x + ", " + y + ")", CommandCode.J, x, y));
        }

        public void AddCommandDown(int x, int y)
        {
            list.Add(new Command(delegate()
            {
                MouseMoveTo(x, y);
                MouseAction(WinAPI.MouseEventFlags.LeftDown);
                if (CheckIfPointerOffScreen()) BreakEvent += new EventHandler(OnBreakEvent);
            }, "Down on: (" + x + ", " + y + ")", CommandCode.H, x, y));
        }

        public void EmptyScript()
        {
            list.Clear();
        }

        public void Open(string fileName)
        {
            IList<Command> tempList = BinaryObjectIO.LoadScriptFile<IList<Command>>(fileName);
            list.Clear();

            Console.WriteLine();
            Console.WriteLine("Reading file:");

            foreach (var v in tempList)
            {
                switch (v.Code) 
                {
                    case CommandCode.G:
                        AddCommandSleep(v.Args.ElementAt<int>(0));
                        break;
                    case CommandCode.S:
                        AddCommandPress(v.Args.ElementAt<int>(0), v.Args.ElementAt<int>(1));
                        break;
                    case CommandCode.H:
                        AddCommandDown(v.Args.ElementAt<int>(0), v.Args.ElementAt<int>(1));
                        break;
                    case CommandCode.J:
                        AddCommandMove(v.Args.ElementAt<int>(0), v.Args.ElementAt<int>(1));
                        break;
                    case CommandCode.K:
                        AddCommandRelease();
                        break;
                }

                Console.WriteLine(v.Text);
            }
        }

        public void Save(string fileName)
        {
            foreach (var v in list)
            {
                v.RunMethod = null;
            }
            BinaryObjectIO.SaveScriptFile<IList<Command>>(fileName, list);

            Open(fileName);
            Console.WriteLine("File saved.");
        }
    }
}
