using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;

namespace Reweave.Core
{
    static class ILProcessorExtensions
    {
        public static void PrependInstructions(this ILProcessor processor, IEnumerable<Instruction> instructions)
        {
            var first = instructions.FirstOrDefault();

            if (first != null)
            {
                processor.InsertBefore(processor.Body.Instructions[0], first);
            }

            var lastAdded = first;
            foreach (var insr in instructions.Skip(1))
            {
                processor.InsertAfter(lastAdded, insr);
                lastAdded = insr;
            }
        }

        public static void InsertInstructionsBefore(this ILProcessor processor, Instruction before, IEnumerable<Instruction> instructions)
        {
            var lastAdded = before;

            foreach (var insr in instructions.Reverse())
            {
                processor.InsertBefore(lastAdded, insr);
                lastAdded = insr;
            }
        }

        public static void InsertInstructionsAfter(this ILProcessor processor, Instruction after, IEnumerable<Instruction> instructions)
        {
            var lastAdded = after;

            foreach (var insr in instructions)
            {
                processor.InsertAfter(lastAdded, insr);
                lastAdded = insr;
            }
        }

        public static Instruction Copy(this ILProcessor processor, Instruction toCopy)
        {
            if (toCopy.OpCode.OperandType != OperandType.InlineNone)
            {
                throw new ArgumentException("Can't this instruction", "toCopy");
            }

            return processor.Create(toCopy.OpCode);
        }
    }
}
