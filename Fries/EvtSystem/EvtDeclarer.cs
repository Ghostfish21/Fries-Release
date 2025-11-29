using System;

namespace Fries.EvtSystem {
    [AttributeUsage(AttributeTargets.Struct)]
    public class EvtDeclarer : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class O : Attribute {
        public readonly int order;
        public O(int order) => this.order = order;
    }
    
    public class GlobalEvt {}
}