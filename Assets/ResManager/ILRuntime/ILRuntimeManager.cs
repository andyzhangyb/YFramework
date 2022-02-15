using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TestClassForCoroutine
{
}

public delegate string TestDelegateFunction(int a);

public class ILRuntimeManager : Singleton<ILRuntimeManager>
{
    private const string dllPath = "Assets/HotFixPatch/HotFixLibrary.dll.txt";
    private const string pdbPath = "Assets/HotFixPatch/HotFixLibrary.pdb.txt";

    private ILRuntime.Runtime.Enviorment.AppDomain appDomain = null;
    public ILRuntime.Runtime.Enviorment.AppDomain AppDomain { get { return appDomain; } }
    MemoryStream msAppDomain = null;
    public void Init()
    {
        LoadHotFixAssembly();
    }

    public void LoadHotFixAssembly()
    {
        if (msAppDomain != null)
        {
            msAppDomain.Close();
            msAppDomain.Dispose();
            appDomain = null;
        }
        appDomain = new ILRuntime.Runtime.Enviorment.AppDomain();
        TextAsset dllText = ResourceManager.Instance.LoadResource<TextAsset>(dllPath);
        msAppDomain = new MemoryStream(dllText.bytes);
        appDomain.LoadAssembly(msAppDomain, null, new ILRuntime.Mono.Cecil.Pdb.PdbReaderProvider());

        appDomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction>((act) =>
        {
            return new UnityEngine.Events.UnityAction(() =>
            {
                ((Action)act)();
            });
        });

        appDomain.RegisterCrossBindingAdaptor(new MonoBehaviourAdapter());
        appDomain.RegisterCrossBindingAdaptor(new CoroutineAdapter());
        appDomain.RegisterCrossBindingAdaptor(new UIBaseAdapter());

        new AddComponentRedirection().StartRedirection(appDomain);

        ILRuntime.Runtime.Generated.CLRBindings.Initialize(appDomain);
    }



}
