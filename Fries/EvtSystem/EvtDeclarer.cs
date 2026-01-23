using System;
using UnityEngine.Scripting;

namespace Fries.EvtSystem {
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class EvtDeclarer : PreserveAttribute { }
    
    public class GlobalEvt {}
}