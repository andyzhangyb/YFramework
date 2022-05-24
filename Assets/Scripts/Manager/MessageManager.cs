using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MessageManager : Singleton<MessageManager>
{
    private class MsgKeyCallback : BasePoolObject
    {
        public string MessageStr { get; set; }
        public Action MessageCallback { get; set; }

        public override void Reset()
        {
            MessageStr = string.Empty;
            MessageCallback = null;
        }
    }

    private ClassObjectPool<MsgKeyCallback> MsgCallPool = new ClassObjectPool<MsgKeyCallback>(100);

    private Dictionary<string, List<Action>> messageDictionary = new Dictionary<string, List<Action>>();
    private Dictionary<GameObject, List<MsgKeyCallback>> gameObjectToCallback = new Dictionary<GameObject, List<MsgKeyCallback>>();

    public void SendMessage(string messageStr)
    {
        if (!messageDictionary.ContainsKey(messageStr)) return;
        for (int i = 0; i < messageDictionary[messageStr].Count; i++)
        {
            messageDictionary[messageStr][i]();
        }
    }

    public void Register(GameObject gameObject, string messageStr, Action callback)
    {
        if (!messageDictionary.ContainsKey(messageStr)) messageDictionary[messageStr] = new List<Action>();
        messageDictionary[messageStr].Add(callback);
        if (!gameObjectToCallback.ContainsKey(gameObject)) gameObjectToCallback[gameObject] = new List<MsgKeyCallback>();
        MsgKeyCallback msgKeyCallback = MsgCallPool.Spawn();
        msgKeyCallback.MessageStr = messageStr;
        msgKeyCallback.MessageCallback = callback;
        gameObjectToCallback[gameObject].Add(msgKeyCallback);
    }

    public void Unregister(GameObject gameObject, string messageStr, Action callback)
    {
        if (!messageDictionary.ContainsKey(messageStr)) return;
        List<Action> callbacks = messageDictionary[messageStr];
        bool removed = false;
        for (int i = callbacks.Count - 1; i >= 0; i--)
        {
            if (callbacks[i] == callback) callbacks.RemoveAt(i);
            removed = true;
        }
        if (removed && gameObjectToCallback.ContainsKey(gameObject))
        {
            for (int i = 0; i < gameObjectToCallback[gameObject].Count; i++)
            {
                if (gameObjectToCallback[gameObject][i].MessageStr == messageStr && gameObjectToCallback[gameObject][i].MessageCallback == callback)
                {
                    MsgCallPool.Recycle(gameObjectToCallback[gameObject][i]);
                    gameObjectToCallback[gameObject].RemoveAt(i);
                    break;
                }
            }
        }
    }

    public void Unregister(GameObject gameObject, string messageStr)
    {
        if (!gameObjectToCallback.ContainsKey(gameObject)) return;
        var callbacks = gameObjectToCallback[gameObject];
        for (int i = callbacks.Count - 1; i >= 0; i--)
        {
            var callback = callbacks[i];
            if (callback.MessageStr == messageStr)
            {
                Unregister(gameObject, callback.MessageStr, callback.MessageCallback);
            }
        }
        if (callbacks.Count == 0)
        {
            gameObjectToCallback.Remove(gameObject);
        }
    }

    public void Unregister(GameObject gameObject)
    {
        if (!gameObjectToCallback.ContainsKey(gameObject)) return;
        var callbacks = gameObjectToCallback[gameObject];
        for (int i = callbacks.Count - 1; i >= 0; i--)
        {
            var callback = callbacks[i];
            Unregister(gameObject, callback.MessageStr, callback.MessageCallback);
        }
        if (callbacks.Count == 0)
        {
            gameObjectToCallback.Remove(gameObject);
        }
    }

}
