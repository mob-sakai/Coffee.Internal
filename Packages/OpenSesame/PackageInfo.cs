using System.IO;

namespace Coffee.OpenSesame
{
    internal struct PackageInfo
    {
        public string packageId { get; }
        public string path { get; }

        public bool isValid => !string.IsNullOrEmpty(path);
        public bool isDotNet => path.EndsWith(".dll");

        private PackageInfo(string packageId, string path)
        {
            this.packageId = packageId;
            this.path = path;
        }

        public static PackageInfo GetInstalledInfo(string packageId)
        {
            var path = Utils.InstallNugetPackage(packageId);
            if (string.IsNullOrEmpty(path)) return new PackageInfo(packageId, "");

            // DotNet version (Runtime: dotnet)
            foreach (var dll in Directory.GetFiles(path, "csc.dll", SearchOption.AllDirectories))
            {
                return new PackageInfo(packageId, dll);
            }

            // Net Framework version (Runtime: mono)
            foreach (var dll in Directory.GetFiles(path, "csc.exe", SearchOption.AllDirectories))
            {
                return new PackageInfo(packageId, dll);
            }

            return new PackageInfo(packageId, "");
        }
    }
}
