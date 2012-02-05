using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Concurrent;

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

            var aspectInfos = new ConcurrentDictionary<string, AspectWeaver>();

            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    var aspects = new List<AspectWeaver>();

                    foreach (var attr in method.CustomAttributes)
                    {
                        var attrName = attr.AttributeType.FullName;

                        if (attrName.EndsWith("AspectAttribute"))
                        {
                            Console.WriteLine("Found Aspect {0} on {1}.{2}", attrName, type.Name, method.Name);

                            var weaver = aspectInfos.GetOrAdd(attrName, _ => new AspectWeaver(attr.AttributeType));

                            aspects.Add(weaver);
                        }
                    }

                    aspects.Reverse();

                    foreach (var weaver in aspects)
                    {
                        weaver.Weave(method);
                    }
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
    }
}
