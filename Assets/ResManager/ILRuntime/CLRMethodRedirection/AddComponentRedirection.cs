using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class AddComponentRedirection
{
    public unsafe void StartRedirection(ILRuntime.Runtime.Enviorment.AppDomain appDomain)
    {
        var array = typeof(UnityEngine.GameObject).GetMethods();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].Name == "AddComponent" && array[i].GetGenericArguments().Length == 1)
            {
                appDomain.RegisterCLRMethodRedirection(array[i], AddComponent);
            }
        }
    }

    private unsafe StackObject* AddComponent(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
    {
        ILRuntime.Runtime.Enviorment.AppDomain appDomain = __intp.AppDomain;
        var ptr = __esp - 1;
        UnityEngine.GameObject instance = StackObject.ToObject(ptr, appDomain, __mStack) as UnityEngine.GameObject;

        if (instance == null)
        {
            throw new System.ArgumentNullException();
        }
        var genericArgument = __method.GenericArguments;
        if (genericArgument != null && genericArgument.Length == 1)
        {
            var type = genericArgument[0];
            object res;
            if (type is ILRuntime.CLR.TypeSystem.CLRType)
            {
                res = instance.AddComponent(type.TypeForCLR);
            }
            else
            {
                var iLInstance = new ILTypeInstance(type as ILRuntime.CLR.TypeSystem.ILType, false);
                var cLRInstance = instance.AddComponent<MonoBehaviourAdapter.Adaptor>();
                cLRInstance.ILInstance = iLInstance;
                cLRInstance.AppDomain = appDomain;
                iLInstance.CLRInstance = cLRInstance;
                cLRInstance.Awake();

                res = cLRInstance.ILInstance;
            }
            return ILIntepreter.PushObject(ptr, __mStack, res);
        }

        return __esp;
    }
}
