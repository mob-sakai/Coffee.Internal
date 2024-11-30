using System.IO;
using System.Text.RegularExpressions;
using Coffee.MinimalResource;
using NUnit.Framework;
using UnityEngine;

internal class MinimalResourceTest
{
    [Test]
    public void FindResourceDir()
    {
        var exist = Directory.Exists(Compiler.k_ResourceDir);
        Assert.IsTrue(exist, "{0} is not found.", Compiler.k_ResourceDir);
    }

    [Test]
    public void GetBuiltinDotNetRuntimePath()
    {
        var path = Compiler.GetBuiltinDotNetRuntimePath();
        Assert.IsNotNull(path);

        var exist = File.Exists(path);
        Assert.IsTrue(exist);
    }

    [Test]
    public void GetBuiltinCscPath()
    {
        var path = Compiler.GetBuiltinCscPath();
        Assert.IsNotNull(path);

        var exist = File.Exists(path);
        Assert.IsTrue(exist);
    }

    [Test]
    public void FindCoreLib()
    {
        var path = Compiler.FindCoreLib();
        Assert.IsNotNull(path);

        var exist = File.Exists(path);
        Assert.IsTrue(exist);
    }

    [Test]
    public void FindStandardLib()
    {
        var path = Compiler.FindStandardLib();
        Assert.IsNotNull(path);

        var exist = File.Exists(path);
        Assert.IsTrue(exist);
    }

    [Test]
    public void FindMscorlib()
    {
        var path = Compiler.FindMscorlib();
        Assert.IsNotNull(path);

        var exist = File.Exists(path);
        Assert.IsTrue(exist);
    }

    [Test]
    public void GetBuiltinDotNetRuntimeVersion()
    {
        var dotnet = Compiler.GetBuiltinDotNetRuntimePath();
        Assert.IsNotNull(dotnet);

        var (code, stdout) = Compiler.ExecuteCommand(dotnet, "--info");
        var version = Regex.Match(stdout, "Version:\\s*(.*)$", RegexOptions.Multiline).Groups[1].Value;
        Debug.Log(version);
    }

    [Test]
    public void Build()
    {
        const string outPath = "Temp/R.dll";
        if (File.Exists(outPath))
        {
            File.Delete(outPath);
        }

        Compiler.Build(outPath);

        var exist = File.Exists(outPath);
        Assert.IsTrue(exist);
    }
}
