using Unity.Entities;

/// <summary>
/// Stores part prefabs. All created parts are instantiated from entities in this buffer.
/// </summary>
public struct PartsBuffer : IBufferElementData {
    public Entity Value;
}
