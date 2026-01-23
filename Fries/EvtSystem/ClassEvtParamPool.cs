using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fries.EvtSystem {
    // TODO 提供相应的 清理类
    public static class ClassEvtParamPoolCleaner {
        private static HashSet<Type> types = new();
        public static void recordType(Type evtType) => types.Add(evtType);
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void clean() {
            types ??= new HashSet<Type>();
            foreach (var type in types) {
                var cacheType = typeof(ClassEvtParamPool<>).MakeGenericType(type);
                var clearMethod = cacheType.GetMethod("clear", BindingFlags.Public | BindingFlags.Static);
                clearMethod?.Invoke(null, null);
            }
            types.Clear();
        }
    }
    
    public static class ClassEvtParamPool<T> where T : new() {
        private static List<T> pool = new();

        static ClassEvtParamPool() => ClassEvtParamPoolCleaner.recordType(typeof(T));
        
        public static void clear() {
            pool ??= new List<T>();
            pool.Clear();
        }

        public static T pop() {
            if (pool == null) pool = new();
            if (pool.Count == 0) return new T();
            T t = pool[^1];
            pool.RemoveAt(pool.Count - 1);
            return t;
        }

        public static void push(T t) {
            if (pool == null)
                pool = new();
            pool.Add(t);
        }
    }
}