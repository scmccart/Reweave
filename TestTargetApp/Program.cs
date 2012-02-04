using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace TestTargetApp
{
    class Program
    {
        public string SomeProperty { get; set; }

        [LoggingAspect]
        static void Main(string[] args)
        {
            var foo = "bar";
            Console.WriteLine(foo);
        }
    }
}
