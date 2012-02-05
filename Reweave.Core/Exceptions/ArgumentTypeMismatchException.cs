using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Reweave.Core.Properties;

namespace Reweave.Core.Exceptions
{
    [Serializable]
    public class ArgumentTypeMismatchException : Exception
    {
        public ArgumentTypeMismatchException() { }
        public ArgumentTypeMismatchException(string message) : base(message) { }
        public ArgumentTypeMismatchException(string message, Exception inner) : base(message, inner) { }
        protected ArgumentTypeMismatchException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public ArgumentTypeMismatchException(
            TypeReference aspectType, 
            MethodReference aspectMethod, 
            ParameterReference param,
            TypeReference expectedType)
            : base(String.Format(Resources.ArgumentTypeMismatchMessageFormat, aspectType.Name, aspectMethod.Name, param.Name, expectedType.Name))
        {

        }

        public ArgumentTypeMismatchException(
            TypeReference aspectType,
            MethodReference aspectMethod,
            ParameterReference param,
            Type expectedType)
            : base(String.Format(Resources.ArgumentTypeMismatchMessageFormat, aspectType.Name, aspectMethod.Name, param.Name, expectedType.Name))
        {

        }
    }
}
