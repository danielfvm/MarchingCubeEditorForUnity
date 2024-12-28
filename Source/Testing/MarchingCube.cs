#if UNITY_EDITOR

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

struct Vertex
{
    public Vector3 pos;
    public Vector3 normal;
};

[ExecuteInEditMode]
public class MarchingCube : MonoBehaviour
{
    [Header("Settings")]
    public Vector3Int gridSize;
    public float sphereRadius;
    public float isoSurface;
    public bool invertNormals;

    [Header("References")]
    public ComputeShader marchingCubeCompute;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    
    #region Local Variables
    private GraphicsBuffer vertexBuffer, indexBuffer;
    private ComputeBuffer dataBuffer, counterBuffer, internalCounterBuffer;
    private Mesh renderMesh, colliderMesh;
    #endregion

    public int VoxelCount => gridSize.x * gridSize.y * gridSize.z;
    public int TriangleBudget => VoxelCount * 5; // Maximum Amount of Triangles. This is probably wrong.

    private void OnDestroy() 
    {
        internalCounterBuffer.Dispose();
        counterBuffer.Dispose();
        dataBuffer.Dispose();
    }

    // This will generate a sphere for testing
    public void GenerateData() 
    {
        Vector3Int center = gridSize / 2;
        float[] data = new float[VoxelCount];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    float distance = sphereRadius - Vector3.Distance(center, offset);

                    data[(z * gridSize.y + y) * gridSize.x + x] = distance;
                }   
            }
        }

        if (dataBuffer != null) 
            dataBuffer.Dispose();
        
        dataBuffer = new ComputeBuffer(VoxelCount, sizeof(float));
        dataBuffer.SetData(data);

        AllocateMesh(TriangleBudget * 3);
        meshFilter.sharedMesh = renderMesh;
    }

    public void GenerateMesh() 
    {
        if (counterBuffer != null) 
            counterBuffer.Dispose();
        
        counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
        counterBuffer.SetCounterValue(0);

        if (internalCounterBuffer != null) 
            internalCounterBuffer.Dispose();
        
        internalCounterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
        internalCounterBuffer.SetCounterValue(0);
        
        // Shader uniforms
        marchingCubeCompute.SetVector("GridSize", new Vector4(gridSize.x, gridSize.y, gridSize.z, 0));
        marchingCubeCompute.SetInt("MaxTriangle", TriangleBudget);
        marchingCubeCompute.SetFloat("IsoSurface", isoSurface);
        marchingCubeCompute.SetBool("InvertNormals", invertNormals);
        
        // Isosurface reconstruction
        int meshReconstructionKernel = marchingCubeCompute.FindKernel("MeshReconstruction");
        marchingCubeCompute.SetBuffer(meshReconstructionKernel, "IndexBuffer", indexBuffer);
        marchingCubeCompute.SetBuffer(meshReconstructionKernel, "VertexBuffer", vertexBuffer);
        marchingCubeCompute.SetBuffer(meshReconstructionKernel, "DataBuffer", dataBuffer);
        marchingCubeCompute.SetBuffer(meshReconstructionKernel, "InternalCounter", internalCounterBuffer);
        marchingCubeCompute.SetBuffer(meshReconstructionKernel, "Counter", counterBuffer);
        marchingCubeCompute.Dispatch(meshReconstructionKernel, gridSize.x, gridSize.y, gridSize.z);

        // Clear unused area of the buffers.
        int clearUnusedKernel = marchingCubeCompute.FindKernel("ClearUnused");
        marchingCubeCompute.SetBuffer(clearUnusedKernel, "VertexBuffer", vertexBuffer);
        marchingCubeCompute.SetBuffer(clearUnusedKernel, "IndexBuffer", indexBuffer);
        marchingCubeCompute.SetBuffer(clearUnusedKernel, "InternalCounter", internalCounterBuffer);
        marchingCubeCompute.Dispatch(clearUnusedKernel, 1024, 1, 1);
    }

    public IEnumerator GenerateCollider() 
    {
        // For some reason the following code only returns 0 for count, count would be good to have so that vertex
        // and index readback dont readback the full buffer but just the amount that was set.
        AsyncGPUReadbackRequest counterRequest = AsyncGPUReadback.Request(counterBuffer);

        while (!counterRequest.done)
            yield return false;

        if (counterRequest.hasError)
        {
            Debug.LogError("Counter Readback has an error");
            yield break;
        }

        int vertexCount = counterRequest.GetData<int>()[0] * 3;
        int vertexSize = sizeof(float) * 3 * 2; // pos + normal
        int indexSize = sizeof(uint);

        AsyncGPUReadbackRequest vertexRequest = AsyncGPUReadback.Request(vertexBuffer, vertexCount * vertexSize, 0);

        while (!vertexRequest.done)
            yield return false;

        if (vertexRequest.hasError)
        {
            Debug.LogError("Vertex Readback has an error");
            yield break;
        }

        var vertices = vertexRequest.GetData<Vertex>();
        colliderMesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);

        AsyncGPUReadbackRequest indexRequest = AsyncGPUReadback.Request(indexBuffer, vertexCount * indexSize, 0);

        while (!indexRequest.done)
            yield return false;

        if (indexRequest.hasError)
        {
            Debug.LogError("Index Readback has an error");
            yield break;
        }

        var indices = indexRequest.GetData<uint>();
        colliderMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        meshCollider.sharedMesh = colliderMesh;
        
        yield return true;
    }

    private void AllocateMesh(int vertexCount)
    {
        renderMesh = new Mesh();
        renderMesh.bounds = new Bounds(Vector3.one / 2, Vector3.one);

        colliderMesh = new Mesh();
        colliderMesh.bounds = new Bounds(Vector3.one / 2, Vector3.one);

        // We want GraphicsBuffer access as Raw (ByteAddress) buffers.
        renderMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        renderMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        };

        // Vertex/index buffer formats
        renderMesh.SetVertexBufferParams(vertexCount, layout);
        renderMesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);

        // Submesh initialization
        renderMesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount), MeshUpdateFlags.DontRecalculateBounds);

        // GraphicsBuffer references
        vertexBuffer = renderMesh.GetVertexBuffer(0);
        indexBuffer = renderMesh.GetIndexBuffer();

        colliderMesh.SetVertexBufferParams(vertexCount, layout);
        colliderMesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);
    }
}

#endif