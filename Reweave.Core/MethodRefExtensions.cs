using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Reweave.Core
{
    static class MethodRefExtensions
    {
        //By Gábor Kozár on http://groups.google.com/group/mono-cecil/browse_thread/thread/9826ade54b91e3b2
        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] arguments)
        {
            var reference = new MethodReference(
                self.Name,
                self.ReturnType,
                self.DeclaringType.MakeGenericInstanceType(arguments))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var generic_parameter in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

            return reference;
        } 

        public static MethodReference ImportInto(this MethodReference self, ModuleDefinition module)
        {
            return module.Import(self);
        }
    }
}
