using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Reweave.Core.Properties;

namespace Reweave.Core.Exceptions
{
    [Serializable]
    public class ArgumentNotRecognizedException : Exception
    {
        public ArgumentNotRecognizedException() { }
        public ArgumentNotRecognizedException(string message) : base(message) { }
        public ArgumentNotRecognizedException(string message, Exception inner) : base(message, inner) { }
        protected ArgumentNotRecognizedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public ArgumentNotRecognizedException(
            TypeReference aspectType, 
            MethodReference aspectMethod, 
            ParameterReference param)
            :base(String.Format(Resources.ArgumentNotRecognizedMessageFormat, aspectType.Name, aspectMethod.Name, param.Name))
        {

        }
    }
}
