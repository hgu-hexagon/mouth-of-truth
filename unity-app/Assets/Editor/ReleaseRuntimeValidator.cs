using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor.Build;

namespace MouthOfTruth.Editor
{
    internal static class ReleaseRuntimeValidator
    {
        private static readonly RequiredReleaseFile[] REQUIRED_RELEASE_FILES =
        {
            new RequiredReleaseFile("python-engine/requirements.txt", string.Empty),
            new RequiredReleaseFile("python-engine/environment.yml", string.Empty),
            new RequiredReleaseFile("python-engine/src/mouth_of_truth/runners/bridge_analysis_runner.py", string.Empty),
            new RequiredReleaseFile("python-engine/src/mouth_of_truth/runners/bridge_analysis_worker.py", string.Empty),
            new RequiredReleaseFile("python-engine/scripts/run_bridge_analysis.sh", string.Empty),
            new RequiredReleaseFile("python-engine/scripts/run_bridge_analysis.bat", string.Empty),
            new RequiredReleaseFile("python-engine/scripts/run_bridge_analysis_worker.sh", string.Empty),
            new RequiredReleaseFile("python-engine/scripts/run_bridge_analysis_worker.bat", string.Empty),
            new RequiredReleaseFile("python-engine/models/face/yolo26x_rafdb_best.pt", "48e47f019b8214b4c6869af87a3ab8a23fa34a0e891a6d4caf7fd25f7492e35a"),
            new RequiredReleaseFile("python-engine/models/voice/best_wav2vec2_iemocap/config.json", "e80a86c0d4e859cd46cc852d4f5864f3de78be8e64f47c1f79b31b687099f5be"),
            new RequiredReleaseFile("python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors", "699c55de39fddb538eee49a24afc1008a20bb78918b7a50429b63b59dc62f5c3"),
            new RequiredReleaseFile("python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json", "8cdfd65ff4115423185a1512bdae100e2e0cd744f5b322417429944aaafd0827"),
        };

        public static void ValidateProjectRuntimeAssets(string runtimeRootPath)
        {
            validateReleaseFiles(runtimeRootPath, "project runtime");
        }

        public static void ValidateDistributionRuntimeAssets(string distributionRootPath)
        {
            validateReleaseFiles(distributionRootPath, "distribution runtime");
        }

        private static void validateReleaseFiles(string releaseRootPath, string releaseRootDescription)
        {
            if (string.IsNullOrWhiteSpace(releaseRootPath))
            {
                throw new BuildFailedException("Release runtime root path is empty.");
            }

            assertDirectoryExists(Path.Combine(releaseRootPath, "python-engine", "src"), releaseRootDescription);
            assertDirectoryExists(Path.Combine(releaseRootPath, "python-engine", "scripts"), releaseRootDescription);
            assertDirectoryExists(Path.Combine(releaseRootPath, "python-engine", "models"), releaseRootDescription);

            foreach (RequiredReleaseFile requiredReleaseFile in REQUIRED_RELEASE_FILES)
            {
                validateRequiredFile(releaseRootPath, releaseRootDescription, requiredReleaseFile);
            }
        }

        private static void assertDirectoryExists(string directoryPath, string releaseRootDescription)
        {
            if (Directory.Exists(directoryPath))
            {
                return;
            }

            throw new BuildFailedException($"Required {releaseRootDescription} directory is missing: {directoryPath}");
        }

        private static void validateRequiredFile(string releaseRootPath, string releaseRootDescription, RequiredReleaseFile requiredReleaseFile)
        {
            string filePath = Path.Combine(releaseRootPath, requiredReleaseFile.RelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(filePath) == false)
            {
                throw new BuildFailedException($"Required {releaseRootDescription} file is missing: {filePath}");
            }

            if (requiredReleaseFile.HasSha256Hash == false)
            {
                return;
            }

            string actualSha256Hash = calculateSha256Hash(filePath);

            if (string.Equals(actualSha256Hash, requiredReleaseFile.Sha256Hash, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new BuildFailedException(
                $"Required {releaseRootDescription} file has an unexpected SHA-256 hash: {filePath}\n"
                + $"expected: {requiredReleaseFile.Sha256Hash}\n"
                + $"actual:   {actualSha256Hash}");
        }

        private static string calculateSha256Hash(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(fileStream);
                StringBuilder hashBuilder = new StringBuilder(hashBytes.Length * 2);

                foreach (byte hashByte in hashBytes)
                {
                    hashBuilder.Append(hashByte.ToString("x2"));
                }

                return hashBuilder.ToString();
            }
        }

        private sealed class RequiredReleaseFile
        {
            public string RelativePath
            {
                get;
            }

            public string Sha256Hash
            {
                get;
            }

            public bool HasSha256Hash
            {
                get;
            }

            public RequiredReleaseFile(string relativePath, string sha256Hash)
            {
                RelativePath = relativePath;
                Sha256Hash = sha256Hash;
                HasSha256Hash = string.IsNullOrWhiteSpace(sha256Hash) == false;
            }
        }
    }
}
