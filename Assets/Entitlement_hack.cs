#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public class HACK_MARS
{
    static HACK_MARS()
    {
        EditorPrefs.SetString("MARS.last_time_entitled", DateTime.UtcNow.ToString());
        //EditorPrefs.DeleteKey("MARS.last_time_entitled");
        Debug.Log("Hacking");
    }
}
#endif