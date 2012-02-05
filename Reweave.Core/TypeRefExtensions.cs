using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Reweave.Core
{
    static class TypeRefExtensions
    {
        public static bool TypeMatches(this TypeReference typeRef, Type otherType)
        {
            return typeRef.Namespace == otherType.Namespace
                && typeRef.Name == otherType.Name;
        }

        public static TypeReference ImportInto(this TypeReference typeRef, ModuleDefinition module)
        {
            return module.Import(typeRef);
        }
    }
}
