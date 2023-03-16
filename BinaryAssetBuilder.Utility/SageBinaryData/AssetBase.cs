using System.Runtime.InteropServices;

namespace SageBinaryData;

public interface IBaseAssetTypeInterface
{
}

public struct BaseAssetType : IBaseAssetTypeInterface
{
}

public interface IBaseInheritableAssetInterface : IBaseAssetTypeInterface
{
}

public struct BaseInheritableAsset : IBaseInheritableAssetInterface
{
}

public interface IRGBColorInterface
{
    float R { get; set; }
    float G { get; set; }
    float B { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct RGBColor : IRGBColorInterface
{
    internal float _r;
    internal float _g;
    internal float _b;

    public float R { get => _r; set => _r = value; }
    public float G { get => _g; set => _g = value; }
    public float B { get => _b; set => _b = value; }
}

public interface IRGBAColorInterface : IRGBColorInterface
{
    float A { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct RGBAColor : IRGBAColorInterface
{
    public RGBColor _base;

    internal float _a;

    public float R { get => _base._r; set => _base._r = value; }
    public float G { get => _base._g; set => _base._g = value; }
    public float B { get => _base._b; set => _base._b = value; }
    public float A { get => _a; set => _a = value; }
}

public interface ICoord2DInterface
{
    float x { get; set; }
    float y { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct Coord2D : ICoord2DInterface
{
    internal float _x;
    internal float _y;

    public float x { get => _x; set => _x = value; }
    public float y { get => _y; set => _y = value; }
}

public interface IICoord2DInterface
{
    int x { get; set; }
    int y { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct ICoord2D : IICoord2DInterface
{
    internal int _x;
    internal int _y;

    public int x { get => _x; set => _x = value; }
    public int y { get => _y; set => _y = value; }
}

public interface ICoord3DInterface
{
    float x { get; set; }
    float y { get; set; }
    float z { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct Coord3D : ICoord3DInterface
{
    internal float _x;
    internal float _y;
    internal float _z;

    public float x { get => _x; set => _x = value; }
    public float y { get => _y; set => _y = value; }
    public float z { get => _z; set => _z = value; }
}

public interface IVector2Interface
{
    float X { get; set; }
    float Y { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct Vector2 : IVector2Interface
{
    internal float _x;
    internal float _y;

    public float X { get => _x; set => _x = value; }
    public float Y { get => _y; set => _y = value; }
}

public interface IVector3Interface
{
    float X { get; set; }
    float Y { get; set; }
    float Z { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct Vector3 : IVector3Interface
{
    internal float _x;
    internal float _y;
    internal float _z;

    public float X { get => _x; set => _x = value; }
    public float Y { get => _y; set => _y = value; }
    public float Z { get => _z; set => _z = value; }
}

public interface IVector4Interface
{
    float X { get; set; }
    float Y { get; set; }
    float Z { get; set; }
    float W { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct Vector4 : IVector4Interface
{
    internal float _x;
    internal float _y;
    internal float _z;
    internal float _w;

    public float X { get => _x; set => _x = value; }
    public float Y { get => _y; set => _y = value; }
    public float Z { get => _z; set => _z = value; }
    public float W { get => _w; set => _w = value; }
}

public interface IQuaternionInterface
{
    float X { get; set; }
    float Y { get; set; }
    float Z { get; set; }
    float W { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct Quaternion : IQuaternionInterface
{
    internal float _x;
    internal float _y;
    internal float _z;
    internal float _w;

    public float X { get => _x; set => _x = value; }
    public float Y { get => _y; set => _y = value; }
    public float Z { get => _z; set => _z = value; }
    public float W { get => _w; set => _w = value; }
}
