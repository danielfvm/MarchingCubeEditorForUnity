#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

[CustomEditor(typeof(MarchingCube))]
public class MarchingCubeInspector : Editor 
{
    private MarchingCube Target => (MarchingCube)target;
    
    public override void OnInspectorGUI() 
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate Data"))
        {
            Measure("GenerateData", () => Target.GenerateData());
        }

        if (GUILayout.Button("Generate Mesh"))
        {
            Measure("GenerateMesh", () => Target.GenerateMesh());
        }

        if (GUILayout.Button("Generate Collider"))
        {
            Measure("GenerateCollider", () => Target.GenerateCollider());
        }
    }

    public void Measure(string name, Func<IEnumerator> func)
    {
        IEnumerator Routine()
        {
            float start = Time.realtimeSinceStartup;
            yield return func();
            float end = Time.realtimeSinceStartup;

            float time =  end - start;
            Debug.Log($"<color=blue>[TIME]</color> {name} {time}s");
        }

        Target.StartCoroutine(Routine());
    }

    public void Measure(string name, Action func)
    {
        float start = Time.realtimeSinceStartup;
        func();
        float end = Time.realtimeSinceStartup;

        float time =  end - start;
        Debug.Log($"<color=blue>[TIME]</color> {name} {time}s");
    }
}

#endif