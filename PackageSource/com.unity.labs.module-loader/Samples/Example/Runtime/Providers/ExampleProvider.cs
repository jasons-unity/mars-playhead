using System;
using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    // ReSharper disable once UnusedMember.Global
    class ExampleProvider : MonoBehaviour, IProviderExample, IProviderExample2<int>
    {
        public event Action ExampleAction;
        public event Action<int> ExampleIntAction;
        public event Func<bool> ExampleFunc;
        public event Func<int, bool> ExampleIntFunc;

        public int ExampleProperty { get; set; }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var subscriberExample = obj as ISubscriberExample;
            if (subscriberExample != null)
                subscriberExample.provider = this;

            var subscriberExample2 = obj as ISubscriberExample2<int>;
            if (subscriberExample2 != null)
                subscriberExample2.provider = this;
#endif
        }

        public void Start()
        {
            if (ExampleAction != null)
                ExampleAction();

            if (ExampleIntAction != null)
                ExampleIntAction(3);

            if (ExampleFunc != null && ExampleFunc())
                Debug.Log("ExampleFunc returned true");

            if (ExampleFunc != null && ExampleIntFunc(42))
                Debug.Log("ExampleIntFunc returned true");
        }

        public void ExampleMethod(string str1, string str2)
        {
            Debug.Log(str1 + " " + str2);
        }

        public int ExampleMethod(int int1, int int2)
        {
            return int1 + int2;
        }

        public bool TryGetMethod(int input, out int output)
        {
            output = input + 3;
            return true;
        }

        public int ExampleGetter()
        {
            return 0;
        }

        public void LoadProvider()
        {
        }

        public void UnloadProvider()
        {
        }
    }
}
