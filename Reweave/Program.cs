using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Reweave
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: Reweave <target assembly>");
                return;
            }

            var targetAsm = args[0];

            if (!File.Exists(targetAsm))
            {
                Console.WriteLine("Target \"{0}\" not found.");
                return;
            }

            var module = ModuleDefinition.ReadModule(targetAsm, new ReaderParameters()
            {
                ReadSymbols = true
            });

            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    var aspects = new List<CustomAttribute>();

                    foreach (var attr in method.CustomAttributes)
                    {
                        var attrName = attr.AttributeType.Name;

                        if (attrName.EndsWith("AspectAttribute"))
                        {
                            Console.WriteLine("Found Aspect {0} on {1}.{2}", attrName, type.Name, method.Name);
                            aspects.Add(attr);
                        }
                    }

                    WeaveAspects(method, aspects);
                }
            }

            module.Write(targetAsm, new WriterParameters()
            {
                WriteSymbols = true
            });

            Console.WriteLine("Executing...");

            AppDomain.CurrentDomain.ExecuteAssembly(targetAsm);

            Console.Write("Done");
            Console.ReadKey();
        }

        private static void WeaveAspects(MethodDefinition method, List<CustomAttribute> aspects)
        {
            var processor = method.Body.GetILProcessor();

            foreach(var aspect in aspects)
            {
                var aspectInst = new VariableDefinition(String.Format("aspect{0}", aspect.AttributeType.Name), aspect.AttributeType);

                method.Body.Variables.Add(aspectInst);

                var aspectType = aspect.AttributeType.Resolve();

                var newAspect = processor.Create(OpCodes.Newobj, aspectType.Methods.First(m => m.IsConstructor));

                var assignToVar = processor.Create(OpCodes.Stloc, aspectInst.Index);
                
                var onExec = aspectType.Methods.FirstOrDefault(m => m.Name == "OnExecute");

                if (onExec != null)
                {
                    var loadAspect = processor.Create(OpCodes.Ldloc, aspectInst.Index);

                    var loadArgs = ProcessArgs(onExec, method, processor);

                    var callOnExec = processor.Create(OpCodes.Call, onExec);

                    PrependInstructions(processor, new[] {
                        newAspect,
                        assignToVar,
                        loadAspect
                    }
                    .Concat(loadArgs)
                    .Concat(new [] {
                        callOnExec
                    }));
                }
            }
        }

        private static IEnumerable<Instruction> ProcessArgs(MethodDefinition onExec, MethodDefinition target, ILProcessor processor)
        {
            //TODO: Split this matching out to parts.
            foreach (var param in onExec.Parameters)
            {
                if (param.Name.Equals("methodName", StringComparison.OrdinalIgnoreCase))
                {
                    yield return processor.Create(OpCodes.Ldstr, target.Name);
                }
                else if (param.Name.Equals("className", StringComparison.OrdinalIgnoreCase))
                {
                    yield return processor.Create(OpCodes.Ldstr, target.DeclaringType.Name);
                }
                else
                {
                    throw new Exception("IDK what to do with " + param.Name);
                }
            }
        }

        private static void PrependInstructions(ILProcessor processor, IEnumerable<Instruction> instructions)
        {
            var first = instructions.FirstOrDefault();

            if (first != null)
            {
                processor.InsertBefore(processor.Body.Instructions[0], first);
            }

            var lastAdded = first;
            foreach (var insr in instructions.Skip(1))
            {
                processor.InsertAfter(lastAdded, insr);
                lastAdded = insr;
            }
        }
    }
}
