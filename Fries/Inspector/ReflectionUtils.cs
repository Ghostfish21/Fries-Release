# if UNITY_EDITOR
using UnityEditor;
# endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Fries.Inspector {
    public static class ReflectionUtils {
# if UNITY_EDITOR
        public static object getValue(this SerializedProperty property) {
            Type parentType = property.serializedObject.targetObject.GetType();
            string[] comps = property.propertyPath.Split(".");
            object value = property.serializedObject.targetObject;
            foreach (var comp in comps) {
                if (comp == "Array") continue;
                if (comp.Contains("data[")) {
                    int i = int.Parse(comp.Replace("data[", "").Replace("]", ""));
                    IList list = value as IList;
                    Debug.Assert(list != null, nameof(list) + " != null");
                    if (i < 0 || i >= list.Count) return null;
                    value = list[i];
                    parentType = value.GetType();
                    continue;
                }

                FieldInfo fi = parentType.GetField(comp);
                if (fi == null) return null;
                value = fi.GetValue(value);
                if (value == null) return null;
                parentType = value.GetType();
            }

            return value;
        }

        public static bool hasAnnotation(this SerializedProperty sp, Type type) {
            if (sp == null || type == null)
                return false;

            // 通过辅助方法获取 FieldInfo
            FieldInfo field = sp.getFieldInfo();
            if (field == null) return false;

            // 调用针对 FieldInfo 的扩展方法进行判断
            return field.hasAnnotation(type);
        }

        public static FieldInfo getFieldInfo(this SerializedProperty property) {
            if (property == null) return null;

            Type parentType = property.serializedObject.targetObject.GetType();
            string[] comps = property.propertyPath.Split(".");
            object value = property.serializedObject.targetObject;
            FieldInfo fi = null;
            foreach (var comp in comps) {
                if (comp == "Array") continue;
                if (comp.Contains("data[")) {
                    int i = int.Parse(comp.Replace("data[", "").Replace("]", ""));
                    IList list = value as IList;
                    Debug.Assert(list != null, nameof(list) + " != null");
                    value = list[i];
                    parentType = value.GetType();
                    continue;
                }

                fi = parentType.GetField(comp);
                if (fi == null) return null;
                value = fi.GetValue(value);
                if (value == null) return null;
                parentType = value.GetType();
            }

            return fi;
        }
# endif

        public static void forStaticMethods(Action<MethodInfo, Delegate> forMethod, Type attributeType, BindingFlags bindingFlags, Type returnType, string[] loadAssembly = null, params Type[] paramTypes) {
            bindingFlags |= BindingFlags.Static;
            loopAssemblies((assembly) => {
                List<MethodInfo> mis = loadMethodsOfType(assembly, attributeType, bindingFlags);
                foreach (var mi in mis) {
                    if (mi.checkSignature(returnType, paramTypes)) {
                        forMethod.Invoke(mi, mi.toDelegate());
                    }
                }
                
            }, loadAssembly);
        }
        
        public static void forStaticMethods(Action<MethodInfo, Delegate> forMethod, Type attributeType, BindingFlags bindingFlags, Type returnType, string[] loadAssembly = null) {
            bindingFlags |= BindingFlags.Static;
            loopAssemblies(assembly => {
                List<MethodInfo> mis = loadMethodsOfType(assembly, attributeType, bindingFlags);
                foreach (var mi in mis) {
                    try {
                        if (mi.checkReturn(returnType))
                            forMethod.Invoke(mi, mi.toDelegate());
                    } catch (Exception e) { Debug.LogWarning($"Failed to load method {mi.Name}!\n{e}"); }
                }
                
            }, loadAssembly);
        }
        
        public static void forType(Action<Type> forMethod, Type attributeType, string[] loadAssembly = null) {
            loopAssemblies(assembly => {
                List<Type> types = loadType(assembly, attributeType);
                foreach (var ty in types) {
                    try { forMethod.Invoke(ty); }
                    catch (Exception e) { Debug.LogWarning($"Failed to load type {ty.FullName}!\n{e}"); }
                }
            }, loadAssembly);
        }

        private static List<Type> loadType(Assembly assembly, Type attributeType) {
            List<Type> info = new();
            Type[] types = assembly.GetTypes();
            foreach (var type in types) {
                var attrs = type.GetCustomAttributes(attributeType);
                if (attrs.Any()) info.Add(type);
            }

            return info;
        }

        public static void loopAssemblies(Action<Assembly> action, string[] loadAssembly = null) {
            // 尝试加载 Assembly-CSharp
            try {
                Assembly assemblyCSharp = Assembly.Load("Assembly-CSharp");
                if (assemblyCSharp != null)
                    action(assemblyCSharp);

                // 加载当前程序集
                Assembly selfAssembly = Assembly.GetExecutingAssembly();
                if (selfAssembly != assemblyCSharp)
                    action(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex) {
                Debug.LogWarning($"Failed to load assembly!\n{ex}");
            }

            // 加载 loadAssembly 中指定的程序集
            if (loadAssembly == null) return;
            foreach (var assemblyName in loadAssembly.Nullable()) {
                try {
                    Assembly asm = Assembly.Load(assemblyName);
                    if (asm != null)
                        action(asm);
                }
                catch {
                    Debug.LogWarning($"Failed to load assembly {assemblyName}!");
                }
            }
        }

        private static List<MethodInfo> loadMethodsOfType(Assembly assembly, Type attributeType,
            BindingFlags bindingFlags) {
            List<MethodInfo> info = new();
            Type[] types = assembly.GetTypes();
            foreach (Type type in types) {
                // 获取类型中所有方法（公有、非公有，静态与实例方法）
                foreach (MethodInfo method in type.GetMethods(bindingFlags)) {
                    // 获取所有标记了 EditorUpdateAttribute 的特性
                    var attr = method.GetCustomAttribute(attributeType, false);
                    if (attr != null) info.Add(method);
                }
            }

            return info;
        }

        public static bool checkReturn(this MethodInfo mi, Type returnType) {
            if (mi.ReturnType != returnType) return false;
            return true;
        }
        
        public static bool checkSignature(this MethodInfo mi, Type returnType, params Type[] paramTypes) {
            if (mi.ReturnType != returnType) return false;
            var parameters = mi.GetParameters();
            int paramLength;
            if (paramTypes == null) paramLength = 0;
            else paramLength = paramTypes.Length;
            if (parameters.Length != paramLength) return false;
            bool shouldReturnFalse = false;
            parameters.ForEach((i, p, b) => {
                if (p.ParameterType == paramTypes[i]) return;
                shouldReturnFalse = true;
                b.@break();
            });
            if (shouldReturnFalse) return false;
            return true;
        }

        public static Delegate toDelegate(this MethodInfo method, object targetInstance = null) {
            // 1. 获取方法的参数类型列表
            var paramTypes = method.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();

            // 2. 动态生成一个 Action<...> 类型
            var actionType = Expression.GetActionType(paramTypes);

            return targetInstance == null
                ? Delegate.CreateDelegate(actionType, method)
                : Delegate.CreateDelegate(actionType, targetInstance, method);
        }
    }
}