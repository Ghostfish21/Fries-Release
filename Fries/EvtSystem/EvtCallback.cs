using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Fries.EvtSystem {
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class EvtCallback : PreserveAttribute {
        // TODO 禁止泛型类声明该callback方法
        public readonly Type type;
        public readonly float priority;
        public readonly bool canBeExternallyCancelled;
        public readonly bool areInstsManaged;
        private readonly HashSet<string> friendAssembliesSet;
        public bool isFriendlyAssembly(string assemblyFullName) {
            if (assemblyFullName == null) return false;
            return friendAssembliesSet.Contains(assemblyFullName);
        }

        public EvtCallback(Type type, float priority = 0, bool areInstsManaged = true, bool canBeExternallyCancelled = false, string[] friendAssemblies = null) {
            this.type = type;
            this.areInstsManaged = areInstsManaged;
            this.priority = priority;
            this.canBeExternallyCancelled = canBeExternallyCancelled;

            friendAssembliesSet = new HashSet<string> { type.Assembly.FullName };
            foreach (var friendAssembly in friendAssemblies.Nullable()) 
                friendAssembliesSet.Add(friendAssembly);
        }
    }
}