using System;
using System.IO;
using Reweave.Core;

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

            var weaver = new AssemblyWeaver();
            weaver.Load(targetAsm);
            weaver.Weave();
            weaver.Write(targetAsm);

            Console.WriteLine("Executing...");

            AppDomain.CurrentDomain.ExecuteAssembly(targetAsm);

            Console.Write("Done");
            Console.ReadKey();
       }
    }
}
