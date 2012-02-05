using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace TestTargetApp
{
    class Program
    {
        static void Main(string[] args)
        {
            TestPrint();
            TestPrintStatic();

            TestPrint2();

            TestPrintParam("boofar", "foobar");

            TestDict();

            try
            {
                TestThrow();

                TestCatch();
            }
            catch (Exception)
            {

            }
        }

        private static void TestDict()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();

            dict.Add("foo", 4);
        }

        [LoggingAspect]
        static string TestPrint()
        {
            Console.WriteLine("foobar");
            return "foo";
        }

        [StaticLoggingAspect]
        static string TestPrintStatic()
        {
            Console.WriteLine("foobar2");
            return "foo2";
        }

        [LoggingAspect]
        static void TestPrint2()
        {
            Console.WriteLine("foobar");
        }

        [LoggingAspect]
        static void TestPrintParam(string param, string param2)
        {
            Console.WriteLine(param, param2);
        }

        [LoggingAspect]
        static void TestThrow()
        {
            throw new Exception();
        }

        static void TestThrow2()
        {
            var handler = new TinyHandler();

            try
            {
                throw new Exception();
            }
            catch (Exception exc)
            {
                handler.HandleExc("TestThrow2", "Program", exc);

                throw;
            }
        }

        static string TestCatch()
        {
            try
            {
                TestThrow();

                return "bar";
            }
            catch (Exception exc)
            {
                HandleIt(exc);
                throw;
            }
        }

        static bool HandleIt(Exception foo)
        {
            return true;
        }

        class TinyHandler
        {
            public void HandleExc(string name, string className, Exception foo)
            {
                Console.WriteLine(foo.Message);
            }
        }
    }
}
