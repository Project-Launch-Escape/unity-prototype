using Unity.Entities;

/// <summary>
/// Stores a part's child parts. If the attached part is moved or deleted, all of its children will move as well.
/// </summary>
public struct PartChildBuffer : IBufferElementData {
    public Entity Value;
}
