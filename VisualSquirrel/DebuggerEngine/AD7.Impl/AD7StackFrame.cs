/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;

namespace VisualSquirrel.Debugger.Engine
{
    // Represents a logical stack frame on the thread stack. 
    // Also implements the IDebugExpressionContext interface, which allows expression evaluation and watch windows.
    class AD7StackFrame : IDebugStackFrame2, IDebugExpressionContext2
    {
        readonly AD7Engine m_engine;
        readonly DebuggedThread m_thread;
        //readonly X86ThreadContext m_threadContext;

     /*   private string m_documentName;       
        private string m_functionName;
        private uint m_lineNum;
        private bool m_hasSource;
        private uint m_numParameters;
        private uint m_numLocals;
        private uint _fakeAddress; //alberto*/

        SquirrelStackFrame sqframe;
        SquirrelDebugContext ctx;
        // An array of this frame's parameters
      //  private VariableInformation[] m_parameters;

        // An array of this frame's locals
       // private VariableInformation[] m_locals;


        public AD7StackFrame(AD7Engine engine, DebuggedThread thread, SquirrelDebugContext ctx, SquirrelStackFrame squirrelsf)
        {
            m_engine = engine;
            m_thread = thread;
            sqframe = squirrelsf;
            this.ctx = ctx;
            //m_threadContext = threadContext;

            // Try to get source information for this location. If symbols for this file have not been found, this will fail.
            /*m_hasSource = m_engine.DebuggedProcess.GetSourceInformation(
                                                            m_threadContext.eip, 
                                                            ref m_documentName, 
                                                            ref m_functionName, 
                                                            ref m_lineNum,
                                                            ref m_numParameters,
                                                            ref m_numLocals);

            // If source information is available, create the collections of locals and parameters and populate them with
            // values from the debuggee.
            if (m_hasSource)
            {
                if (m_numParameters > 0)
                {
                    m_parameters = new VariableInformation[m_numParameters];
                    m_engine.DebuggedProcess.GetFunctionArgumentsByIP(m_threadContext.eip, m_threadContext.ebp, m_parameters);
                }

                if (m_numLocals > 0)
                {
                    m_locals = new VariableInformation[m_numLocals];
                    m_engine.DebuggedProcess.GetFunctionLocalsByIP(m_threadContext.eip, m_threadContext.ebp, m_locals);
                }
            }*/
        }

        #region Non-interface methods

