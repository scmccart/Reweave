﻿using System;
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

            TestPrint2();

            try
            {
                TestThrow();

                TestCatch();
            }
            catch (Exception)
            {

            }
        }

        [LoggingAspect]
        static string TestPrint()
        {
            Console.WriteLine("foobar");
            return "foo";
        }

        [LoggingAspect]
        static void TestPrint2()
        {
            Console.WriteLine("foobar");
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

        static void HandleIt(Exception foo)
        {

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
