using Unity.Entities;

public struct PartComponent : IComponentData
{
    public int layer; // this name might not be representative, if a part is placed on nothing , layer =1 , if placed on a part placed on nothing layer=2 ... // maybe start at zero could be better idk
    public int id;
    public int selfplace; // this is like which sibling it is , the first one ? the second one ? // ######
}