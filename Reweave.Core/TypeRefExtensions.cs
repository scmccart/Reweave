using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Reweave.Core
{
    static class TypeRefExtensions
    {
        public static bool TypeMatches(this TypeReference typeRef, Type otherType)
        {
            return typeRef.Namespace == otherType.Namespace
                && typeRef.Name == otherType.Name;
        }
    }
}
