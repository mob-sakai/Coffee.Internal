using System.IO;
using System.Text.RegularExpressions;
using Coffee.OpenSesame;
using NUnit.Framework;
using UnityEngine;
using PackageInfo = Coffee.OpenSesame.PackageInfo;

internal class OpenSesameTest
{
    [Test]
    public void Install_NotFoundPackage()
    {
        var packageId = "OpenSesame.Net.Compilers.Toolset.x.x.x";
        Assert.That(() => PackageInfo.GetInstalledInfo(packageId), Throws.TypeOf<System.Exception>());
    }

    [Test]
    public void Install_OpenSesameNetCompilers()
    {
        var packageId = "OpenSesame.Net.Compilers.4.0.1";
        var packagesDir = Path.Combine("Library", "InstalledPackages", packageId);
        if (Directory.Exists(packagesDir))
        {
            Directory.Delete(packagesDir, true);
        }

        var info = PackageInfo.GetInstalledInfo(packageId);
        Assert.IsTrue(info.isValid);
        Assert.IsFalse(info.isDotNet);

        var expected = Path.Combine(packagesDir, "tools", "csc.exe");
        Assert.AreEqual(expected, info.path);
    }

    [Test]
    public void Install_OpenSesameNetCompilersToolset()
    {
        var packageId = "OpenSesame.Net.Compilers.Toolset.4.0.1";
        var packagesDir = Path.Combine("Library", "InstalledPackages", packageId);
        if (Directory.Exists(packagesDir))
        {
            Directory.Delete(packagesDir, true);
        }

        var info = PackageInfo.GetInstalledInfo(packageId);
        Assert.IsTrue(info.isValid);
        Assert.IsTrue(info.isDotNet);

        var expected = Path.Combine(packagesDir, "tasks", "netcoreapp3.1", "bincore", "csc.dll");
        Assert.AreEqual(expected, info.path);
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
    public void GetBuiltinDotNetRuntimeVersion()
    {
        var dotnet = Compiler.GetBuiltinDotNetRuntimePath();
        Assert.IsNotNull(dotnet);

        var (code, stdout) = Utils.ExecuteCommand(dotnet, "--info");
        var version = Regex.Match(stdout, "Version:\\s*(.*)$", RegexOptions.Multiline).Groups[1].Value;
        Debug.Log(version);
    }

    [Test]
    public void GetBuiltinMonoRuntimePath()
    {
        var path = Compiler.GetBuiltinMonoRuntimePath();
        Assert.IsNotNull(path);

        var exist = File.Exists(path);
        Assert.IsTrue(exist);
    }

    [Test]
    public void GetResponseFilePath()
    {
        var path = Compiler.GetResponseFilePath("Coffee.OpenSesame.Test");

        // Skip
        if (string.IsNullOrEmpty(path)) return;

        Assert.IsTrue(File.Exists(path));
    }

    [Test]
    public void Build()
    {
        const string asmName = "Coffee.OpenSesame.Test.generated";
        foreach (var path in Directory.GetFiles("Temp", $"{asmName}.*", SearchOption.TopDirectoryOnly))
        {
            File.Delete(path);
        }

        // Skip
        if (string.IsNullOrEmpty(Compiler.GetResponseFilePath("Coffee.OpenSesame.Test"))) return;

        var output = Path.Combine("Temp", $"{asmName}.dll");
        Compiler.Build("Coffee.OpenSesame.Test", output, CompileOptions.XmlDoc | CompileOptions.RefDll);
        Assert.IsTrue(File.Exists(output), "generated dll file not found.");
        Assert.IsTrue(File.Exists(Path.Combine("Temp", $"{asmName}.xml")), "xml file not found.");
        Assert.IsTrue(File.Exists(Path.Combine("Temp", $"{asmName}.pdb")), "pdb file not found.");
        Assert.IsTrue(File.Exists(Path.Combine("Temp", $"{asmName}.ref.dll")), "ref.dll file not found.");
    }
}
