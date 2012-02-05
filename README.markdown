__Reweave__  
A Mono.Cecil based assembly re-writer focused on enabling aspect oriented programming for .Net. This is an extremely early version - pre v0.1 to say the least.
  
With Reweave aspects are convention based, any Attribute with a name ending in "AspectAttribute" is considered an aspect - no run time library required. Likewise hooking into the method's execution is based on convention:

  - OnExecute - is called before the body of the method
    - methodName : string - the name of the method being invoked
    - className : string - the name of the class containing the method
    - return value : object - can optionally return a correlation value to be passed to the other methods of the aspect
  - OnComplete - is called before the method returns but after the body
    - methodName : string - the name of the method being invoked
    - className : string - the name of the class containing the method
    - correlation : object - the value optionally returned from OnExecute
  - OnException - is called whenever an exception would bubble up from the 
    - methodName : string - the name of the method being invoked
    - className : string - the name of the class containing the methodmethod
    - correlation : object - the value optionally returned from OnExecute
    - exception : Exception - the exception that is bubbling up from the method

In addition aspects can either be static or instance by nature - if any of the above methods are defined as instance methods a new instance of the aspect will be created each time a method marked with it is invoked. Otherwise, if they are all static no instance will be created.

Here's the canonical logging example implemented for Reweave,
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
class LoggingAspectAttribute : Attribute
{
    public void OnExecute(string methodName, string className)
    {
        Console.WriteLine("Executing {0}.{1}", className, methodName);
    }

    public void OnComplete(string methodName, string className)
    {
        Console.WriteLine("Completed {0}.{1}", className, methodName);
    }

    public void OnException(string methodName, string className, Exception exception)
    {
        Console.WriteLine("Exception {0}.{1}: {2}", className, methodName, exception.Message);
    }
}
```

Note that nothing other than the System namespace is required here.

So, if you are brave give it a go and let me know if it screws up on you.