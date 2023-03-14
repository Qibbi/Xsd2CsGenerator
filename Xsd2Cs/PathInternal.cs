using System.Diagnostics;
using System.IO;
using System;
using Xsd2Cs.Text;
using System.Runtime.CompilerServices;

namespace Xsd2Cs;

internal static class PathInternal
{
    // \\?\, \\.\, \??\
    private const int DevicePrefixLength = 4;
    // \\
    private const int UncPrefixLength = 2;
    // \\?\UNC\, \\.\UNC\
    private const int UncExtendedPrefixLength = 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDirectorySeparator(char c)
    {
        return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
    }

    private static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
    {
        return path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]);
    }

    private static bool EndsInDirectorySeparator(string? path)
    {
        return !string.IsNullOrEmpty(path) && IsDirectorySeparator(path![path.Length - 1]);
    }

    private static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty)
        {
            return true;
        }

        foreach (char c in path)
        {
            if (c != ' ')
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsExtended(ReadOnlySpan<char> path)
    {
        // While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
        // Skipping of normalization will *only* occur if back slashes ('\') are used.
        return path.Length >= DevicePrefixLength && path[0] == '\\' && (path[1] == '\\' || path[1] == '?') && path[2] == '?' && path[3] == '\\';
    }

    private static bool IsDevice(ReadOnlySpan<char> path)
    {
        // If the path begins with any two separators it will be recognized and normalized and prepped with
        // "\??\" for internal usage correctly. "\??\" is recognized and handled, "/??/" is not.
        return IsExtended(path) || (path.Length >= DevicePrefixLength && IsDirectorySeparator(path[0]) && IsDirectorySeparator(path[1]) && (path[2] == '.' || path[2] == '?') && IsDirectorySeparator(path[3]));
    }

    private static bool IsDeviceUNC(ReadOnlySpan<char> path)
    {
        return path.Length >= UncExtendedPrefixLength && IsDevice(path) && IsDirectorySeparator(path[7]) && path[4] == 'U' && path[5] == 'N' && path[6] == 'C';
    }

    private static bool IsValidDriveChar(char value)
    {
        return (uint)((value | 0x20) - 'a') <= (uint)('z' - 'a');
    }

    private static int GetRootLength(ReadOnlySpan<char> path)
    {
        int pathLength = path.Length;
        int index = 0;

        bool deviceSyntax = IsDevice(path);
        bool deviceUnc = deviceSyntax && IsDeviceUNC(path);

        if ((!deviceSyntax || deviceUnc) && pathLength > 0 && IsDirectorySeparator(path[0]))
        {
            // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")
            if (deviceUnc || (pathLength > 1 && IsDirectorySeparator(path[1])))
            {
                // UNC (\\?\UNC\ or \\), scan past server/share

                // Start past the prefix ("\\" or "\\?\UNC\")
                index = deviceUnc ? UncExtendedPrefixLength : UncPrefixLength;

                // Skip two separators at most
                int n = index;
                while (index < pathLength && (!IsDirectorySeparator(path[index]) || --n > 0))
                {
                    index++;
                }
            }
            else
            {
                // Current drive rooted (e.g. "\foo")
                index = 1;
            }
        }
        else if (deviceSyntax)
        {
            // Device path (e.g. "\\?\.", "\\.\")
            // Skip any characters following the prefix that aren't a separator
            index = DevicePrefixLength;
            while (index < pathLength && !IsDirectorySeparator(path[index]))
            {
                index++;
            }

            // If there is another separator take it, as long as we have had at least one
            // non-separator after the prefix (e.g. don't take "\\?\\", but take "\\?\a\")
            if (index < pathLength && index > DevicePrefixLength && IsDirectorySeparator(path[index]))
            {
                index++;
            }
        }
        else if (pathLength >= 2 && path[index] == Path.VolumeSeparatorChar && IsValidDriveChar(path[0]))
        {
            // Valid drive specified path ("C:", "D:", etc.)
            index = 2;

            // If the colon is followed by a directory separator, move past it (e.g. "C:\")
            if (pathLength > 2 && IsDirectorySeparator(path[2]))
            {
                index++;
            }
        }

        return index;
    }

    private static bool AreRootsEqual(string? first, string? second, StringComparison comparisonType)
    {
        int firstRootLength = GetRootLength(first.AsSpan());
        int secondRootLength = GetRootLength(second.AsSpan());

        return firstRootLength == secondRootLength && string.Compare(first, 0, second, 0, firstRootLength, comparisonType) == 0;
    }

    private static unsafe int EqualStartingCharacterCount(string? first, string? second, bool ignoreCase)
    {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
        {
            return 0;
        }

        int commonChars = 0;

        fixed (char* f = first)
        fixed (char* s = second)
        {
            char* l = f;
            char* r = s;
            char* leftEnd = l + first!.Length;
            char* rightEnd = r + second!.Length;

            while (l != leftEnd && r != rightEnd && (*l == *r || (ignoreCase && char.ToUpperInvariant(*l) == char.ToUpperInvariant(*r))))
            {
                commonChars++;
                l++;
                r++;
            }
        }

        return commonChars;
    }

    private static int GetCommonPathLength(string first, string second, bool ignoreCase)
    {
        int commonChars = EqualStartingCharacterCount(first, second, ignoreCase);

        // If nothing matches
        if (commonChars == 0)
        {
            return commonChars;
        }

        // Or we're a full string and equal length or match to a separator
        if (commonChars == first.Length && (commonChars == second.Length || IsDirectorySeparator(second[commonChars])))
        {
            return commonChars;
        }

        if (commonChars == second.Length && IsDirectorySeparator(first[commonChars]))
        {
            return commonChars;
        }

        // It's possible we matched somewhere in the middle of a segment e.g. C:\Foodie and C:\Foobar.
        while (commonChars > 0 && !IsDirectorySeparator(first[commonChars - 1]))
        {
            commonChars--;
        }

        return commonChars;
    }

    public static string GetRelativePath(string relativeTo, string path)
    {
        return GetRelativePath(relativeTo, path, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetRelativePath(string relativeTo, string path, StringComparison comparisonType)
    {
        if (IsEffectivelyEmpty(relativeTo.AsSpan()))
        {
            throw new ArgumentException("Path is empty!", nameof(relativeTo));
        }
        if (IsEffectivelyEmpty(path.AsSpan()))
        {
            throw new ArgumentException("Path is empty!", nameof(path));
        }

        Debug.Assert(comparisonType is StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase);

        relativeTo = Path.GetFullPath(relativeTo);
        path = Path.GetFullPath(path);

        // Need to check if the roots are different - if they are we need to return the "to" path.
        if (!AreRootsEqual(relativeTo, path, comparisonType))
        {
            return path;
        }

        int commonLength = GetCommonPathLength(relativeTo, path, comparisonType == StringComparison.OrdinalIgnoreCase);

        // If there is nothing in common they can't share the same root, return the "to" path as is.
        if (commonLength == 0)
        {
            return path;
        }

        // Trailing separators aren't significant for comparison
        int relativeToLength = relativeTo.Length;
        if (EndsInDirectorySeparator(relativeTo.AsSpan()))
        {
            relativeToLength--;
        }

        bool pathEndsInSeparator = EndsInDirectorySeparator(path.AsSpan());
        int pathLength = path.Length;
        if (pathEndsInSeparator)
        {
            pathLength--;
        }

        // If we have effectively the same path, return "."
        if (relativeToLength == pathLength && commonLength >= relativeToLength)
        {
            return ".";
        }

        // We have the same root, we need to calculate the difference now using the
        // common Length and Segment count past the length.
        // 
        // Some examples:
        // 
        //  C:\Foo C:\Bar L3, S1 -> ..\Bar
        //  C:\Foo C:\Foo\Bar L6, S0 -> Bar
        //  C:\Foo\Bar C:\Bar\Bar L3, S2 -> ..\..\Bar\Bar
        //  C:\Foo\Foo C:\Foo\Bar L7, S1 -> ..\Bar

        ValueStringBuilder sb = new(stackalloc char[260]);
        sb.EnsureCapacity(Math.Max(relativeTo.Length, path.Length));

        // Add parent segments for segments past the common on the "from" path
        if (commonLength < relativeToLength)
        {
            sb.Append("..");

            for (int idx = commonLength + 1; idx < relativeToLength; ++idx)
            {
                if (IsDirectorySeparator(relativeTo[idx]))
                {
                    sb.Append(Path.DirectorySeparatorChar);
                    sb.Append("..");
                }
            }
        }
        else if (IsDirectorySeparator(path[commonLength]))
        {
            // No parent segments and we need to eat the initial separator
            //  (C:\Foo C:\Foo\Bar case)
            commonLength++;
        }

        // Now add the rest of the "to" path, adding back the trailing separator
        int differenceLength = pathLength - commonLength;
        if (pathEndsInSeparator)
        {
            differenceLength++;
        }

        if (differenceLength > 0)
        {
            if (sb.Length > 0)
            {
                sb.Append(Path.DirectorySeparatorChar);
            }

            sb.Append(path.AsSpan(commonLength, differenceLength));
        }

        return sb.ToString();
    }
}
