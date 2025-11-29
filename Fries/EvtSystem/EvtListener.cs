using System;
using System.Collections.Generic;

namespace Fries.EvtSystem {
    [AttributeUsage(AttributeTargets.Method)]
    public class EvtListener : Attribute {
        public readonly Type type;
        public readonly float priority;
        public readonly bool canBeExternallyCancelled;
        private readonly HashSet<string> friendAssembliesSet;
        public bool isFriendlyAssembly(string assemblyFullName) {
            if (assemblyFullName == null) return false;
            return friendAssembliesSet.Contains(assemblyFullName);
        }

        public EvtListener(Type type, float priority = 0, bool canBeExternallyCancelled = false, string[] friendAssemblies = null) {
            this.type = type;
            
            this.priority = priority;
            this.canBeExternallyCancelled = canBeExternallyCancelled;

            friendAssembliesSet = new HashSet<string> { type.Assembly.FullName };
            foreach (var friendAssembly in friendAssemblies.Nullable()) 
                friendAssembliesSet.Add(friendAssembly);
        }
    }
}