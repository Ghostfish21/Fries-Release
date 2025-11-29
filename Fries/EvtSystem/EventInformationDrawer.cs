// namespace Fries.InsertionEventSys {
// // Assets/Editor/InsertionEventInformationDrawer.cs
// #if UNITY_EDITOR
// using UnityEngine;
// using UnityEditor;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
//
// [CustomPropertyDrawer(typeof(InsertionEventInformation))]
// public class InsertionEventInformationDrawer : PropertyDrawer
// {
//     const float HPad = 6f;
//     const float VPad = 3f;
//     const float ColGap = 6f;
//     const float TokenGap = 10f;
//
//     static GUIStyle _boxStyle;
//     static GUIStyle BoxStyle {
//         get {
//             if (_boxStyle == null) {
//                 _boxStyle = new GUIStyle(EditorStyles.textField) {
//                     alignment = TextAnchor.MiddleLeft,
//                     padding = new RectOffset(6, 6, 2, 2),
//                     wordWrap = false,
//                     richText = false
//                 };
//                 var bright = EditorStyles.label.normal.textColor; // 使用 label 的正常文字色，避免变灰
//                 _boxStyle.normal.textColor  = bright;
//                 _boxStyle.focused.textColor = bright;
//                 _boxStyle.hover.textColor   = bright;
//                 _boxStyle.active.textColor  = bright;
//             }
//             return _boxStyle;
//         }
//     }
//
//     static GUIStyle _tokenStyle;
//     static GUIStyle TokenStyle {
//         get {
//             if (_tokenStyle == null) {
//                 _tokenStyle = new GUIStyle(EditorStyles.label) {
//                     alignment = TextAnchor.MiddleLeft,
//                     clipping  = TextClipping.Overflow,
//                     wordWrap  = false,
//                     padding   = new RectOffset(0,0,0,0)
//                 };
//                 var bright = EditorStyles.label.normal.textColor;
//                 _tokenStyle.normal.textColor = bright;
//             }
//             return _tokenStyle;
//         }
//     }
//     
//     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//     {
//         EditorGUI.BeginProperty(position, label, property);
//
//         var lineH = EditorGUIUtility.singleLineHeight;
//         float y = position.y;
//
//         var info = GetTargetObjectOfProperty(property) as InsertionEventInformation;
//         if (info == null) {
//             EditorGUI.LabelField(position, label, new GUIContent("(null)"));
//             EditorGUI.EndProperty();
//             return;
//         }
//
//         string insertedName = TypeToNiceName(info.insertedClass);
//         string eventName = info.eventName ?? "null";
//         var args = info.argsTypes ?? Array.Empty<Type>();
//         var listeners = info.listeners ?? new List<string>();
//
//         // 第一行：Foldout + 三个“值框”
//         var headerRect = new Rect(position.x, y, position.width, lineH);
//         var foldRect   = new Rect(headerRect.x, headerRect.y, 16f, lineH);
//         property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, GUIContent.none, true);
//
//         float usableW = headerRect.width - 16f;
//         float colW = usableW / 3f;
//         var col1 = new Rect(foldRect.xMax, y, colW - ColGap, lineH);
//         var col2 = new Rect(col1.xMax + ColGap, y, colW - ColGap, lineH);
//         var col3 = new Rect(col2.xMax + ColGap, y, usableW - (colW * 2) - (ColGap * 2), lineH);
//
//         ReadOnlyBox(col1, $"{insertedName}");
//         ReadOnlyBox(col2, $"{eventName}");
//         ReadOnlyBox(col3, $"Listeners: {listeners.Count}");
//
//         y += lineH + VPad;
//
//         // 内容区背景（从第二行开始）
//         int argRows = CalcArgRows(args, position.width - HPad * 2);
//         float argsHeight = Mathf.Max(1, argRows) * lineH;
//         float listenersHeight = (property.isExpanded && listeners.Count > 0) ? listeners.Count * (lineH + VPad) : 0f;
//         float contentHeight = argsHeight + VPad + listenersHeight;
//
//         var bgRect = new Rect(position.x, y, position.width, contentHeight + VPad);
//         EditorGUI.DrawRect(bgRect, new Color(0,0,0,0));
//
//         // 第二行：Args（可自动换行）
//         var argsStart = new Rect(position.x + HPad, y + VPad, position.width - HPad * 2, lineH);
//         DrawArgsTokens(argsStart, args);
//         y += argsHeight + VPad;
//
//         // 展开后：监听器，每行两个“值框”
//         if (property.isExpanded && listeners.Count > 0) {
//             float leftW = Mathf.Round(position.width - HPad * 2);
//             for (int i = 0; i < listeners.Count; i++) {
//                 var row = new Rect(position.x + HPad, y + VPad, position.width - HPad * 2, lineH);
//                 var left  = new Rect(row.x, row.y, leftW, lineH);
//
//                 ReadOnlyBox(left, $"{(listeners[i] ?? "null")}");
//
//                 y += (lineH + VPad);
//             }
//         }
//
//         EditorGUI.EndProperty();
//     }
//
//     public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//     {
//         var lineH = EditorGUIUtility.singleLineHeight;
//         float height = lineH + VPad; // header
//
//         var info = GetTargetObjectOfProperty(property) as InsertionEventInformation;
//         if (info == null) return height + lineH;
//
//         var args = info.argsTypes ?? Array.Empty<Type>();
//         var listeners = info.listeners ?? new List<string>();
//
//         float width = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 40f);
//         int argRows = CalcArgRows(args, width - HPad * 2);
//
//         height += Mathf.Max(1, argRows) * lineH + VPad;
//         if (property.isExpanded && listeners.Count > 0)
//             height += listeners.Count * (lineH + VPad);
//         else
//             height += VPad;
//
//         return height;
//     }
//
//     // ---------- 绘制辅助 ----------
//     static void ReadOnlyBox(Rect r, string text)
//     {
//         // 画一个“像 TextField 的值框”，但不可编辑
//         GUI.Label(r, text, BoxStyle);
//     }
//
//     void DrawArgsTokens(Rect startRect, Type[] args)
//     {
//         var lineH = EditorGUIUtility.singleLineHeight;
//         float x = startRect.x;
//         float y = startRect.y;
//         float maxX = startRect.x + startRect.width;
//
//         if (args == null || args.Length == 0) {
//             GUI.Label(new Rect(x, y, startRect.width, lineH), "(no arguments)", TokenStyle);
//             return;
//         }
//
//         for (int i = 0; i < args.Length; i++) {
//             string txt = $"{TypeToNiceName(args[i])}";
//             Vector2 sz = TokenStyle.CalcSize(new GUIContent(txt));
//             if (x + sz.x > maxX) { x = startRect.x; y += lineH; }
//             GUI.Label(new Rect(x, y, sz.x, lineH), txt, TokenStyle);
//             x += sz.x + TokenGap;
//         }
//     }
//
//     static int CalcArgRows(Type[] args, float width)
//     {
//         if (args == null || args.Length == 0) return 1;
//         float x = 0f; int rows = 1;
//         foreach (var t in args) {
//             string txt = $"{TypeToNiceName(t)}";
//             Vector2 sz = TokenStyle.CalcSize(new GUIContent(txt));
//             if (x > 0f && x + sz.x > width) { rows++; x = 0f; }
//             x += sz.x + TokenGap;
//         }
//         return Mathf.Max(1, rows);
//     }
//
//     static string TypeToNiceName(Type t)
//     {
//         if (t == null) return "null";
//         if (!t.IsGenericType) return t.Name;
//         string name = t.Name;
//         int tick = name.IndexOf('`');
//         if (tick >= 0) name = name.Substring(0, tick);
//         var args = t.GetGenericArguments().Select(TypeToNiceName);
//         return $"{name}<{string.Join(", ", args)}>";
//     }
//
//     // ---------- 反射取实例 ----------
//     static object GetTargetObjectOfProperty(SerializedProperty prop)
//     {
//         if (prop == null) return null;
//
// #if UNITY_2020_1_OR_NEWER
//         if (prop.propertyType == SerializedPropertyType.ManagedReference)
//             return prop.managedReferenceValue;
// #endif
//         var path = prop.propertyPath.Replace(".Array.data[", "[");
//         object obj = prop.serializedObject.targetObject;
//         foreach (var element in path.Split('.')) {
//             if (element.Contains("[")) {
//                 string name = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
//                 int index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal)).Trim('[', ']'));
//                 obj = GetValue_Imp(obj, name, index);
//             } else {
//                 obj = GetValue_Imp(obj, element);
//             }
//             if (obj == null) return null;
//         }
//         return obj;
//     }
//     static object GetValue_Imp(object source, string name)
//     {
//         if (source == null) return null;
//         var type = source.GetType();
//         while (type != null) {
//             var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
//             if (f != null) return f.GetValue(source);
//             var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
//             if (p != null) return p.GetValue(source, null);
//             type = type.BaseType;
//         }
//         return null;
//     }
//     static object GetValue_Imp(object source, string name, int index)
//     {
//         var enumerable = GetValue_Imp(source, name) as IEnumerable;
//         if (enumerable == null) return null;
//         var enm = enumerable.GetEnumerator();
//         for (int i = 0; i <= index; i++) if (!enm.MoveNext()) return null;
//         return enm.Current;
//     }
// }
// #endif
//
// }