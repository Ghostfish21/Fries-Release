using System;
using UnityEngine;

namespace Fries.EvtSystem {
    public class EvtHandle {
        private static readonly Func<EvtHandle, bool> defaultFunc = _ => true;
        
        public readonly ReadonlyEvtInfo eventInfo;
        public EvtHandle(ReadonlyEvtInfo eventInfo) => this.eventInfo = eventInfo;

        public void reset() {
            nextListener = null;
            shouldProcessEvt = defaultFunc;
            shouldProcessRegisterAssemblyFullname = null;
        }
        
        internal EvtListenerInfo nextListener = null;
        private string shouldProcessRegisterAssemblyFullname = null;
        private Func<EvtHandle, bool> shouldProcessEvt = defaultFunc;
        public void setShouldProcess(Func<EvtHandle, bool> shouldProcessEvt) {
            if (shouldProcessEvt.Method.DeclaringType == null) {
                Debug.LogWarning("You can't register a Process Check method without declaring type!");
                return;
            }
            shouldProcessRegisterAssemblyFullname = shouldProcessEvt.Method.DeclaringType.Assembly.FullName;

            this.shouldProcessEvt = shouldProcessEvt;
        }

        public bool shouldProcess() {
            if (shouldProcessEvt == null) return true;
            try {
                if (!nextListener.canBeExternallyCancelled) return true;
                if (!nextListener.isFriendlyAssembly(shouldProcessRegisterAssemblyFullname)) return true;
                
                bool result = shouldProcessEvt(this);
                return result;
            }
            catch (Exception ex) {
                Debug.LogWarning($"EvtHandle {this} failed to process: {ex.Message}");
                return true;
            }
        }
    }
}