        // Construct a FRAMEINFO for this stack frame with the requested information.
        public void SetFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, out FRAMEINFO frameInfo)
        {
            frameInfo = new FRAMEINFO();
            
            //uint ip = m_threadContext.eip;
            DebuggedModule module = m_engine.DebuggedProcess.Module;

            // The debugger is asking for the formatted name of the function which is displayed in the callstack window.
            // There are several optional parts to this name including the module, argument types and values, and line numbers.
            // The optional information is requested by setting flags in the dwFieldSpec parameter.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME) != 0)
            {
                // If there is source information, construct a string that contains the module name, function name, and optionally argument names and values.
                if (sqframe.Source != "NATIVE")
                {
                    frameInfo.m_bstrFuncName = "";

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE) != 0)
                    {
                        frameInfo.m_bstrFuncName = sqframe.Source + " ! ";
                    }

                    frameInfo.m_bstrFuncName += sqframe.Function;

                    /*if ((dwFieldSpec & (uint)enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS) != 0 && m_numParameters > 0)
                    {
                        frameInfo.m_bstrFuncName += "(";
                        for (int i = 0; i < m_parameters.Length; i++)
                        {
                            if ((dwFieldSpec & (uint)enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_TYPES) != 0)
                            {
                                frameInfo.m_bstrFuncName += m_parameters[i].TypeName + " ";
                            }

                            if ((dwFieldSpec & (uint)enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_NAMES) != 0)
                            {
                                frameInfo.m_bstrFuncName += m_parameters[i].Name;
                            }

                            if ((dwFieldSpec & (uint)enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_VALUES) != 0)
                            {
                                frameInfo.m_bstrFuncName += "=" + m_parameters[i].Value;
                            }

                            if (i < m_parameters.Length - 1)
                            {
                                frameInfo.m_bstrFuncName += ", ";
                            }
                        }
                        frameInfo.m_bstrFuncName += ")";
                    }*/

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LINES) != 0)
                    {
                        frameInfo.m_bstrFuncName += " Line:" + sqframe.Line.ToString();
                    }
                }
                else
                {                   
                    // No source information, so only return the module name and the instruction pointer.
                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE) != 0)
                    {
                        frameInfo.m_bstrFuncName = sqframe.Function;
                    }
                    else
                    {
                        frameInfo.m_bstrFuncName = sqframe.Function;//EngineUtils.GetAddressDescription(null, ip);
                    }
                }
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME;
            }

            // The debugger is requesting the name of the module for this stack frame.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_MODULE) != 0)
            {
                frameInfo.m_bstrModule = module.Name;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_MODULE;
            }

            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_LANGUAGE) != 0)
            {
                frameInfo.m_bstrLanguage = sqframe.Source != "NATIVE" ? "Squirrel" : "Native";
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_LANGUAGE;
            }

            // The debugger is requesting the range of memory addresses for this frame.
            // For the sample engine, this is the contents of the frame pointer.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_STACKRANGE) != 0)
            {
                frameInfo.m_addrMin = sqframe.Address;
                frameInfo.m_addrMax = sqframe.Address;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STACKRANGE;
            }

            // The debugger is requesting the IDebugStackFrame2 value for this frame info.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FRAME) != 0)
            {
                frameInfo.m_pFrame = this;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FRAME;
            }
            
            // Does this stack frame of symbols loaded?
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO) != 0)
            {
                frameInfo.m_fHasDebugInfo = sqframe.Source != "NATIVE" ? 1 : 0;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO;
            }

            // Is this frame stale?
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_STALECODE) != 0)
            {
                frameInfo.m_fStaleCode = 0;
                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STALECODE;
            }

            // The debugger would like a pointer to the IDebugModule2 that contains this stack frame.
            if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP) != 0)
            {
                if (module != null)
                {
                    AD7Module ad7Module = (AD7Module)module.Client;
                    Debug.Assert(ad7Module != null);
                    frameInfo.m_pModule = ad7Module;
                    frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP;
                }
            }
        }

        // Construct an instance of IEnumDebugPropertyInfo2 for the combined locals and parameters.
        private void CreateLocalsPlusArgsProperties(out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject, bool hex)
        {
           // bool hex = SquirrelDebuggerEngine.SquirrelDebuggerEnginePackage.HexDisplayMode;
            
            elementsReturned = 0;

            int localsLength = 0;
            List<SquirrelDebugObject> locals = sqframe.Locals;
            if (locals != null)
            {
                localsLength = locals.Count;
                elementsReturned += (uint)localsLength;
            }

            /*if (m_parameters != null)
            {
                elementsReturned += (uint)m_parameters.Length;
            }*/
            DEBUG_PROPERTY_INFO[] propInfo = new DEBUG_PROPERTY_INFO[elementsReturned];

            if (sqframe.Locals != null)
            {
                for (int i = 0; i < sqframe.Locals.Count; i++)
                {
                    AD7Property property = new AD7Property(locals[i]);
                    propInfo[i] = property.ConstructDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_STANDARD, hex);
                }
            }

            /*if (m_parameters != null)
            {
                for (int i = 0; i < m_parameters.Length; i++)
                {                   
                    AD7Property property = new AD7Property(m_parameters[i]);
                    propInfo[localsLength + i] = property.ConstructDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_STANDARD); 
                }
            }*/

            enumObject = new AD7PropertyInfoEnum(propInfo);
        }

        // Construct an instance of IEnumDebugPropertyInfo2 for the locals collection only.
        private void CreateLocalProperties(out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject,bool hex)
        {

           // bool hex = SquirrelDebuggerEngine.SquirrelDebuggerEnginePackage.HexDisplayMode;
            List<SquirrelDebugObject> locals = sqframe.Locals;
            elementsReturned = (uint)locals.Count;
            DEBUG_PROPERTY_INFO[] propInfo = new DEBUG_PROPERTY_INFO[locals.Count];

            for (int i = 0; i < propInfo.Length; i++)
            {
                AD7Property property = new AD7Property(locals[i]);
                propInfo[i] = property.ConstructDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_STANDARD, hex);
            }

            enumObject = new AD7PropertyInfoEnum(propInfo);
        }

        // Construct an instance of IEnumDebugPropertyInfo2 for the parameters collection only.
        private void CreateParameterProperties(out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            /*elementsReturned = (uint)m_parameters.Length;
            DEBUG_PROPERTY_INFO[] propInfo = new DEBUG_PROPERTY_INFO[m_parameters.Length];

            for (int i = 0; i < propInfo.Length; i++)
            {
                AD7Property property = new AD7Property(m_parameters[i]);
                propInfo[i] = property.ConstructDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_STANDARD);
            }*/
            elementsReturned = 0;
            enumObject = new AD7PropertyInfoEnum(new DEBUG_PROPERTY_INFO[0]);
        }

        #endregion

        #region IDebugStackFrame2 Members

        // Creates an enumerator for properties associated with the stack frame, such as local variables.
        // The sample engine only supports returning locals and parameters. Other possible values include
        // class fields (this pointer), registers, exceptions...
        int IDebugStackFrame2.EnumProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, uint nRadix, ref Guid guidFilter, uint dwTimeout, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            int hr;

            elementsReturned = 0;
            enumObject = null;
            bool hex = nRadix == 16;
            try
            {
                if (guidFilter == AD7Guids.guidFilterLocalsPlusArgs ||
                        guidFilter == AD7Guids.guidFilterAllLocalsPlusArgs ||
                        guidFilter == AD7Guids.guidFilterAllLocals)        
                {
                    CreateLocalsPlusArgsProperties(out elementsReturned, out enumObject,hex);
                    hr = EngineConstants.S_OK;
                }
                else if (guidFilter == AD7Guids.guidFilterLocals)
                {
                    CreateLocalProperties(out elementsReturned, out enumObject, hex);
                    hr = EngineConstants.S_OK;
                }
                else if (guidFilter == AD7Guids.guidFilterArgs)
                {
                    CreateParameterProperties(out elementsReturned, out enumObject);
                    hr = EngineConstants.S_OK;
                }
                else
                {
                    hr = EngineConstants.E_NOTIMPL;
                }
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
            
            return hr;
        }

        // Gets the code context for this stack frame. The code context represents the current instruction pointer in this stack frame.
        int IDebugStackFrame2.GetCodeContext(out IDebugCodeContext2 memoryAddress)
        {
            memoryAddress = null;

            try
            {
                memoryAddress = new AD7MemoryAddress(m_engine, (uint)sqframe.Address);
                return EngineConstants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Gets a description of the properties of a stack frame.
        // Calling the IDebugProperty2::EnumChildren method with appropriate filters can retrieve the local variables, method parameters, registers, and "this" 
        // pointer associated with the stack frame. The debugger calls EnumProperties to obtain these values in the sample.
        int IDebugStackFrame2.GetDebugProperty(out IDebugProperty2 property)
        {
            //throw new NotImplementedException();
            property = null;
            return EngineConstants.E_NOTIMPL;
        }

        // Gets the document context for this stack frame. The debugger will call this when the current stack frame is changed
        // and will use it to open the correct source document for this stack frame.
        int IDebugStackFrame2.GetDocumentContext(out IDebugDocumentContext2 docContext)
        {
            docContext = null;
            try
            {
                if (sqframe.Source != "NATIVE")
                {
                    // Assume all lines begin and end at the beginning of the line.
                    TEXT_POSITION begTp = new TEXT_POSITION();
                    begTp.dwColumn = 0;
                    begTp.dwLine = (uint)sqframe.Line - 1;
                    TEXT_POSITION endTp = new TEXT_POSITION();
                    endTp.dwColumn = 0;
                    endTp.dwLine = (uint)sqframe.Line - 1;

                    docContext = new AD7DocumentContext(sqframe.Source, begTp, endTp, null);
                    return EngineConstants.S_OK;
                }
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return EngineConstants.S_FALSE;
        }

        // Gets an evaluation context for expression evaluation within the current context of a stack frame and thread.
        // Generally, an expression evaluation context can be thought of as a scope for performing expression evaluation. 
        // Call the IDebugExpressionContext2::ParseText method to parse an expression and then call the resulting IDebugExpression2::EvaluateSync 
        // or IDebugExpression2::EvaluateAsync methods to evaluate the parsed expression.
        int IDebugStackFrame2.GetExpressionContext(out IDebugExpressionContext2 ppExprCxt)
        {
            ppExprCxt = (IDebugExpressionContext2)this;
            return EngineConstants.S_OK;
        }

        // Gets a description of the stack frame.
        int IDebugStackFrame2.GetInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, FRAMEINFO[] pFrameInfo)
        {
            try
            {
                SetFrameInfo(dwFieldSpec, out pFrameInfo[0]);

                return EngineConstants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Gets the language associated with this stack frame. 
        // In this sample, all the supported stack frames are C++
        int IDebugStackFrame2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            pbstrLanguage = "Squirrel";
            pguidLanguage = AD7Guids.guidLanguageSquirrel;
            return EngineConstants.S_OK;
        }

        // Gets the name of the stack frame.
        // The name of a stack frame is typically the name of the method being executed.
        int IDebugStackFrame2.GetName(out string name)
        {
            name = null;

            try
            {
                name = sqframe.Function;// EngineUtils.GetAddressDescription(null, sqframe.Address);

                return EngineConstants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Gets a machine-dependent representation of the range of physical addresses associated with a stack frame.
        int IDebugStackFrame2.GetPhysicalStackRange(out ulong addrMin, out ulong addrMax)
        {
            addrMin = sqframe.Address;
            addrMax = sqframe.Address;

            return EngineConstants.S_OK;
        }

        // Gets the thread associated with a stack frame.
        int IDebugStackFrame2.GetThread(out IDebugThread2 thread)
        {
            thread = m_thread;
            return EngineConstants.S_OK;
        }

        #endregion

        #region IDebugExpressionContext2 Members

        // Retrieves the name of the evaluation context. 
        // The name is the description of this evaluation context. It is typically something that can be parsed by an expression evaluator 
        // that refers to this exact evaluation context. For example, in C++ the name is as follows: 
        // "{ function-name, source-file-name, module-file-name }"
        int IDebugExpressionContext2.GetName(out string pbstrName)
        {
            //throw new NotImplementedException();

            pbstrName = string.Format("{ {0}, {1} }", sqframe.Source, sqframe.Function);
            return EngineConstants.S_OK;
        }

        bool EvaluateSquirrelObject(List<SquirrelDebugObject> children, string[] names, int depth, out SquirrelDebugObject val)
        {

            foreach (SquirrelDebugObject currVariable in children)
            {
                if (String.CompareOrdinal(currVariable.Name, names[depth]) == 0)
                {
                    if (names.Length - 1 > depth)
                    {
                        if (currVariable.Value.Children != null)
                        {
                            return EvaluateSquirrelObject(currVariable.Value.Children, names, depth + 1, out val);
                        }
                        else
                        {
                            val = null;
                            return false;
                        }
                    }
                    else
                    {
                        val = currVariable;
                        return true;
                    }
                }
            }
            val = null;
            return false;            
        }
        // Parses a text-based expression for evaluation.
        // The engine sample only supports locals and parameters so the only task here is to check the names in those collections.
        int IDebugExpressionContext2.ParseText(string pszCode,
                                                enum_PARSEFLAGS dwFlags, 
                                                uint nRadix, 
                                                out IDebugExpression2 ppExpr, 
                                                out string pbstrError, 
                                                out uint pichError)
        {
            pbstrError = "";
            pichError = 0;
            ppExpr = null;
            if (pszCode.Length == 0)
            {
                pbstrError = "Invalid Expression";
                pichError = (uint)pbstrError.Length;
                return EngineConstants.S_FALSE;
            }

            try
            {
                bool iswatch = true;
                //HACK WARNING
                //I put a $ in front of the expression so I know that is not a watch in 
                //is done by SquirrelAuthoringScope::GetDataTipText (alberto)
                if (pszCode[0] == '$')
                {
                    Debug.WriteLine("!Evaluating " + pszCode);
                    iswatch = false;
                    pszCode = pszCode.Substring(1);
                    if (pszCode.Length == 0)
                    {
                        pbstrError = "Invalid Expression";
                        pichError = (uint)pbstrError.Length;
                        return EngineConstants.S_FALSE;
                    }
                }
                if (sqframe.Locals != null)
                {
                    Debug.WriteLine("Evaluating " + pszCode);
                    string[] targets = pszCode.Split(new char[] { '.' });
                    SquirrelDebugObject val;
                    if(EvaluateSquirrelObject(sqframe.Locals,targets,0,out val))
                    {

                        ppExpr = new AD7Expression(new SquirrelDebugObject(pszCode,val.Value));
                        
                        return EngineConstants.S_OK;
                    }

                    if (targets[0] != "this")
                    {
                        List<string> trgswiththis = new List<string>();
                        trgswiththis.Add("this");
                        trgswiththis.AddRange(targets);
                        if (EvaluateSquirrelObject(sqframe.Locals, trgswiththis.ToArray(), 0, out val))
                        {

                            ppExpr = new AD7Expression(new SquirrelDebugObject(pszCode, val.Value));
                            Debug.WriteLine("Succeeded with 'this'" + pszCode);
                            return EngineConstants.S_OK;
                        }
                    }
                }
                
                //if(sqframe.Watches.ContainsKey(pszCode))
                if (iswatch)
                {
                    ctx.AddWatch(pszCode);
                    SquirrelDebugObject val;
                    if (sqframe.Watches.TryGetValue(pszCode, out val))
                    {
                        ppExpr = new AD7Expression(val);
                        return EngineConstants.S_OK;
                    }
                }
               

                pbstrError = "Invalid Expression";
                pichError = (uint)pbstrError.Length;
                return EngineConstants.S_FALSE;
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        #endregion
    }
}

