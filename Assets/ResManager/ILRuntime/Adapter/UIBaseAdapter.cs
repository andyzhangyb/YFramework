using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UIBaseAdapter : CrossBindingAdaptor
{
    public override Type BaseCLRType => typeof(UIBase);

    public override Type AdaptorType => typeof(Adaptor);

    public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
    {
        return new Adaptor(appdomain, instance);
    }
    public class Adaptor : UIBase, CrossBindingAdaptorType
    {
        private ILRuntime.Runtime.Enviorment.AppDomain appDomain = null;
        private ILTypeInstance iLTypeInstance = null;
        private bool isCallBaseMethod = false;

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

        IMethod mAwakeMethod;
        bool mAwakeMethodGot;
        public override void Awake()
        {
            if (!mAwakeMethodGot)
            {
                mAwakeMethod = iLTypeInstance.Type.GetMethod("Awake", 0);
                mAwakeMethodGot = true;
            }

            if (mAwakeMethod != null)
            {
                appDomain.Invoke(mAwakeMethod, iLTypeInstance, null);
            }
        }

        IMethod mStartMethod;
        bool mStartMethodGot;
        public override void Start()
        {
            if (!mStartMethodGot)
            {
                mStartMethod = iLTypeInstance.Type.GetMethod("Start", 0);
                mStartMethodGot = true;
            }

            if (mStartMethod != null)
            {
                appDomain.Invoke(mStartMethod, iLTypeInstance, null);
            }
        }

        IMethod mUpdateMethod;
        bool mUpdateMethodGot;
        public override void Update()
        {
            if (!mUpdateMethodGot)
            {
                mUpdateMethod = iLTypeInstance.Type.GetMethod("Update", 0);
                mUpdateMethodGot = true;
            }

            if (mUpdateMethod != null)
            {
                appDomain.Invoke(mUpdateMethod, iLTypeInstance, null);
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

