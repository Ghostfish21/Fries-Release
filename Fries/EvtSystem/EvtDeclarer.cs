using System;
using UnityEngine.Scripting;

namespace Fries.EvtSystem {
    [AttributeUsage(AttributeTargets.Struct)]
    public class EvtDeclarer : PreserveAttribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class O : PreserveAttribute {
        public readonly int order;
        public O(int order) => this.order = order;
    }
    
    public class GlobalEvt {}
}