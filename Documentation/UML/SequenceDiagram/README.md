sequenceDiagram
    actor User

    User ->> MarchingCubeEditor: Interacts with UI
    MarchingCubeEditor ->> MarchingCubesController: AddShape(selectedShape, updateCollider)

    activate MarchingCubesController
    MarchingCubesController ->> EditShape: PrecomputeTransform(gridTransform)
    activate EditShape
    EditShape -->> MarchingCubesController: PrecomputedTransformMatrix
    deactivate EditShape

    loop Modify affected grid
        MarchingCubesController ->> MarchingCubesModel: GetVoxel(x, y, z)
        MarchingCubesController ->> IVoxelModifier: ModifyVoxel(x, y, z, currentValue, distance)
        IVoxelModifier -->> MarchingCubesController: ModifiedVoxelValue
        MarchingCubesController ->> MarchingCubesModel: SetVoxel(x, y, z, newValue)
    end

    MarchingCubesController ->> MarchingCubesView: MarkAffectedChunksDirty(minGrid, maxGrid)
    MarchingCubesController ->> MarchingCubesView: UpdateAffectedChunks(minGrid, maxGrid, enableCollider)

    loop Update chunk meshes
        MarchingCubesView ->> MarchingCubesModel: GetCubeWeights(x, y, z)
        MarchingCubesView ->> MarchingCubesMeshData: GenerateCubeMesh(cubeWeights, x, y, z)
        MarchingCubesMeshData -->> MarchingCubesView: GeneratedMeshData
        MarchingCubesView ->> MarchingCubesView: UpdateMesh(meshData, enableCollider)
    end

    deactivate MarchingCubesController