﻿using System;
using Robot.Utils.Win32;
using System.Diagnostics;
using System.Drawing;
using RobotRuntime;

namespace Robot
{
    public class MouseRobot
    {
        static private MouseRobot m_Instance = new MouseRobot();
        static public MouseRobot Instance { get { return m_Instance; } }

        public event Action<bool> RecordingStateChanged;
        public event Action<bool> PlayingStateChanged;

        private bool m_IsRecording;
        private bool m_IsPlaying;

        private Stopwatch m_SleepTimer = new Stopwatch();
        private Point m_LastClickPos = new Point(0, 0);

        public CommandManagerProperties commandManagerProperties;

        private MouseRobot()
        {
            ScriptManager.Instance.NewScript();
            InputCallbacks.inputEvent += OnInputEvent;

            ScriptThread.Instance.Finished += OnScriptFinished;

            //ScreenStateThread.Instace.Start(10);
        }

        public void ForceInit() { } // This is to make sure that mouse robot singleton is created

        private void OnInputEvent(KeyEvent e)
        {
            if (!m_IsRecording)
                return;

            if (ScriptManager.Instance.LoadedScripts.Count == 0)
                return;

            var activeScript = ScriptManager.Instance.ActiveScript;

            if (e.IsKeyDown())
            {
                if (e.keyCode == commandManagerProperties.DefaultSleepKey)
                    activeScript.AddCommandSleep(commandManagerProperties.DefaultSleepTime);

                if (e.keyCode == commandManagerProperties.SleepKey)
                {
                    m_SleepTimer.Reset();
                    m_SleepTimer.Start();
                }

                if (e.keyCode == commandManagerProperties.SmoothMouseMoveKey)
                {
                    activeScript.AddCommandMove(e.X, e.Y);
                    m_LastClickPos = e.Point;
                    // TODO: TIME ?
                }

                if (e.keyCode == commandManagerProperties.MouseDownButton)
                {
                    if (commandManagerProperties.AutomaticSmoothMoveBeforeMouseDown && Distance(m_LastClickPos, e.Point) > 20)
                        activeScript.AddCommandMove(e.X, e.Y); // TODO: TIME ?

                    if (commandManagerProperties.TreatMouseDownAsMouseClick)
                        activeScript.AddCommandPress(e.X, e.Y);
                    else
                        activeScript.AddCommandDown(e.X, e.Y);
                    m_LastClickPos = e.Point;
                }
            }
            else if (e.IsKeyUp())
            {
                if (e.keyCode == commandManagerProperties.SleepKey)
                {
                    m_SleepTimer.Stop();
                    activeScript.AddCommandSleep((int)m_SleepTimer.ElapsedMilliseconds);
                }

                if (e.keyCode == commandManagerProperties.MouseDownButton && !commandManagerProperties.TreatMouseDownAsMouseClick)
                {
                    if (commandManagerProperties.AutomaticSmoothMoveBeforeMouseUp && Distance(m_LastClickPos, e.Point) > 20)
                        activeScript.AddCommandMove(e.X, e.Y); // TODO: TIME ?
                    else
                        activeScript.AddCommandSleep(commandManagerProperties.ThresholdBetweenMouseDownAndMouseUp);

                    activeScript.AddCommandRelease();
                    m_LastClickPos = e.Point;
                }
            }
            else
            {
                throw new Exception("Key event was fired but neither KeyUp or KeyDown was true");
            }

        }

        public void StartScript()
        {
            if (ScriptManager.Instance.ActiveScript == null)
                return;

            ScriptThread.Instance.Start(ScriptManager.Instance.ActiveScript.ToLightScript());
        }

        public void StopScript()
        {
            ScriptThread.Instance.Stop();
        }

        private void OnScriptFinished()
        {
            IsPlaying = false;
        }

        public bool IsRecording
        {
            get
            {
                return m_IsRecording;
            }
            set
            {
                if (m_IsPlaying)
                    throw new InvalidOperationException("Cannot record while playing script.");

                if (value != m_IsRecording)
                {
                    m_IsRecording = value;
                    RecordingStateChanged?.Invoke(value);
                }
            }
        }

        public bool IsPlaying
        {
            get
            {
                return m_IsPlaying;
            }
            set
            {
                if (m_IsRecording)
                    throw new InvalidOperationException("Cannot play a script while is recording.");

                if (value != m_IsPlaying)
                {
                    m_IsPlaying = value;
                    PlayingStateChanged?.Invoke(m_IsPlaying);

                    if (m_IsPlaying)
                        StartScript();
                    else
                        StopScript();
                }
            }
        }

        private float Distance(Point a, Point b)
        {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }
    }
}