using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fries.EvtsysSrcgen {
    public class EvtAttrReceiver : ISyntaxReceiver {
        public List<MethodDeclarationSyntax> candidateMethods { get; } = new List<MethodDeclarationSyntax>();
        public List<MethodDeclarationSyntax> candidateInstMethods { get; } = new List<MethodDeclarationSyntax>();
        public List<StructDeclarationSyntax> candidateStructs { get; } = new List<StructDeclarationSyntax>();
        
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            checkForMethod(syntaxNode);
            checkForInstMethod(syntaxNode);
            checkForStruct(syntaxNode);
        }
        
        private void checkForMethod(SyntaxNode syntaxNode) {
            if (!(syntaxNode is MethodDeclarationSyntax method)) return;
            if (method.AttributeLists.Count <= 0) return;
            
            foreach (var attribute in method.AttributeLists.SelectMany(a => a.Attributes)) {
                if (!attribute.Name.ToString().Contains("EvtListener")) continue;
                candidateMethods.Add(method);
                break;
            }
        }
        
        private void checkForStruct(SyntaxNode syntaxNode) {
            if (!(syntaxNode is StructDeclarationSyntax structDeclaration)) return;
            if (structDeclaration.AttributeLists.Count <= 0) return;
            
            foreach (var attribute in structDeclaration.AttributeLists.SelectMany(a => a.Attributes)) {
                if (!attribute.Name.ToString().Contains("EvtDeclarer")) continue;
                candidateStructs.Add(structDeclaration);
                break;
            }
        }
        
        private void checkForInstMethod(SyntaxNode syntaxNode) {
            if (!(syntaxNode is MethodDeclarationSyntax method)) return;
            if (method.AttributeLists.Count <= 0) return;
            
            foreach (var attribute in method.AttributeLists.SelectMany(a => a.Attributes)) {
                if (!attribute.Name.ToString().Contains("EvtCallback")) continue;
                candidateInstMethods.Add(method);
                break;
            }
        }
    }
}