using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fries {
    public static class SystemUtils {
        public static long currentTimeMillis() {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public static string projectName() {
            string dataPath = Application.dataPath;
            string projectName = new DirectoryInfo(dataPath).Parent.Name;
            return projectName;
        }
        
        public static bool hasAnnotation(this FieldInfo field, Type type) {
            if (field == null || type == null)
                return false;

            // 判断该字段上是否定义了指定的特性（不搜索继承链）
            return Attribute.IsDefined(field, type, false);
        }

        public static T[] concat<T>(T[] first, T[] second) {
            var joined = new T[first.Length + second.Length];
            first.CopyTo(joined, 0);
            second.CopyTo(joined, first.Length);
            return joined;
        }
    }
}