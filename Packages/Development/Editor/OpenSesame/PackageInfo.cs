using System.IO;

namespace Coffee.OpenSesame
{
    internal struct PackageInfo
    {
        public string packageId { get; }
        public string path { get; }

        public bool isValid => !string.IsNullOrEmpty(path);

        private PackageInfo(string packageId, string path)
        {
            this.packageId = packageId;
            this.path = path;
        }

        public static PackageInfo GetInstalledInfo(string packageId)
        {
            var path = Utils.InstallNugetPackage(packageId);
            if (string.IsNullOrEmpty(path)) return new PackageInfo(packageId, "");

            // Find csc.dll
            foreach (var dll in Directory.GetFiles(path, "csc.dll", SearchOption.AllDirectories))
            {
                return new PackageInfo(packageId, dll);
            }

            return new PackageInfo(packageId, "");
        }
    }
}
