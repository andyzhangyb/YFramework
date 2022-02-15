using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineAdapter : CrossBindingAdaptor
{
    public override Type BaseCLRType => null;

    public override Type AdaptorType => typeof(Adaptor);

    public override Type[] BaseCLRTypes
    {
        get
        {
            return new Type[] { typeof(IEnumerator<System.Object>), typeof(IEnumerator), typeof(IDisposable) };
        }
    }

    public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
    {
        return new Adaptor(appdomain, instance);
    }

    public class Adaptor : IEnumerator<System.Object>, IEnumerator, IDisposable, CrossBindingAdaptorType
    {
        private ILRuntime.Runtime.Enviorment.AppDomain appDomain = null;
        private ILTypeInstance iLTypeInstance = null;

        public Adaptor() { }
        public Adaptor(ILRuntime.Runtime.Enviorment.AppDomain appDomain, ILTypeInstance iLTypeInstance)
        {
            this.appDomain = appDomain;
            this.iLTypeInstance = iLTypeInstance;
        }

        public ILTypeInstance ILInstance
        {
            get { return iLTypeInstance; }
        }

        private ILRuntime.CLR.Method.IMethod currentMethod = null;
        private bool haveGotCurrentMethod = false;
        public object Current
        {
            get
            {
                if (!haveGotCurrentMethod)
                {
                    currentMethod = iLTypeInstance.Type.GetMethod("get_Current", 0);
                    if (currentMethod == null)
                    {
                        currentMethod = iLTypeInstance.Type.GetMethod("System.Collections.IEnumerator.get_Current", 0);
                    }
                    haveGotCurrentMethod = true;
                }
                if (currentMethod != null)
                {
                    return appDomain.Invoke(currentMethod, iLTypeInstance, null);
                }
                return null;
            }
        }

        private ILRuntime.CLR.Method.IMethod disposeMethod = null;
        private bool haveGotDisposeMethod = false;
        public void Dispose()
        {
            if (!haveGotDisposeMethod)
            {
                disposeMethod = iLTypeInstance.Type.GetMethod("Dispose", 0);
                if (disposeMethod == null)
                {
                    disposeMethod = iLTypeInstance.Type.GetMethod("System.IDisposable.Dispose", 0);
                }
                haveGotDisposeMethod = true;
            }
            if (disposeMethod != null)
            {
                appDomain.Invoke(disposeMethod, iLTypeInstance, null);
            }
        }

        private ILRuntime.CLR.Method.IMethod moveNextMethod = null;
        private bool haveGotNextMethod = false;
        public bool MoveNext()
        {
            if (!haveGotNextMethod)
            {
                moveNextMethod = iLTypeInstance.Type.GetMethod("MoveNext", 0);
                haveGotNextMethod = true;
            }
            if (moveNextMethod != null)
            {
                return (bool)appDomain.Invoke(moveNextMethod, iLTypeInstance, null);
            }
            else
            {
                return false;
            }
        }

        private ILRuntime.CLR.Method.IMethod resetMethod = null;
        private bool haveGotResetMethod = false;
        public void Reset()
        {
            if (!haveGotResetMethod)
            {
                resetMethod = iLTypeInstance.Type.GetMethod("Reset", 0);
                haveGotResetMethod = true;
            }
            if (resetMethod != null)
            {
                appDomain.Invoke(resetMethod, iLTypeInstance, null);
            }
        }

        public override string ToString()
        {
            IMethod m = appDomain.ObjectType.GetMethod("ToString", 0);
            m = iLTypeInstance.Type.GetVirtualMethod(m);
            if (m == null || m is ILMethod)
            {
                return iLTypeInstance.ToString();
            }
            else
                return iLTypeInstance.Type.FullName;
        }
    }
}