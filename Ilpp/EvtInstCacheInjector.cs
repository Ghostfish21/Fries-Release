// # define SRCGEN_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Fries.EvtSystem;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using MethodAttributes = Mono.Cecil.MethodAttributes; // 显式引用，避免歧义

namespace Fries.Ilpp.EvtInstCacheIl {
    public class EvtInstCacheInjector : ILPostProcessor {
        private static void resetLog(string assemblyName) {
# if SRCGEN_DEBUG
            try {
                string tempDir = Path.GetTempPath();
                string logFilePath = Path.Combine(tempDir, $"{assemblyName}-Ilpp-Debug.txt");
                File.WriteAllText(logFilePath, "");
            }
            catch { }
# endif
        }
        public static void log(string message) {
# if SRCGEN_DEBUG
            try {
                string tempDir = Path.GetTempPath();
                string logFilePath = Path.Combine(tempDir, $"Ilpp-Debug.txt");
                File.AppendAllText(logFilePath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            } catch {}
# endif
        }
        public static void log(string assemblyName, string message) {
# if SRCGEN_DEBUG
            try {
                string tempDir = Path.GetTempPath();
                string logFilePath = Path.Combine(tempDir, $"{assemblyName}-Ilpp-Debug.txt");
                File.AppendAllText(logFilePath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            } catch {}
# endif
        }
        
        
        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) {
            var name = compiledAssembly.Name;

            if (name.StartsWith("Unity.") ||
                name.StartsWith("UnityEngine.") ||
                name.StartsWith("UnityEditor."))
                return false;

            if (name.Contains("NewAssembly") || name.Contains("Fries")) return true;
            
            return compiledAssembly.References.Any(r => r.EndsWith("NewAssembly.dll") || r.EndsWith("Fries.dll"));
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly) {
            try {
                resetLog(compiledAssembly.Name);
                log(compiledAssembly.Name, "Ilpp started...");

                if (!WillProcess(compiledAssembly)) {
                    log(compiledAssembly.Name, "Ilpp exited due to invalid assembly...");
                    return null;
                }

                using var stream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData);
                using var pdbStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData); 
                
                var resolver = new DefaultAssemblyResolver();
                collectAssembly(resolver, compiledAssembly);
                
                var readerParameters = new ReaderParameters {
                    AssemblyResolver = resolver,
                    ReadWrite = true,
                    ReadSymbols = true, // 2. 开启符号读取
                    SymbolStream = pdbStream, // 3. 传入原始 PDB 数据
                    SymbolReaderProvider = new PortablePdbReaderProvider() // 4. 指定读取器 (Unity 新版通常是 Portable PDB)
                };
                using var assemblyDefinition = AssemblyDefinition.ReadAssembly(stream, readerParameters);

                bool isAssemblyModified = false;
                
                // 获取目标方法
                TypeDefinition evtInstStaticDef = getEvtInstStaticDef(assemblyDefinition, resolver, compiledAssembly);
                var addMethodDef = evtInstStaticDef.Methods.First(m => m.Name == "add");
                
                // 参数一，二类引用
                var typeParamDef = addMethodDef.Parameters[0].ParameterType.Resolve(); 
                var systemTypeDef = typeParamDef;
                // typeof 等价引用
                var getTypeFromHandleDef = systemTypeDef.Methods.First(m => m.Name == "GetTypeFromHandle");
                var getTypeFromHandleRef = assemblyDefinition.MainModule.ImportReference(getTypeFromHandleDef);
                // 目标方法引用
                var addMethodRef = assemblyDefinition.MainModule.ImportReference(addMethodDef);

                // 遍历处理所有程序集
                foreach (var module in assemblyDefinition.Modules) {
                    if (ProcessModule(compiledAssembly.Name, module, addMethodRef, getTypeFromHandleRef))
                        isAssemblyModified = true;
                }

                if (!isAssemblyModified) {
                    log(compiledAssembly.Name, "Ilpp exited due to no changes...");
                    return null;
                }

                var pe = new MemoryStream();
                var pdb = new MemoryStream();
                var writerParameters = new WriterParameters {
                    SymbolWriterProvider = new PortablePdbWriterProvider(),
                    WriteSymbols = true,
                    SymbolStream = pdb
                };
                assemblyDefinition.Write(pe, writerParameters);

                return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()),
                    new List<DiagnosticMessage>());
            } catch (Exception e) {
                log(compiledAssembly.Name, e.ToString());
                log(compiledAssembly.Name, "Ilpp exited due to exception...");
                return null;
            }
        }

        private TypeDefinition getEvtInstStaticDef(AssemblyDefinition assemblyDefinition, DefaultAssemblyResolver resolver, ICompiledAssembly compiledAssembly) {
            var evtInstStaticDef = assemblyDefinition.MainModule.GetType("Fries.EvtSystem.EvtInstStatic");

            if (evtInstStaticDef == null) {
                var friesAssemblyRef = assemblyDefinition.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name.Contains("Fries") || r.Name.Contains("NewAssembly"));
                if (friesAssemblyRef != null) {
                    var friesAssemblyDef = resolver.Resolve(friesAssemblyRef);
                    evtInstStaticDef = friesAssemblyDef.MainModule.GetType("Fries.EvtSystem.EvtInstStatic");
                }
            }
            if (evtInstStaticDef == null) {
                log(compiledAssembly.Name, "Skipping: Could not find Fries.EvtSystem.EvtInstStatic definition.");
                return null; 
            }
            
            return evtInstStaticDef;
        }

        private void collectAssembly(BaseAssemblyResolver resolver, ICompiledAssembly compiledAssembly) {
            var searchDirs = new HashSet<string>();
            foreach (var refPath in compiledAssembly.References) {
                try {
                    var dir = Path.GetDirectoryName(refPath);
                    if (!string.IsNullOrEmpty(dir)) searchDirs.Add(dir);
                }
                catch (Exception e) {
                    log(compiledAssembly.Name, "Failed to get assembly location: " + e);
                }
            }
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) continue;
                    var dir = Path.GetDirectoryName(asm.Location);
                    if (!string.IsNullOrEmpty(dir)) searchDirs.Add(dir);
                }
                catch (Exception e) {
                    log(compiledAssembly.Name, "Failed to get assembly location: " + e);
                }
            }
            foreach (var dir in searchDirs) 
                resolver.AddSearchDirectory(dir);
        }

        private bool ProcessModule(string assemblyName, ModuleDefinition module, MethodReference addMethod, MethodReference getTypeFromHandle) {
            bool isModuleModified = false;

            log(assemblyName, $"Ilpp processing module {module.Name}...");

            // 遍历所有类
            foreach (var typeDef in module.Types) {
                // 是接口的话就返回 - 接口没有构造函数
                if (typeDef.IsInterface) continue;

                // 收集该类所有需要注册的 EventType
                var eventTypesToInject = new List<TypeReference>();
                var capturedEvents = new HashSet<string>();

                // 遍历所有方法，搜索含有目标 Attribute 的方法
                foreach (var method in typeDef.Methods) {
                    // 不处理静态方法
                    if (method.IsStatic) continue;

                    var attr = GetCustomAttribute(method, evtCallbackType.FullName);
                    if (attr == null) continue;

                    log(assemblyName, $"Found evt callback method {method.FullName}!"); 
                    
                    // 获取 AreInstManaged 值，不管理的话退出
                    if (!GetAttributeBoolField(attr, 2)) {
                        log(assemblyName, "Managed insts flag is off, skipping...");
                        continue;
                    }

                    log(assemblyName, "Found evt callback method with managed insts flag on!");
                    
                    // 从 EvtCallback 中获取事件的类型
                    if (attr.ConstructorArguments.Count <= 0 ||
                        attr.ConstructorArguments[0].Value is not TypeReference targetTypeRef) continue;
                    if (capturedEvents.Add(targetTypeRef.FullName)) 
                        eventTypesToInject.Add(targetTypeRef);
                }

                if (eventTypesToInject.Count == 0) continue;

                // 判断目标 object 是否是 Unity Object。是的话注入在 Awake 中
                // 这里应该替换为 更精细的 Unity类型检查
                // MonoBehaviour -> 注入至 Awake
                // ScriptableObject -> 注入至 OnEnable
                // 其他 Unity Object -> 不支持
                // System.Object -> ctor 的开头
                if (isInstanceOf(typeDef, "UnityEngine.MonoBehaviour")) {
                    log(assemblyName, $"Type {typeDef.Name} is a Unity MonoBehaviour. Injecting into Awake.");
                    var awakeMethod = getOrCreateMethod(typeDef, module, "Awake");
                    // 注入到 Awake 开头 (injectAtStart = true)
                    if (InjectCode(module, awakeMethod, typeDef, eventTypesToInject, addMethod, getTypeFromHandle, injectAtStart: true))
                        isModuleModified = true;
                }
                else if (isInstanceOf(typeDef, "UnityEngine.ScriptableObject")) {
                    log(assemblyName, $"Type {typeDef.Name} is a Unity ScriptableObject. Injecting into OnEnable.");
                    var awakeMethod = getOrCreateMethod(typeDef, module, "OnEnable");
                    // 注入到 Awake 开头 (injectAtStart = true)
                    if (InjectCode(module, awakeMethod, typeDef, eventTypesToInject, addMethod, getTypeFromHandle, injectAtStart: true))
                        isModuleModified = true;
                }
                else if (isInstanceOf(typeDef, "UnityEngine.Object")) {
                    log(assemblyName, $"Type {typeDef.Name} is a Unity Object. Injecting is unsupported, skipping...");
                }
                else {
                    log(assemblyName, $"Type {typeDef.Name} is a Standard Class. Injecting into Constructor.");

                    if (!hasEqualityOperator(typeDef)) {
                        log(assemblyName, $"Type {typeDef.Name} is a Standard Class with no equality overload is unsupported, skipping...");
                        continue;
                    }
                    foreach (var ctor in typeDef.Methods) {
                        if (!ctor.IsConstructor || ctor.IsStatic) continue;
                        if (isDelegatingToThisCtor(ctor, typeDef)) continue;
                        if (InjectCode(module, ctor, typeDef, eventTypesToInject, addMethod, getTypeFromHandle, injectAtStart: false)) 
                            isModuleModified = true;
                    }
                }
            }

            return isModuleModified;
        }

        private static bool isDelegatingToThisCtor(MethodDefinition ctor, TypeDefinition typeDef) {
            if (!ctor.HasBody) return false;
            var instructions = ctor.Body.Instructions;

            foreach (var instr in instructions) {
                // 找到第一个对 .ctor 的 call
                if (instr.OpCode != OpCodes.Call && instr.OpCode != OpCodes.Callvirt) continue;
                if (instr.Operand is MethodReference { Name: ".ctor" } mr) {
                    var declaringType = mr.DeclaringType.Resolve();
                    // 同类链式调用返回 True, 基类继承调用返回 False
                    // 基类的构造器不会能够通过 typeDef 扫到，所以这里这样做是安全的
                    return declaringType == typeDef;
                }
                break;
            }
            return false;
        }

        private static bool hasEqualityOperator(TypeDefinition typeDef) {
            for (var current = typeDef; current != null; current = current.BaseType?.Resolve()) {
                foreach (var m in current.Methods) {
                    if (!m.IsStatic) continue;
                    if (!m.IsSpecialName || !m.IsHideBySig) continue;
                    if (m.Name != "op_Equality") continue;
                    if (m.Parameters.Count != 2) continue;
                    
                    var p0 = m.Parameters[0].ParameterType.FullName;
                    var p1 = m.Parameters[1].ParameterType.FullName;

                    // 一般 C# 自定义 == 都是 (T, T)
                    if (p0 == current.FullName && p1 == current.FullName)
                        return true;
                }
            }

            return false;
        }

        private bool isInstanceOf(TypeDefinition typeDef, string typeFullName) {
            var current = typeDef;
            while (current != null) {
                if (current.FullName == typeFullName) return true;
                
                try {
                    if (current.BaseType == null) break;
                    current = current.BaseType.Resolve();
                } catch {
                    // 如果无法解析基类（比如在其他程序集且未加载），则停止查找
                    break;
                }
            }
            return false;
        }

        private MethodDefinition getOrCreateMethod(TypeDefinition typeDef, ModuleDefinition module, string methodName) {
            var awake = typeDef.Methods.FirstOrDefault(m => m.Name == methodName && m.Parameters.Count == 0);
            if (awake != null) return awake;

            var methodAttributes = MethodAttributes.Private | MethodAttributes.HideBySig;
            awake = new MethodDefinition(methodName, methodAttributes, module.TypeSystem.Void);
            
            awake.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            typeDef.Methods.Add(awake);
            return awake;
        }

        private bool InjectCode(ModuleDefinition module, MethodDefinition method, TypeDefinition instType, List<TypeReference> eventTypes, MethodReference addMethod, MethodReference getTypeFromHandle, bool injectAtStart) {
            method.Body.SimplifyMacros();
            
            var processor = method.Body.GetILProcessor();
            var instructions = method.Body.Instructions;

            Instruction injectionPoint = null;

            if (injectAtStart) {
                // 如果是 Awake，注入点直接是第一条指令
                if (instructions.Count > 0)
                    injectionPoint = instructions[0];
                else {
                    // 理论上不会走到这里，因为即便是新建的 Awake 也有 ret，但防个万一
                    var ret = processor.Create(OpCodes.Ret);
                    processor.Append(ret);
                    injectionPoint = ret;
                }
            }
            else {
                // 如果是构造函数，寻找 base..ctor 调用后
                for (int i = 0; i < instructions.Count; i++) {
                    var instr = instructions[i];
                    if (instr.OpCode != OpCodes.Call || instr.Operand is not MethodReference methodRef) continue;
                    if (methodRef.Name != ".ctor") continue;

                    var methodDef = methodRef.Resolve();
                    var baseTypeDef = instType.BaseType?.Resolve();

                    if (methodDef == null || baseTypeDef == null || methodDef.DeclaringType != baseTypeDef) 
                        continue; 

                    injectionPoint = instr.Next; 
                    break;
                }
            }
            if (injectionPoint == null && !injectAtStart) return false;
            
            foreach (var evtType in eventTypes) {
                var ilList = new List<Instruction>();
                var importedEvtType = module.ImportReference(evtType);
                var importedInstType = module.ImportReference(instType);

                // 1. eventType (Type)
                ilList.Add(processor.Create(OpCodes.Ldtoken, importedEvtType));
                ilList.Add(processor.Create(OpCodes.Call, getTypeFromHandle));
                
                // 2. instType (Type) -> 使用编译时类型 (typeof(A))
                ilList.Add(processor.Create(OpCodes.Ldtoken, importedInstType));
                ilList.Add(processor.Create(OpCodes.Call, getTypeFromHandle));
                
                // 3. obj (object) -> this
                ilList.Add(processor.Create(OpCodes.Ldarg_0));

                ilList.Add(processor.Create(OpCodes.Call, addMethod));

                // 执行插入
                if (injectionPoint != null) {
                    foreach (var instruction in ilList) 
                        processor.InsertBefore(injectionPoint, instruction);
                } else {
                    // 只有当 injectionPoint 为空（极其罕见的情况），直接 Append
                    foreach (var instruction in ilList) 
                        processor.Append(instruction);
                }
            }

            method.Body.OptimizeMacros();
            
            return true;
        }
        
        private CustomAttribute GetCustomAttribute(MethodDefinition method, string attributeFullName) {
            if (!method.HasCustomAttributes) return null;
    
            return method.CustomAttributes.FirstOrDefault(a => 
                a.AttributeType.FullName == attributeFullName
            );
        }

        private bool GetAttributeBoolField(CustomAttribute attr, int i) {
            if (attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[i].Value is bool val) 
                return val;
            return false;
        }

        private static Type evtCallbackType => typeof(EvtCallback);
    }
}