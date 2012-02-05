using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.Collections.Concurrent;
using System.Diagnostics;
using Reweave.Core.Properties;

namespace Reweave.Core
{
    public class AssemblyWeaver
    {
        ModuleDefinition _module;

        public void Load(string filename)
        {
            _module = ModuleDefinition.ReadModule(filename, new ReaderParameters()
            {
                ReadSymbols = true
            });
        }

        public void Weave()
        {
            var weaverCache = new ConcurrentDictionary<string, AspectWeaver>();

            foreach (var type in _module.Types)
            {
                foreach (var method in type.Methods)
                {
                    var aspects = new List<AspectWeaver>();

                    foreach (var attr in method.CustomAttributes)
                    {
                        var attrName = attr.AttributeType.FullName;

                        if (attrName.EndsWith(Resources.AspectAttribute))
                        {
                            Debug.WriteLine("Found Aspect {0} on {1}.{2}", attrName, type.Name, method.Name);

                            var weaver = weaverCache.GetOrAdd(attrName, _ => new AspectWeaver(attr.AttributeType));

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
        }

        public void Write(string filename)
        {
            _module.Write(filename, new WriterParameters()
            {
                WriteSymbols = true
            });
        }
    }
}
