using UnityEditor;
using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
[System.Reflection.Obfuscation(Exclude = true)]
public class ILRuntimeCLRBinding
{
    private static string hotFixDll = "Assets/HotFixPatch/HotFixLibrary.dll.txt";
    private static string outputDir = "Assets/ResManager/ILRuntime/ILRuntimeCLRBindingAuto";

    [MenuItem("ILRuntime/根据常用类型生成CLR绑定")]
    static void GenerateCLRBindingByAnalysis()
    {
        List<Type> types = new List<Type>();
        types.Add(typeof(int));
        types.Add(typeof(float));
        types.Add(typeof(long));
        types.Add(typeof(object));
        types.Add(typeof(string));
        types.Add(typeof(Array));
        types.Add(typeof(Vector2));
        types.Add(typeof(Vector3));
        types.Add(typeof(Quaternion));
        types.Add(typeof(GameObject));
        types.Add(typeof(UnityEngine.Object));
        types.Add(typeof(Transform));
        types.Add(typeof(RectTransform));
        types.Add(typeof(Time));
        types.Add(typeof(Debug));
        //所有DLL内的类型的真实C#类型都是ILTypeInstance
        types.Add(typeof(List<ILRuntime.Runtime.Intepreter.ILTypeInstance>));

        ILRuntime.Runtime.CLRBinding.BindingCodeGenerator.GenerateBindingCode(types, outputDir);

        AssetDatabase.Refresh();
    }

    [MenuItem("ILRuntime/通过自动分析热更DLL生成CLR绑定")]
    static void AutoGenerateCLRBindingByAnalysis()
    {
        //用新的分析热更dll调用引用来生成绑定代码
        ILRuntime.Runtime.Enviorment.AppDomain domain = new ILRuntime.Runtime.Enviorment.AppDomain();
        using (System.IO.FileStream fs = new System.IO.FileStream(hotFixDll, System.IO.FileMode.Open, System.IO.FileAccess.Read))
        {
            domain.LoadAssembly(fs);

            //Crossbind Adapter is needed to generate the correct binding code
            InitILRuntime(domain);
            ILRuntime.Runtime.CLRBinding.BindingCodeGenerator.GenerateBindingCode(domain, outputDir);
        }

        AssetDatabase.Refresh();
    }

    static void InitILRuntime(ILRuntime.Runtime.Enviorment.AppDomain domain)
    {
        ////这里需要注册所有热更DLL中用到的跨域继承Adapter，否则无法正确抓取引用
        domain.RegisterCrossBindingAdaptor(new MonoBehaviourAdapter());
        domain.RegisterCrossBindingAdaptor(new CoroutineAdapter());
        domain.RegisterCrossBindingAdaptor(new UIBaseAdapter());
        //domain.RegisterCrossBindingAdaptor(new TestClassBaseAdapter());
        //domain.RegisterValueTypeBinder(typeof(Vector3), new Vector3Binder());
        //domain.RegisterValueTypeBinder(typeof(Vector2), new Vector2Binder());
        //domain.RegisterValueTypeBinder(typeof(Quaternion), new QuaternionBinder());
    }
}
