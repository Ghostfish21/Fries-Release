using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Fries.EvtSystem {
    public static class EvtInstStatic {
        private static HashSet<(Type, Type)> types = new();
        public static void recordType(Type evtType, Type instType) => types.Add((evtType, instType));
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void clean() {
            types ??= new HashSet<(Type, Type)>();
            foreach (var (evtType, instType) in types) {
                var cacheType = typeof(EvtInstCache<,>).MakeGenericType(evtType, instType);
                var clearMethod = cacheType.GetMethod("clear", BindingFlags.Public | BindingFlags.Static);
                clearMethod?.Invoke(null, null);
            }
            types.Clear();
            addMethodCache ??= new Dictionary<(Type, Type), Action<object>>();
            addMethodCache.Clear();
        }
        
        private static Dictionary<(Type evtType, Type instType), Action<object>> addMethodCache = new();
        public static void add(Type evtType, Type instType, object inst) {
            var key = (evtType, instType);

            if (!addMethodCache.TryGetValue(key, out var addAction)) {
                var cacheType = typeof(EvtInstCache<,>).MakeGenericType(evtType, instType);
                var addMethod = cacheType.GetMethod("add", BindingFlags.Public | BindingFlags.Static);
                var paramObj = Expression.Parameter(typeof(object), "inst");
                var paramCast = Expression.Convert(paramObj, instType);
                var callExpr = Expression.Call(addMethod, paramCast);
                var lambda = Expression.Lambda<Action<object>>(callExpr, paramObj);
                addAction = lambda.Compile();
                addMethodCache[key] = addAction;
            }
            addAction(inst);
        }
    }
    
    public static class EvtInstCache<E, I> {
        public static HashSet<I> insts = new();

        static EvtInstCache() => EvtInstStatic.recordType(typeof(E), typeof(I));
        
        public static Type evtType => typeof(E);
        public static void add(I inst) => insts.Add(inst);
        public static void remove(I inst) => insts.Remove(inst);
        public static bool contains(I inst) => insts.Contains(inst);
        public static void clear() => insts.Clear();
    }
}