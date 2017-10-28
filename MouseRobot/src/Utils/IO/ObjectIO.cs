﻿using Robot.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robot.IO
{
    public abstract class ObjectIO
    {
        public abstract T LoadObject<T>(string path);
        public abstract void SaveObject<T>(string path, T objToWrite);

        public static ObjectIO Create()
        {
            // TODO: User prefs file/user settings or something

            //if (true)
                return new BinaryObjectIO();
            /*else if (false)
                return new YamlObjectIO();
            else
                return null;*/
        }
    }
}
