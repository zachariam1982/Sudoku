#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

public sealed class AndroidR8OptimizationPostprocessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // Unity passes the unityLibrary module path. Resource shrinking and R8
        // configuration belong to the application (launcher) module.
        var unityLibraryDirectory = Path.GetFullPath(path);
        var gradleRoot = Directory.GetParent(unityLibraryDirectory);
        if (gradleRoot == null)
        {
            throw new BuildFailedException("Could not locate the generated Gradle project root.");
        }

        var launcherBuildFile = Path.Combine(gradleRoot.FullName, "launcher", "build.gradle");
        if (!File.Exists(launcherBuildFile))
        {
            throw new BuildFailedException(
                "Could not find launcher/build.gradle to enable release R8 optimization.");
        }

        var original = File.ReadAllText(launcherBuildFile);
        var updated = ConfigureReleaseBuild(original);
        if (!String.Equals(original, updated, StringComparison.Ordinal))
        {
            File.WriteAllText(launcherBuildFile, updated, new UTF8Encoding(false));
            Debug.Log("[Android R8] Enabled optimized ProGuard rules and resource shrinking for release.");
        }
    }

    private static string ConfigureReleaseBuild(string gradle)
    {
        var buildTypesMatch = Regex.Match(
            gradle,
            @"(?m)^[ \t]*buildTypes[ \t]*\{");
        if (!buildTypesMatch.Success)
        {
            throw new BuildFailedException("Could not find buildTypes in launcher/build.gradle.");
        }

        var buildTypesOpen = gradle.IndexOf('{', buildTypesMatch.Index);
        var buildTypesClose = FindMatchingBrace(gradle, buildTypesOpen);
        if (buildTypesClose < 0)
        {
            throw new BuildFailedException("Could not parse buildTypes in launcher/build.gradle.");
        }

        var releaseMatch = Regex.Match(
            gradle.Substring(buildTypesOpen + 1, buildTypesClose - buildTypesOpen - 1),
            @"(?m)^[ \t]*release[ \t]*\{");
        if (!releaseMatch.Success)
        {
            throw new BuildFailedException("Could not find the release build type in launcher/build.gradle.");
        }

        var releaseOpen = buildTypesOpen + 1 + releaseMatch.Index;
        releaseOpen = gradle.IndexOf('{', releaseOpen);
        var releaseClose = FindMatchingBrace(gradle, releaseOpen);
        if (releaseClose < 0 || releaseClose > buildTypesClose)
        {
            throw new BuildFailedException("Could not parse the release build type in launcher/build.gradle.");
        }

        var releaseBlock = gradle.Substring(releaseOpen, releaseClose - releaseOpen + 1);
        if (!Regex.IsMatch(releaseBlock, @"\bminifyEnabled\s+true\b"))
        {
            throw new BuildFailedException(
                "Release minification is not enabled. Keep Android Minify Release enabled before enabling resource shrinking.");
        }

        releaseBlock = Regex.Replace(
            releaseBlock,
            @"getDefaultProguardFile\(\s*['""]proguard-android\.txt['""]\s*\)",
            "getDefaultProguardFile('proguard-android-optimize.txt')");

        if (!Regex.IsMatch(releaseBlock, @"\bshrinkResources\s+true\b"))
        {
            var existingShrinkSetting = Regex.Match(releaseBlock, @"\bshrinkResources\s+false\b");
            if (existingShrinkSetting.Success)
            {
                releaseBlock = Regex.Replace(releaseBlock, @"\bshrinkResources\s+false\b", "shrinkResources true");
            }
            else
            {
                releaseBlock = releaseBlock.Insert(releaseBlock.IndexOf('{') + 1, "\n            shrinkResources true");
            }
        }

        return gradle.Substring(0, releaseOpen)
            + releaseBlock
            + gradle.Substring(releaseClose + 1);
    }

    private static int FindMatchingBrace(string text, int openBrace)
    {
        if (openBrace < 0 || openBrace >= text.Length || text[openBrace] != '{')
        {
            return -1;
        }

        var depth = 0;
        var quote = '\0';
        var escaped = false;
        var lineComment = false;
        var blockComment = false;

        for (var i = openBrace; i < text.Length; i++)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (lineComment)
            {
                if (current == '\n')
                {
                    lineComment = false;
                }
                continue;
            }

            if (blockComment)
            {
                if (current == '*' && next == '/')
                {
                    blockComment = false;
                    i++;
                }
                continue;
            }

            if (quote != '\0')
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == quote)
                {
                    quote = '\0';
                }
                continue;
            }

            if (current == '/' && next == '/')
            {
                lineComment = true;
                i++;
                continue;
            }

            if (current == '/' && next == '*')
            {
                blockComment = true;
                i++;
                continue;
            }

            if (current == '\'' || current == '"')
            {
                quote = current;
                continue;
            }

            if (current == '{')
            {
                depth++;
            }
            else if (current == '}' && --depth == 0)
            {
                return i;
            }
        }

        return -1;
    }
}
#endif
