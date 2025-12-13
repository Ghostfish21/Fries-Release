// # define SRCGEN_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fries.EvtsysSrcgen {

    [Generator]
    public class AttributeUsageCollector : ISourceGenerator {
        private const string VERSION = "1.2";
        
        private static void resetLog(string assemblyName) {
# if SRCGEN_DEBUG
            try {
                string tempDir = Path.GetTempPath();
                string logFilePath = Path.Combine(tempDir, $"{assemblyName}-EvtsysSrcgen-{VERSION}-Debug.txt");
                File.WriteAllText(logFilePath, "");
            }
            catch { }
# endif
        }
        public static void log(string message) {
# if SRCGEN_DEBUG
            try {
                string tempDir = Path.GetTempPath();
                string logFilePath = Path.Combine(tempDir, $"EvtsysSrcgen-{VERSION}-Debug.txt");
                File.AppendAllText(logFilePath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            } catch {}
# endif
        }
        public static void log(string assemblyName, string message) {
# if SRCGEN_DEBUG
            try {
                string tempDir = Path.GetTempPath();
                string logFilePath = Path.Combine(tempDir, $"{assemblyName}-EvtsysSrcgen-{VERSION}-Debug.txt");
                File.AppendAllText(logFilePath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            } catch {}
# endif
        }

        public void Initialize(GeneratorInitializationContext context) {
            context.RegisterForSyntaxNotifications(() => new EvtAttrReceiver());
        }

        public void Execute(GeneratorExecutionContext context) { 
            string assemblyName = context.Compilation.AssemblyName;
            assemblyName = AssemblyNameUtils.toValidClassName(assemblyName);
            resetLog(assemblyName);
            log(assemblyName, "Source Generator starting to work...");

            var symbolDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine("using Fries.EvtSystem;"); 
            sb.AppendLine("namespace Fries.EvtSystem {");
            sb.AppendLine($"    public class {assemblyName}EvtInitializer : Fries.EvtSystem.EvtInitializer {{");
            sb.AppendLine("        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]");
            sb.AppendLine("        private static void create() { ");
            sb.AppendLine($"            var initializer = new {assemblyName}EvtInitializer();");
            sb.AppendLine("            Fries.EvtSystem.EvtInitializer.register(initializer);");
            sb.AppendLine("        }");
            sb.AppendLine($"        public {assemblyName}EvtInitializer() : base() {{}}");
            
            Compilation compilation = context.Compilation;
            try {
                if (!(context.SyntaxReceiver is EvtAttrReceiver receiver)) {
                    log(assemblyName, "Source Generator quit working because this is not the correct SyntaxReceiver!");
                    return;
                }
                
                object targetAttr = compilation.GetTypeByMetadataName("Fries.EvtSystem.EvtCallback");
                if (targetAttr == null) {
                    log(assemblyName, "Cannot find EvtCallback type, did you delete it by accident? Terminating...");
                    return;
                }
                INamedTypeSymbol evtCallbackAttrSymbol = (INamedTypeSymbol)targetAttr;
                createListener4Callbacks(evtCallbackAttrSymbol, receiver, context, assemblyName, sb, symbolDisplayFormat);
                
                object symbol4Method = compilation.GetTypeByMetadataName("Fries.EvtSystem.EvtListener");
                object symbol4Struct = compilation.GetTypeByMetadataName("Fries.EvtSystem.EvtDeclarer");

                if (symbol4Method == null) {
                    log(assemblyName, "Cannot find EvtListener type, did you delete it by accident? Terminating...");
                    return;
                }
                if (symbol4Struct == null) {
                    log(assemblyName, "Cannot find EvtDeclarer type, did you delete it by accident? Terminating...");
                    return;
                }
                INamedTypeSymbol targetAttrSymbol4Method = (INamedTypeSymbol)symbol4Method;
                INamedTypeSymbol targetAttrSymbol4Struct = (INamedTypeSymbol)symbol4Struct;
                
                log(assemblyName, $"Detected {receiver.candidateMethods.Count} methods with EvtListener attribute..."); 
                log(assemblyName, $"Detected {receiver.candidateStructs.Count} structs with EvtDeclarer attribute..."); 
                handleListener(receiver, context, assemblyName, sb, targetAttrSymbol4Method, evtCallbackAttrSymbol, symbolDisplayFormat);
                handleEvent(receiver, context, assemblyName, sb, targetAttrSymbol4Struct, symbolDisplayFormat);
                

                sb.AppendLine("    }");
                sb.AppendLine("}");

                context.AddSource($"{assemblyName}EvtInitializer.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

                log(assemblyName, "Code generated: \n" + sb);
            }
            catch (Exception e) {
                log(assemblyName, "Encountered exception during code generation: " + e);
            }
        }

        private void createListener4Callbacks(INamedTypeSymbol targetAttrSymbol, EvtAttrReceiver receiver, GeneratorExecutionContext context, string assemblyName, StringBuilder sb, SymbolDisplayFormat symbolDisplayFormat) {
            Compilation compilation = context.Compilation;
            int o = 0;
            foreach (var receiverCandidateInstMethod in receiver.candidateInstMethods) {
                o++;
                SemanticModel model = compilation.GetSemanticModel(receiverCandidateInstMethod.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(receiverCandidateInstMethod);
                if (symbol == null) {
                    log(assemblyName, $"Could not get inst method symbol of model {model}, skipping...");
                    continue;
                }
                
                if (!(symbol is IMethodSymbol methodInfo)) {
                    log(assemblyName, $"Could not get valid inst method symbol of model {model}, skipping...");
                    continue;
                }
                
                if (methodInfo.IsStatic) continue;
                
                var attributes = methodInfo.GetAttributes();

                AttributeData ad = null;
                foreach (var attribute in attributes) {
                    if (attribute.AttributeClass == null) continue;
                    if (!attribute.AttributeClass.Equals(targetAttrSymbol, SymbolEqualityComparer.Default))
                        continue;
                    ad = attribute;
                    break;
                }

                if (ad == null) {
                    log(assemblyName,
                        $"Turns out method {methodInfo.Name} does not have the target EvtCallback attribute, skipping...");
                    continue;
                }
                
                ITypeSymbol evtTypeSymbol = ad.ConstructorArguments[0].Value as ITypeSymbol;
                if (evtTypeSymbol == null) {
                    log(assemblyName, $"Inst Method {methodInfo.Name}'s Event Type Symbol is null! Skipping...");
                    continue;
                }

                string evtTypeFullName = evtTypeSymbol.ToDisplayString(symbolDisplayFormat);
                bool isManaged = getArgValue<bool>(ad, 2, "areInstsManaged", true);

                string rawClassFullName = methodInfo.ContainingType.ToDisplayString(symbolDisplayFormat);
                string classFullName = AssemblyNameUtils.toValidClassName(rawClassFullName);
                string rawMethodName = methodInfo.Name;
                string methodName = AssemblyNameUtils.toValidClassName(rawMethodName);

                // TODO 前面加 this.GetType()
                string parameterTypes = "";
                string parameterTypeofs = "";
                string parametersDeclaration = "";
                string parameters = "";
                if (methodInfo.Parameters.Length > 0) {
                    int i = 0;
                    foreach (var methodInfoParameter in methodInfo.Parameters) {
                        i++;
                        parameterTypes += methodInfoParameter.Type.ToDisplayString(symbolDisplayFormat) + ", ";
                        parameterTypeofs += $"typeof({methodInfoParameter.Type.ToDisplayString(symbolDisplayFormat)}), ";
                        parametersDeclaration += methodInfoParameter.Type.ToDisplayString(symbolDisplayFormat) + $" arg{i}, ";
                        parameters += $"arg{i}, ";
                    }
                    parameterTypes = parameterTypes.Substring(0, parameterTypes.Length - 2);
                    parametersDeclaration = parametersDeclaration.Substring(0, parametersDeclaration.Length - 2);
                    parameters = parameters.Substring(0, parameters.Length - 2);
                    parameterTypeofs = parameterTypeofs.Substring(0, parameterTypeofs.Length - 2);
                }

                bool isPublic = methodInfo.DeclaredAccessibility == Accessibility.Public;
                if (!isPublic) {
                    parameterTypes = $"{rawClassFullName}, " + parameterTypes;
                    parameters = "elem, " + parameters;
                }

                // 当不是 public 的时候，使用反射获取实例 MethodInfo 时，parameterTypes 注定非空
                if (!isPublic) {
                    sb.AppendLine($"        private static Action<{parameterTypes}> {classFullName}{methodName}{o}Action;");
                    sb.AppendLine($"        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
                    sb.AppendLine($"        private static void {classFullName}{methodName}{o}Resetter() {{"); 
                    sb.AppendLine($"            MethodInfo methodInfo = typeof({rawClassFullName}).GetMethod(\"{rawMethodName}\", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] {{ {parameterTypeofs} }}, null);");
                    sb.AppendLine($"            {classFullName}{methodName}{o}Action = (Action<{parameterTypes}>) Delegate.CreateDelegate(typeof(Action<{parameterTypes}>), methodInfo);");
                    sb.AppendLine($"        }}");
                }
                sb.AppendLine($"        private static void {classFullName}{methodName}{o}({parametersDeclaration}) {{");
                
                if (!isManaged) {
                    sb.AppendLine($"            foreach (var elem in EvtInstCache<{evtTypeFullName}, {rawClassFullName}>.insts) {{");
                    
                    if (isPublic) sb.AppendLine($"                try {{ elem.{rawMethodName}({parameters}); }}");
                    else sb.AppendLine($"                try {{ {classFullName}{methodName}{o}Action({parameters}); }}");
                    
                    sb.AppendLine("                catch (Exception e) { Debug.LogError($\"Caught error during calling callbacks: {e}\"); }");
                    sb.AppendLine("            }");
                }
                else {
                    sb.AppendLine($"            List<{rawClassFullName}> toRemoves = new();");
                    sb.AppendLine($"            foreach (var elem in EvtInstCache<{evtTypeFullName}, {rawClassFullName}>.insts) {{");
                    sb.AppendLine("                if (elem == null) {");
                    sb.AppendLine("                    toRemoves.Add(elem);");
                    sb.AppendLine("                    continue;");
                    sb.AppendLine("                }");
                    
                    if (isPublic) sb.AppendLine($"                try {{ elem.{rawMethodName}({parameters}); }}");
                    else sb.AppendLine($"                try {{ {classFullName}{methodName}{o}Action({parameters}); }}");
                    
                    sb.AppendLine("                catch (Exception e) { Debug.LogError($\"Caught error during calling callbacks: {e}\"); }");
                    sb.AppendLine("            }");
                    sb.AppendLine("            foreach (var inst in toRemoves) {");
                    sb.AppendLine($"                try {{ EvtInstCache<{evtTypeFullName}, {rawClassFullName}>.remove(inst); }}");
                    sb.AppendLine("                catch (Exception e) { Debug.LogError($\"Caught exception when evict cached inst {e}\"); }");
                    sb.AppendLine("            }");
                }
                sb.AppendLine("        }");
            }
        }
        
        private void handleListener(EvtAttrReceiver receiver, GeneratorExecutionContext context, string assemblyName, StringBuilder sb, INamedTypeSymbol evtListenerAttrSymbol, INamedTypeSymbol evtCallbackAttrSymbol, SymbolDisplayFormat symbolDisplayFormat) {
            sb.AppendLine("        protected override void init(Action<string, Type, EvtListener, Delegate> registerEvtListenerByInfo, Action<MethodInfo> registerEvtListenerByReflection) {");
            sb.AppendLine("            base.init(registerEvtListenerByInfo, registerEvtListenerByReflection);");
            sb.AppendLine("            EvtListener listener;");
            
            Compilation compilation = context.Compilation;
            
            List<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();
            methods.AddRange(receiver.candidateInstMethods);
            methods.AddRange(receiver.candidateMethods);

            int o = 0;
            foreach (var method in methods) {
                o++;
                SemanticModel model = compilation.GetSemanticModel(method.SyntaxTree);
                object methodSymbol = model.GetDeclaredSymbol(method);
                if (methodSymbol == null) {
                    log(assemblyName, $"Could not get method symbol of model {model}, skipping...");
                    continue;
                }

                if (!(methodSymbol is IMethodSymbol methodInfo)) {
                    log(assemblyName, $"Could not get valid method symbol of model {model}, skipping...");
                    continue;
                }

                var attributes = methodInfo.GetAttributes();

                AttributeData ad = null;
                foreach (var attribute in attributes) {
                    if (attribute.AttributeClass == null) continue;
                    if (!attribute.AttributeClass.Equals(evtListenerAttrSymbol, SymbolEqualityComparer.Default) 
                        && !attribute.AttributeClass.Equals(evtCallbackAttrSymbol, SymbolEqualityComparer.Default))
                        continue;
                    ad = attribute;
                    break;
                }

                if (ad == null) {
                    log(assemblyName,
                        $"Turns out method {methodInfo.Name} does not have the target EvtListener attribute, skipping...");
                    continue;
                }

                if (ad.AttributeClass.Equals(evtListenerAttrSymbol, SymbolEqualityComparer.Default)) 
                    processEvtListener(ad, assemblyName, methodInfo, sb, symbolDisplayFormat);
                else if (ad.AttributeClass.Equals(evtCallbackAttrSymbol, SymbolEqualityComparer.Default)) 
                    processEvtCallback(ad, assemblyName, methodInfo, sb, symbolDisplayFormat, o);

                sb.AppendLine();
            }
            
            sb.AppendLine("        }");
        }

        private void processEvtListener(AttributeData ad, string assemblyName, IMethodSymbol methodInfo,
            StringBuilder sb, SymbolDisplayFormat symbolDisplayFormat) {
            if (!methodInfo.IsStatic) return;

            ITypeSymbol evtTypeSymbol = ad.ConstructorArguments[0].Value as ITypeSymbol;
            if (evtTypeSymbol == null) {
                log(assemblyName, $"Method {methodInfo.Name}'s Event Type Symbol is null! Skipping...");
                return;
            }

            string evtTypeFullName = evtTypeSymbol.ToDisplayString(symbolDisplayFormat);
            float priority = getArgValue<float>(ad, 1, "priority", 0f);
            bool canCancel = getArgValue<bool>(ad, 2, "canBeExternallyCancelled", false);

            string friendAssembliesCode = "null";
            var friendsArg = GetArgTypedConstant(ad, 3, "friendAssemblies");
            if (!friendsArg.IsNull) {
                var values = friendsArg.Values.Select(v => $"\"{v.Value}\"");
                friendAssembliesCode = $"new string[] {{ {string.Join(", ", values)} }}";
            }

            string priorityCode = priority + "f";
            string canCancelCode = canCancel.ToString().ToLower();

            string classFullName = methodInfo.ContainingType.ToDisplayString(symbolDisplayFormat);
            string methodName = methodInfo.Name;
            
            if (methodInfo.DeclaredAccessibility == Accessibility.Public) {
                sb.AppendLine(
                    $"            listener = new EvtListener(typeof({evtTypeFullName}), {priorityCode}, {canCancelCode}, {friendAssembliesCode});");
                if (methodInfo.Parameters.Length > 0) {
                    string paramTypeFullName = "";
                    foreach (var methodInfoParameter in methodInfo.Parameters)
                        paramTypeFullName +=
                            methodInfoParameter.Type.ToDisplayString(symbolDisplayFormat) + ", ";
                    paramTypeFullName = paramTypeFullName.Substring(0, paramTypeFullName.Length - 2);
                    sb.AppendLine(
                        $"            this.registerEvtListenerByInfo(\"{methodName}\", typeof({classFullName}), listener, new Action<{paramTypeFullName}>({classFullName}.{methodName}));");
                }
                else
                    sb.AppendLine(
                        $"            this.registerEvtListenerByInfo(\"{methodName}\", typeof({classFullName}), listener, new Action({classFullName}.{methodName}));");
            }
            else {
                string bindingFlags = "BindingFlags.NonPublic | BindingFlags.Static";
                sb.AppendLine(
                    $"            this.registerEvtListenerByReflection(typeof({classFullName}).GetMethod(\"{methodName}\", {bindingFlags}));");
            }
        }

        private void processEvtCallback(AttributeData ad, string assemblyName, IMethodSymbol methodInfo,
            StringBuilder sb, SymbolDisplayFormat symbolDisplayFormat, int o) {
            if (methodInfo.IsStatic) return;

            ITypeSymbol evtTypeSymbol = ad.ConstructorArguments[0].Value as ITypeSymbol;
            if (evtTypeSymbol == null) {
                log(assemblyName, $"Method {methodInfo.Name}'s Event Type Symbol is null! Skipping...");
                return;
            }
            
            string evtTypeFullName = evtTypeSymbol.ToDisplayString(symbolDisplayFormat);
            float priority = getArgValue<float>(ad, 1, "priority", 0f);
            bool canCancel = getArgValue<bool>(ad, 3, "canBeExternallyCancelled", false);
                
            string friendAssembliesCode = "null";
            var friendsArg = GetArgTypedConstant(ad, 4, "friendAssemblies");
            if (!friendsArg.IsNull) {
                var values = friendsArg.Values.Select(v => $"\"{v.Value}\"");
                friendAssembliesCode = $"new string[] {{ {string.Join(", ", values)} }}";
            }

            string priorityCode = priority + "f";
            string canCancelCode = canCancel.ToString().ToLower();

            string rawClassFullName = methodInfo.ContainingType.ToDisplayString(symbolDisplayFormat);
            string classFullName = AssemblyNameUtils.toValidClassName(rawClassFullName);
            string rawMethodName = methodInfo.Name;
            string methodName = AssemblyNameUtils.toValidClassName(rawMethodName);
            string finalMethodName = $"{classFullName}{methodName}{o}";
            string thisClassName = $"{assemblyName}EvtInitializer";
            
            sb.AppendLine($"            listener = new EvtListener(typeof({evtTypeFullName}), {priorityCode}, {canCancelCode}, {friendAssembliesCode});");
            if (methodInfo.Parameters.Length > 0) {
                string paramTypeFullName = "";
                foreach (var methodInfoParameter in methodInfo.Parameters)
                    paramTypeFullName +=
                        methodInfoParameter.Type.ToDisplayString(symbolDisplayFormat) + ", ";
                paramTypeFullName = paramTypeFullName.Substring(0, paramTypeFullName.Length - 2);
                sb.AppendLine(
                    $"            this.registerEvtListenerByInfo(\"{finalMethodName}\", typeof({thisClassName}), listener, new Action<{paramTypeFullName}>({finalMethodName}));");
            }
            else
                sb.AppendLine(
                    $"            this.registerEvtListenerByInfo(\"{finalMethodName}\", typeof({thisClassName}), listener, new Action({finalMethodName}));");
        }

        private void handleEvent(EvtAttrReceiver declarer, GeneratorExecutionContext context, string assemblyName, StringBuilder sb, INamedTypeSymbol targetAttrSymbol, SymbolDisplayFormat symbolDisplayFormat) {
            sb.AppendLine("        protected override void declare(Action<Type, Type[]> registerEventByType) {");
            sb.AppendLine("            base.declare(registerEventByType);");
            
            Compilation compilation = context.Compilation;
            foreach (var structDeclaration in declarer.candidateStructs) {
                var model = compilation.GetSemanticModel(structDeclaration.SyntaxTree);
                object structt = model.GetDeclaredSymbol(structDeclaration);
                if (structt == null) {
                    log(assemblyName, $"Could not get struct symbol of model {model}, skipping...");
                    continue;
                }

                if (!(structt is INamedTypeSymbol structSymbol)) {
                    log(assemblyName, $"Could not get valid struct symbol of model {model}, skipping...");
                    continue;
                }

                var attributes = structSymbol.GetAttributes();

                AttributeData ad = null;
                foreach (var attribute in attributes) {
                    if (attribute.AttributeClass == null) continue;
                    if (!attribute.AttributeClass.Equals(targetAttrSymbol, SymbolEqualityComparer.Default))
                        continue;
                    ad = attribute;
                    break;
                }

                if (ad == null) {
                    log(assemblyName,
                        $"Turns out struct {structSymbol.Name} does not have the target EvtDeclarer attribute, skipping...");
                    continue;
                }

                bool b = false;
                string typeNames = "";
                foreach (var structDeclarationMember in structDeclaration.Members) {
                    if (!(structDeclarationMember is FieldDeclarationSyntax fieldDeclarationSyntax)) continue;
                    foreach (var varDeclareSyntax in fieldDeclarationSyntax.Declaration.Variables) {
                        if (!(model.GetDeclaredSymbol(varDeclareSyntax) is IFieldSymbol sym)) continue;
                        typeNames += $"typeof({sym.Type.ToDisplayString(symbolDisplayFormat)}), ";
                        b = true;
                    }
                }
                if (b) typeNames = typeNames.Substring(0, typeNames.Length - 2);
                string typeArray = "new Type[] {" + typeNames + "}";
                
                string structTypeFullName = structSymbol.ToDisplayString(symbolDisplayFormat);
                sb.AppendLine($"            this.registerEventByType(typeof({structTypeFullName}), {typeArray});");

                sb.AppendLine();
            }

            sb.AppendLine("        }");
        }

        private T getArgValue<T>(AttributeData data, int position, string name, T defaultValue) {
            TypedConstant constant = GetArgTypedConstant(data, position, name);
            if (constant.IsNull) return defaultValue;
            return (T)constant.Value;
        }

        private TypedConstant GetArgTypedConstant(AttributeData data, int position, string name) {
            // 1. 优先检查命名参数 (e.g. priority: 5)
            foreach (var namedArg in data.NamedArguments) {
                if (namedArg.Key == name) return namedArg.Value;
            }
            // 2. 检查位置参数 (e.g. [Attr(type, 5)])
            if (data.ConstructorArguments.Length > position) 
                return data.ConstructorArguments[position];
            return default;
        }
    }
}