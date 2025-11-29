using System;
using System.Collections.Generic;

namespace Fries.EvtSystem {
    public class EvtListenerInfo {
        public readonly Type type;
        public readonly bool canBeExternallyCancelled;
        public readonly Func<string, bool> isFriendlyAssembly;
        public readonly string listenerName;
        public readonly float priority;

        public EvtListenerInfo(Type type, string listenerName, float priority, bool canBeExternallyCancelled, Func<string, bool> isFriendlyAssembly) {
            this.type = type;
            this.listenerName = listenerName;
            this.priority = priority;
            this.canBeExternallyCancelled = canBeExternallyCancelled;
            this.isFriendlyAssembly = isFriendlyAssembly;
        }
        
        public bool Equals(EvtListenerInfo other) =>
            other is not null &&
            type == other.type &&
            priority == other.priority &&
            StringComparer.Ordinal.Equals(listenerName, other.listenerName);

        public override bool Equals(object obj) => obj is EvtListenerInfo other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(StringComparer.Ordinal.GetHashCode(listenerName), priority, type);
    }

    sealed class ListenerComparer : IComparer<EvtListenerInfo> {
        public int Compare(EvtListenerInfo x, EvtListenerInfo y) {
            if (x == null || y == null) {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                return 1;
            }
            // 先按 priority 降序
            int byPrio = y.priority.CompareTo(x.priority);
            if (byPrio != 0) return byPrio;
            
            // 再按 监听器名称排序
            int byName = StringComparer.Ordinal.Compare(x.listenerName, y.listenerName);
            if (byName != 0) return byName;

            // 最后按类型全名排序，避免不同类型但同名同优先级的监听器被当作重复键
            return StringComparer.Ordinal.Compare(x.type.FullName, y.type.FullName);
        }
    }
}