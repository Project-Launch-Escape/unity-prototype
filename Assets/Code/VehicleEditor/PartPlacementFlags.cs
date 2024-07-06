using System;

/// <summary>
/// Defines what surfaces a part can be placed on (or snap points it can snap to).
/// </summary>
[Flags]
public enum PartPlacementFlags {
    None = 0,
    Surface = 1,
    Top = 2,
    Bottom = 4,
}
