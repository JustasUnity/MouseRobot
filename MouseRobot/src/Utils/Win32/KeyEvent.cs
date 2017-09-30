﻿using System.Drawing;
using System.Windows.Forms;

namespace Robot.Utils.Win32
{
    public enum KeyAction { KeyDown, KeyUp }

    public struct KeyEvent
    {
        /// <summary>
        /// Keyboard or mouse button code (standard .NET Keys struct)
        /// </summary>
        public Keys keyCode;

        /// <summary>
        /// Is key down or key up
        /// </summary>
        public KeyAction keyAction;

        /// <summary>
        /// Screen point for mouse click
        /// </summary>
        private Point m_Point;

        public KeyEvent(Keys keyCode, KeyAction keyAction, Point point)
        {
            this.keyCode = keyCode;
            this.keyAction = keyAction;
            this.m_Point = point;
        }
        public KeyEvent(Keys keyCode, KeyAction keyAction)
        {
            this.keyCode = keyCode;
            this.keyAction = keyAction;
            this.m_Point = default(Point);
        }

        public bool IsKeyDown()
        {
            return KeyAction.KeyDown == keyAction;
        }

        public bool IsKeyUp()
        {
            return KeyAction.KeyUp == keyAction;
        }

        public int X { get { return m_Point.X; } }
        public int Y { get { return m_Point.Y; } }
    }
}
