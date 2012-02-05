using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestTargetApp
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    class LoggingAspectAttribute : Attribute
    {
        public void OnExecute(string methodName, string className, Dictionary<string, object> arguments)
        {
            Console.WriteLine("Executing {0}.{1}({2})", className, methodName, string.Join(", ", arguments.Select(kvp => String.Format("{0}: {1}", kvp.Key, kvp.Value))));
        }

        public void OnComplete(string methodName, string className, object returnValue)
        {
            Console.WriteLine("Completed {0}.{1} with {2}", className, methodName, returnValue ?? "null");
        }

        public void OnException(string methodName, string className, Exception exception)
        {
            Console.WriteLine("Exception {0}.{1}: {2}", className, methodName, exception.Message);
        }
    }
}
