﻿using Robot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace Robot
{
    public static class BinaryObjectIO
    {
        public static T LoadObject<T>(string fileName)
        {
            using (Stream stream = File.Open(fileName, FileMode.Open))
            {
                try
                {
                    return (T)new BinaryFormatter().Deserialize(stream);
                }
                catch (SerializationException)
                {
                    Console.WriteLine("Failed to read from file: " + fileName);
                    return default(T);
                }
            }
        }

        public static void SaveObject<T>(string fileName, T objToWrite)
        {
            using (Stream stream = File.Open(fileName, FileMode.Create))
            {
                try
                {
                    new BinaryFormatter().Serialize(stream, objToWrite);
                }   
                catch(SerializationException)
                {
                    Console.WriteLine("Failed to write to file: " + fileName);
                }
            }
        }
    }
}
