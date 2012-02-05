using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestTargetApp
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    class StaticLoggingAspectAttribute : Attribute
    {
        public static void OnExecute(string methodName, string className)
        {
            Console.WriteLine("Executing {0}.{1}", className, methodName);
        }

        public static void OnComplete(string methodName, string className)
        {
            Console.WriteLine("Completed {0}.{1}", className, methodName);
        }

        public static void OnException(string methodName, string className, Exception exception)
        {
            Console.WriteLine("Exception {0}.{1}: {2}", className, methodName, exception.Message);
        }
    }
}
