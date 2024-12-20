#if UNITY_EDITOR
using iffnsStuff.MarchingCubeEditor.Core;
using UnityEngine;

public class VisualisationManager : MonoBehaviour
{
    MarchingCubesController linkedController;
    Transform linkedControllerTransform;

    public bool ShowGridOutline = true;

    public bool InvertNormals
    {
        set
        {
            linkedController.InvertAllNormals = value;
        }
    }

    public void Initialize(MarchingCubesController linkedController)
    {
        this.linkedController = linkedController;
        linkedControllerTransform = linkedController.transform;
    }

    private void OnDrawGizmos()
    {
        if (!ShowGridOutline) return;

        if (linkedController == null || !linkedController.IsInitialized) return;

        Vector3 outlineSize = new Vector3(linkedController.GridResolutionX - 1, linkedController.GridResolutionY - 1, linkedController.GridResolutionZ - 1);

        Gizmos.color = Color.cyan; // Set outline color
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, outlineSize);
        Gizmos.DrawWireCube(Vector3.one / 2f, Vector3.one);
    }
}
#endif