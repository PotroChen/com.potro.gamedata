using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal static class CodeDomUtils
{
    public static void AddUsingNameSapce(CodeCompileUnit compileUnit,ICollection<string> usingNamespaces)
    {
        CodeNamespace emptyNamespace = null;

        HashSet<string> rootNamespaces = new HashSet<string>();
        foreach (var ns in compileUnit.Namespaces)
        {
            var curNs = ns as CodeNamespace;
            if (curNs.Name == "")
            {
                emptyNamespace = curNs;
            }
            rootNamespaces.Add(curNs.Name);
        }
        if (emptyNamespace == null)
        {
            emptyNamespace = new CodeNamespace();
            compileUnit.Namespaces.Add(emptyNamespace);
        }

        if (usingNamespaces != null && usingNamespaces.Count > 0)
        {
            foreach (var ns in usingNamespaces) 
            {
                if (!rootNamespaces.Contains(ns))
                    emptyNamespace.Imports.Add(new CodeNamespaceImport(ns));
            }
        }
    }
}
