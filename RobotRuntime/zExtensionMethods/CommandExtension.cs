﻿using System;
using System.Reflection;

namespace RobotRuntime.Utils
{
    public static class CommandExtension
    {
        private const BindingFlags k_BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public static void CopyAllProperties(this Command dest, Command source)
        {
            var destProps = dest.GetType().GetProperties(k_BindingFlags);
            var sourceProps = source.GetType().GetProperties(k_BindingFlags);

            foreach(var destProp in destProps)
            {
                foreach(var sourceProp in sourceProps)
                {
                    if (destProp.CanWrite && sourceProp.CanRead && destProp.PropertyType == sourceProp.PropertyType && destProp.Name == sourceProp.Name)
                        destProp.SetValue(dest, sourceProp.GetValue(source));
                }
            }
        }

        public static void CopyAllFields(this Command dest, Command source)
        {
            var destFields = dest.GetType().GetFields(k_BindingFlags);
            var sourceFields = source.GetType().GetFields(k_BindingFlags);

            foreach (var destField in destFields)
            {
                foreach (var sourceField in sourceFields)
                {
                    if (destField.FieldType == sourceField.FieldType && destField.Name == sourceField.Name)
                        destField.SetValue(dest, sourceField.GetValue(source));
                }
            }
        }

        public static void CopyPropertyFromIfExist(this Command dest, Command source, string name)
        {
            var destProp = dest.GetType().GetProperty(name, k_BindingFlags);
            var sourceProp = source.GetType().GetProperty(name, k_BindingFlags);

            if (destProp != null && sourceProp != null)
            {
                destProp.SetValue(dest, sourceProp.GetValue(source));
            }
        }

        public static void SetPropertyIfExist(this Command dest, string name, object value)
        {
            var destProp = dest.GetType().GetProperty(name, k_BindingFlags);
            destProp?.SetValue(dest, value);
        }

        public static object GetPropertyIfExist(this Command source, string name)
        {
            var prop = source.GetType().GetProperty(name, k_BindingFlags);
            return prop != null ? prop.GetValue(source) : null;
        }

        public static void SetFieldIfExist(this Command dest, string name, object value)
        {
            var destField = dest.GetType().GetField(name, k_BindingFlags);
            destField?.SetValue(dest, value);
        }

        public static object GetFieldIfExist(this Command source, string name)
        {
            var field = source.GetType().GetField(name, k_BindingFlags);
            return field != null ? field.GetValue(source) : null;
        }
    }
}
