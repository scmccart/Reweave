using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestTargetApp
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    class LoggingAspectAttribute : Attribute
    {
        public void OnExecute(string methodName, string className)
        {
            Console.WriteLine("Executing {0}.{1}", className, methodName);
        }
    }
}
