using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fries.EvtSystem {
    public class EvtInitializer {
        private static List<EvtInitializer> initializers = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void init() => initializers = new List<EvtInitializer>();
        protected static void register(EvtInitializer initializer) => initializers.Add(initializer);
        protected EvtInitializer() { }

        
        
        public static void createAllListeners(Action<string, Type, EvtListener, Delegate> registerEvtListenerByInfo,
            Action<MethodInfo> registerEvtListenerByReflection) {
            foreach (var evtInitializer in initializers) {
                evtInitializer.init(registerEvtListenerByInfo, registerEvtListenerByReflection);
            }
        }
        
        protected Action<string, Type, EvtListener, Delegate> registerEvtListenerByInfo;
        protected Action<MethodInfo> registerEvtListenerByReflection;
        
        protected virtual void init(Action<string, Type, EvtListener, Delegate> registerEvtListenerByInfo,
            Action<MethodInfo> registerEvtListenerByReflection) {
            this.registerEvtListenerByInfo = registerEvtListenerByInfo;
            this.registerEvtListenerByReflection = registerEvtListenerByReflection;
        }
        
        
        
        public static void createAllEvents(Action<Type> registerEventByType) {
            foreach (var evtInitializer in initializers) {
                evtInitializer.declare(registerEventByType);
            }
        }

        protected Action<Type> registerEventByType;

        protected virtual void declare(Action<Type> registerEventByType) {
            this.registerEventByType = registerEventByType;
        }
    }
}