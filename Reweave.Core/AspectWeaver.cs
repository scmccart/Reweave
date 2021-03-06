﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Reweave.Core
{
    public class AspectWeaver
    {
        TypeDefinition _type;
        MethodDefinition _onExecute;
        MethodDefinition _onComplete;
        MethodDefinition _onException;
        MethodDefinition _aspectCtor;

        bool _requiresAspectInstance;
        bool _requiresCorrelationVariable;

        string _aspectInstanceVariableName;
        string _correlationVariableName;
        string _exceptionVariableName;

        TypeReference _correlationType;

        public AspectWeaver(TypeReference aspectType)
        {
            _type = aspectType.Resolve();

            _onExecute = _type.Methods.FirstOrDefault(m => m.Name == "OnExecute");
            _onComplete = _type.Methods.FirstOrDefault(m => m.Name == "OnComplete");
            _onException = _type.Methods.FirstOrDefault(m => m.Name == "OnException");

            _requiresAspectInstance = (_onExecute != null && !_onExecute.IsStatic)
                || (_onComplete != null && !_onComplete.IsStatic)
                || (_onException != null && !_onException.IsStatic);

            if (_requiresAspectInstance)
            {
                _aspectCtor = _type.Methods.FirstOrDefault(m => m.IsConstructor && !m.HasParameters);

                if (_aspectCtor == null)
                {
                    throw new Exception(String.Format("Parameterless constructor required for {0}.", _type.Name));
                }
            }

            _requiresCorrelationVariable = _onExecute != null
                && _onExecute.IsStatic
                && !_onExecute.ReturnType.TypeMatches(typeof(void));

            if (_requiresCorrelationVariable)
            {
                _correlationType = _onExecute.ReturnType;
            }

            _aspectInstanceVariableName = String.Format("aspectInstance_{0}", _type.Name);
            _correlationVariableName = String.Format("aspectCorrelation_{0}", _type.Name);
            _exceptionVariableName = String.Format("aspectException_{0}", _type.Name);
        }

        public void Weave(MethodDefinition targetMethod)
        {
            var ilp = targetMethod.Body.GetILProcessor();

            VariableDefinition aspectInstance = null;
            VariableDefinition correlation = null;

            Instruction addAfter = null;

            if (_requiresAspectInstance)
            {
                targetMethod.Body.InitLocals = true;

                aspectInstance = new VariableDefinition(_aspectInstanceVariableName, _type);
                targetMethod.Body.Variables.Add(aspectInstance);

                var createInst = ilp.Create(OpCodes.Newobj, _aspectCtor);
                var storeInst = ilp.Create(OpCodes.Stloc, aspectInstance);

                ilp.PrependInstructions(new[] { createInst, storeInst });

                addAfter = storeInst;
            }

            if (_requiresCorrelationVariable)
            {
                targetMethod.Body.InitLocals = true;

                correlation = new VariableDefinition(_correlationVariableName, _correlationType);
                targetMethod.Body.Variables.Add(correlation);
            }

            WeaveOnExecute(targetMethod, ilp, aspectInstance, correlation, addAfter);

            WeaveOnComplete(targetMethod, ilp, aspectInstance, correlation);

            WeaveOnException(targetMethod, ilp, aspectInstance, correlation, addAfter);
        }

        private void WeaveOnExecute(MethodDefinition targetMethod, ILProcessor ilp, VariableDefinition aspectInstance, VariableDefinition correlation, Instruction addAfter)
        {
            if (_onExecute != null)
            {
                var onExecInstructions = GetOnExecuteInstructions(targetMethod, ilp, aspectInstance, correlation);

                if (addAfter == null)
                {
                    ilp.PrependInstructions(onExecInstructions);
                }
                else
                {
                    ilp.InsertInstructionsAfter(addAfter, onExecInstructions);
                }
            }
        }

        private void WeaveOnComplete(MethodDefinition targetMethod, ILProcessor ilp, VariableDefinition aspectInstance, VariableDefinition correlation)
        {
            if (_onComplete != null)
            {
                var last = targetMethod.Body.Instructions.Last();

                if (last.OpCode == OpCodes.Ret)
                {
                    var insertBefore = last.Previous;

                    while (insertBefore.OpCode != OpCodes.Nop && insertBefore.OpCode != OpCodes.Br_S)
                    {
                        insertBefore = insertBefore.Previous;
                    }

                    Instruction loadReturn = null;
        
                    //if it's a br_s then we are returning a value.
                    if (insertBefore.OpCode == OpCodes.Br_S)
                    {
                        loadReturn = ilp.Copy(insertBefore.Next);
                    }

                    var onCompleteInstructions = GetOnCompleteInstructions(targetMethod, ilp, aspectInstance, correlation, loadReturn).ToArray();

                    //Place this code before the last nop or br_s so that the onException can place itself correctly.
                                                         
                    ilp.InsertInstructionsBefore(insertBefore, onCompleteInstructions);
                }
            }
        }

        private void WeaveOnException(MethodDefinition targetMethod, ILProcessor ilp, VariableDefinition aspectInstance, VariableDefinition correlation, Instruction addAfter)
        {
            if (_onException != null)
            {
                targetMethod.Body.InitLocals = true;

                var exceptionType = targetMethod.Module.Import(typeof(Exception));

                var onExceptionInstructions = GetOnExceptionInstructions(targetMethod, ilp, aspectInstance, correlation, exceptionType).ToArray();

                var tryEnd = ilp.Body.Instructions.Last();
                var returns = false;

                if (tryEnd.OpCode == OpCodes.Ret)
                {
                    returns = true;

                    var nopOrLoad = tryEnd.Previous;

                    if (nopOrLoad.OpCode == OpCodes.Nop)
                    {
                        var leaveToNop = ilp.Create(OpCodes.Leave_S, nopOrLoad);
                        ilp.InsertBefore(nopOrLoad, leaveToNop);
                        tryEnd = nopOrLoad;
                    }
                    else
                    {
                        var nopOrBreak = nopOrLoad.Previous;

                        if (nopOrBreak.OpCode == OpCodes.Br_S)
                        {
                            var nop = ilp.Create(OpCodes.Nop);
                            ilp.InsertBefore(nopOrBreak, nop);

                            ilp.Remove(nopOrBreak);

                            var leave = ilp.Create(OpCodes.Leave_S, nop);
                            ilp.InsertBefore(nop, leave);

                            tryEnd = nop;
                        }
                        else
                        {
                            tryEnd = nopOrBreak;
                        }
                    }
                }
                else
                {
                    var nop = ilp.Create(OpCodes.Nop);
                    ilp.Append(nop);

                    tryEnd = nop;
                }

                var insertPoint = tryEnd.Previous;

                ilp.InsertInstructionsAfter(insertPoint, onExceptionInstructions);

                if (!returns)
                {
                    ilp.Remove(tryEnd);
                }

                tryEnd = insertPoint.Next;

                var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Catch)
                {
                    TryStart = addAfter != null
                        ? addAfter.Next
                        : ilp.Body.Instructions.First(),
                    TryEnd = tryEnd,
                    HandlerStart = tryEnd,
                    HandlerEnd = returns
                        ? onExceptionInstructions.Last().Next
                        : null,
                    CatchType = exceptionType
                };

                targetMethod.Body.ExceptionHandlers.Add(exceptionHandler);
            }
        }

        private IEnumerable<Instruction> GetOnExecuteInstructions(MethodDefinition targetMethod, ILProcessor ilp, VariableDefinition aspectInstance, VariableDefinition correlation)
        {
            if (_requiresAspectInstance && !_onExecute.IsStatic)
            {
                yield return ilp.Create(OpCodes.Ldloc, aspectInstance);
            }

            var args = ProcessArgs(ilp, _onExecute, targetMethod);

            foreach (var arg in args)
            {
                yield return arg;
            }

            yield return ilp.Create(OpCodes.Call, _onExecute);

            if (_requiresCorrelationVariable)
            {
                yield return ilp.Create(OpCodes.Stloc, correlation);
            }
        }

        private IEnumerable<Instruction> GetOnCompleteInstructions(MethodDefinition targetMethod, ILProcessor ilp, VariableDefinition aspectInstance, VariableDefinition correlation, Instruction loadReturn)
        {
            if (_requiresAspectInstance && !_onComplete.IsStatic)
            {
                yield return ilp.Create(OpCodes.Ldloc, aspectInstance);
            }

            var dynamicArgs = new Dictionary<string, Tuple<Type, IEnumerable<Instruction>>>()
            {
                {"returnvalue", Tuple.Create(typeof(object), (loadReturn ?? ilp.Create(OpCodes.Ldnull)).AsEnumerable())}
            };

            if (_requiresCorrelationVariable)
            {
                dynamicArgs.Add("correlation", Tuple.Create(typeof(object), ilp.Create(OpCodes.Ldloc, correlation).AsEnumerable()));
            }

            var args = ProcessArgs(ilp, _onComplete, targetMethod, dynamicArgs).ToArray();

            foreach (var arg in args)
            {
                yield return arg;
            }

            yield return ilp.Create(OpCodes.Call, _onComplete);
        }

        private IEnumerable<Instruction> GetOnExceptionInstructions(MethodDefinition targetMethod, ILProcessor ilp, VariableDefinition aspectInstance, VariableDefinition correlation, TypeReference exceptionType)
        {
            var excVariable = new VariableDefinition(_exceptionVariableName, exceptionType);
            targetMethod.Body.Variables.Add(excVariable);

            yield return ilp.Create(OpCodes.Stloc, excVariable);

            if (_requiresAspectInstance && !_onException.IsStatic)
            {
                yield return ilp.Create(OpCodes.Ldloc, aspectInstance);
            }

            var dynamicArgs = new Dictionary<string, Tuple<Type, IEnumerable<Instruction>>>()
            {
                {"exception", Tuple.Create(typeof(Exception), ilp.Create(OpCodes.Ldloc, excVariable).AsEnumerable())}
            };

            if (_requiresCorrelationVariable)
            {
                dynamicArgs.Add("correlation", Tuple.Create(typeof(object), ilp.Create(OpCodes.Ldloc, correlation).AsEnumerable()));
            }

            var args = ProcessArgs(ilp, _onException, targetMethod, dynamicArgs).ToArray();

            foreach (var arg in args)
            {
                yield return arg;
            }

            yield return ilp.Create(OpCodes.Call, _onException);

            yield return ilp.Create(OpCodes.Rethrow);
        }

        private IEnumerable<Instruction> ProcessArgs(
            ILProcessor ilp, 
            MethodDefinition aspectMethod, 
            MethodDefinition targetMethod, 
            IDictionary<string, Tuple<Type, IEnumerable<Instruction>>> dynamicArgs = null)
        {
            foreach (var param in aspectMethod.Parameters)
            {
                if (param.Name.Equals("methodName", StringComparison.OrdinalIgnoreCase))
                {
                    ValidateParamType(aspectMethod, param, typeof(string));

                    yield return ilp.Create(OpCodes.Ldstr, targetMethod.Name);
                }
                else if (param.Name.Equals("className", StringComparison.OrdinalIgnoreCase))
                {
                    ValidateParamType(aspectMethod, param, typeof(string));

                    yield return ilp.Create(OpCodes.Ldstr, targetMethod.DeclaringType.Name);
                }
                else if (param.Name.Equals("arguments", StringComparison.OrdinalIgnoreCase))
                {
                    ValidateParamType(aspectMethod, param, typeof(Dictionary<string, object>));

                    var module = targetMethod.Module;

                    var dict = module.Import(typeof(Dictionary<,>)).Resolve();
                    var instDict = dict.MakeGenericInstanceType(module.TypeSystem.String, module.TypeSystem.Object)
                        .ImportInto(module);

                    var instDictResolved = instDict.Resolve();

                    var ctor = instDictResolved
                        .Methods
                        .First(m => m.IsConstructor && !m.HasParameters)
                        .Resolve()
                        .MakeHostInstanceGeneric(module.TypeSystem.String, module.TypeSystem.Object)
                        .ImportInto(module);

                    var add = instDictResolved
                        .Methods
                        .First(m => m.Name == "Add")
                        .Resolve()
                        .MakeHostInstanceGeneric(module.TypeSystem.String, module.TypeSystem.Object)
                        .ImportInto(module);
                                        
                    yield return ilp.Create(OpCodes.Newobj, ctor);

                    if (targetMethod.Parameters.Count > 0)
                    {
                        var argsVar = new VariableDefinition(instDict);
                        targetMethod.Body.Variables.Add(argsVar);
                        
                        yield return ilp.Create(OpCodes.Stloc, argsVar);

                        foreach (var targetParam in targetMethod.Parameters)
                        {
                            yield return ilp.Create(OpCodes.Ldloc, argsVar);
                            yield return ilp.Create(OpCodes.Ldstr, targetParam.Name);
                            yield return ilp.Create(OpCodes.Ldarg, targetParam);
                            yield return ilp.Create(OpCodes.Call, add);
                        }

                        yield return ilp.Create(OpCodes.Ldloc, argsVar);
                    }
                }
                else if (dynamicArgs != null && dynamicArgs.ContainsKey(param.Name.ToLower()))
                {
                    var typeAndInstrs = dynamicArgs[param.Name.ToLower()];

                    ValidateParamType(aspectMethod, param, typeAndInstrs.Item1);

                    foreach (var instr in typeAndInstrs.Item2)
                    {
                        yield return instr;
                    }
                }
                else
                {
                    throw new Exceptions.ArgumentNotRecognizedException(_type, aspectMethod, param);
                }
            }
        }

        private void ValidateParamType(MethodReference aspectMethod, ParameterReference parameter, Type expectedType)
        {
            if (!parameter.ParameterType.TypeMatches(expectedType))
            {
                throw new Exceptions.ArgumentTypeMismatchException(_type, aspectMethod, parameter, expectedType);
            }
        }
    }
}
