using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal static class PathUitls
{
    public static string MakeRelativePath(string fromPath, string toPath)
    {
        // MONO BUG: https://github.com/mono/mono/pull/471
        // In the editor, Application.dataPath returns <Project Folder>/Assets. There is a bug in
        // mono for method Uri.GetRelativeUri where if the path ends in a folder, it will
        // ignore the last part of the path. Thus, we need to add fake depth to get the "real"
        // relative path.
        fromPath += "/fake_depth";
        try
        {
            if (string.IsNullOrEmpty(fromPath))
                return toPath;

            if (string.IsNullOrEmpty(toPath))
                return "";

            var fromUri = new System.Uri(fromPath);
            var toUri = new System.Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
                return toPath;

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = System.Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath;
        }
        catch
        {
            return toPath;
        }
    }
}
