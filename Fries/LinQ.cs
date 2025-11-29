using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Random = UnityEngine.Random;

namespace Fries {
    public class Break {
        public bool b = false;
        public void @break() {
            b = true;
        }
    }
    
    public static class LinQ {
        public static T[] Join<T>(this T[] array, T[] toAdd) {
            T[] newArray = new T[array.Length + toAdd.Length];
            (array, toAdd).ForEach<T>((i, e) => {
                newArray[i] = e;
            });

            return newArray;
        }
        
        public static void ForEach<T>(this ITuple tuple, Action<T> action) {
            for (int i = 0; i < tuple.Length; i++) {
                if (tuple[i] is IEnumerable<T> e) {
                    foreach (var e1 in e) action(e1); 
                }
                else throw new InvalidEnumArgumentException("Input Tuple must only contains IEnumerable<T>!");
            }
        }
        
        public static void ForEach<T>(this ITuple tuple, Action<int, T> actionWithElementIndex) {
            int j = 0;
            for (int i = 0; i < tuple.Length; i++) {
                if (tuple[i] is IEnumerable<T> e) {
                    foreach (var e1 in e) {
                        actionWithElementIndex(j, e1);
                        j++;
                    } 
                }
                else throw new InvalidEnumArgumentException("Input Tuple must only contains IEnumerable<T>!");
            }
        }
        
        public static void ForEach<T>(this ITuple tuple, Action<int, int, T> actionWithListAndElementIndex) {
            int j = 0;
            for (int i = 0; i < tuple.Length; i++) {
                if (tuple[i] is IEnumerable<T> e) {
                    foreach (var e1 in e) {
                        actionWithListAndElementIndex(i, j, e1);
                        j++;
                    }
                }
                else throw new InvalidEnumArgumentException("Input Tuple must only contains IEnumerable<T>!");
            }
        }
        
        public static void ForEach<T>(this ITuple tuple, Action<int, int, int, T> actionWithListAndLocalAndElementIndex) {
            int j = 0;
            for (int i = 0; i < tuple.Length; i++) {
                if (tuple[i] is IEnumerable<T> e) {
                    int li = 0;
                    foreach (var e1 in e) {
                        actionWithListAndLocalAndElementIndex(i, li, j, e1);
                        j++;
                        li++;
                    }
                }
                else throw new InvalidEnumArgumentException("Input Tuple must only contains IEnumerable<T>!");
            }
        }
        
        public static void ForEach<T>(this ITuple tuple, Action<int, int, T, Break> breakableActionWithListAndElementIndex) {
            Break b = new Break();
            int j = 0;
            for (int i = 0; i < tuple.Length; i++) {
                if (tuple[i] is IEnumerable<T> e) {
                    foreach (var e1 in e) {
                        breakableActionWithListAndElementIndex(i, j, e1, b);
                        if (b.b) return;
                        j++;
                    }
                }
                else throw new InvalidEnumArgumentException("Input Tuple must only contains IEnumerable<T>!");
            }
        }

        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action) {
            foreach (var item in ie) action(item);
        }
        
        public static void ForEach<T>(this IEnumerable<T> ie, Action<int, T, Break> action) {
            int i = 0;
            Break b = new Break();
            foreach (var item in ie) {
                action(i, item, b);
                if (b.b) break;
                i++;
            }
        }
        
        public static void ForEach<T>(this IEnumerable<T> ie, Action<int, T> action) {
            int i = 0;
            foreach (var item in ie) {
                action(i, item);
                i++;
            }
        }
        
        public static void ForRange(this int from, int exclusiveTo, Action<int> action) {
            for (int i = from; i < exclusiveTo; i++) {
                action(i);
            }
        }
        
        public static void ForRange(this (int from, int exclusiveTo) param, Action<int> action) {
            param.from.ForRange(param.exclusiveTo, action);
        }
        
        public static void For<T>(this IEnumerable<T> ie, Action<int, T> action) {
            int i = 0;
            foreach (var item in ie) {
                action(i, item);
                i++;
            }
        }

        public static T RandomElement<T>(this IList<T> list) {
            int ri = Random.Range(0, list.Count);
            return list[ri];
        }
        public static T RandomElement<T>(this IList<T> list, System.Random rand) {
            int ri = rand.Next(0, list.Count);
            return list[ri];
        }

        public static T Until<T>(this Func<T> execute, Func<T, bool> condition) {
            T r = execute();
            while (!condition(r)) 
                r = execute();
            return r;
        }

        public static List<T> Nullable<T>(this List<T> list) {
            if (list == null) return new List<T>();
            return list;
        }
        
        public static T[] Nullable<T>(this T[] array) {
            if (array == null) return Array.Empty<T>();
            return array;
        }

        public static string Nullable(this string str) {
            if (str == null) return "";
            return str;
        }
    }
}