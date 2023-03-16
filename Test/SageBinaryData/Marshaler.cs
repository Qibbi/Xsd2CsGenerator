using Relo;
using Xml;

namespace SageBinaryData;

public static partial class Marshaler
{
    public const float PI = 3.14159265359f;
    public const float RADS_PER_DEGREE = PI / 180.0f;
    public const float LOGICFRAMES_PER_SECOND = 5;
    public const float MSEC_PER_SECOND = 1000;
    public const float LOGICFRAMES_PER_MSEC_REAL = LOGICFRAMES_PER_SECOND / MSEC_PER_SECOND;
    public const float LOGICFRAMES_PER_SECONDS_REAL = LOGICFRAMES_PER_SECOND;
    public const float SECONDS_PER_LOGICFRAME_REAL = 1.0f / LOGICFRAMES_PER_SECONDS_REAL;

    public static readonly char[] WhiteSpaces = new[] { ' ', '\t', '\n', '\v', '\f', '\r' };

    public static void Marshal(Node? node, ref BaseAssetType objT, Relo.Tracker state)
    {
    }

    public static void Marshal(Node? node, ref BaseInheritableAsset objT, Relo.Tracker state)
    {
    }

    public static void Marshal<T>(ReadOnlySpan<char> text, ref AssetReference<T> objT, Tracker state) where T : unmanaged
    {
        int index = text.IndexOf('\\');
        if (index == -1)
        {
            return;
        }
        int value = int.Parse(text[(index + 1)..]);
        state.AddReference(ref objT, value);
    }

    public static void Marshal<T>(Value? value, ref AssetReference<T> objT, Tracker state) where T : unmanaged
    {
        if (value is null)
        {
            return;
        }

        Marshal(value.Text, ref objT, state);
    }

    public static void Marshal<T>(Node? node, ref AssetReference<T> objT, Tracker state) where T : unmanaged
    {
        if (node is null)
        {
            return;
        }

        Marshal(node.Value, ref objT, state);
    }
}
