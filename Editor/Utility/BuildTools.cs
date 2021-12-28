using UnityEditor;

namespace BlackTundra.Foundation.Editor.Utility {

    public static class BuildTools {

        private const string ReleaseBuildPath = "Builds/Release/";
        private const string DevelopmentBuildPath = "Builds/Development/";
        private const string Windows = "Windows/";
        private const string Mac = "Mac/";
        private const string Linux = "Linux/";

        // windows:

        [MenuItem("Tools/Build/Standalone/Windows/Release")]
        private static void BuildStandaloneWindowsRelease() => CreateBuild(BuildTarget.StandaloneWindows64, BuildOptions.CompressWithLz4HC | BuildOptions.StrictMode, ReleaseBuildPath + Windows);

        [MenuItem("Tools/Build/Standalone/Windows/Development")]
        private static void BuildStandaloneWindowsDevelopment() => CreateBuild(BuildTarget.StandaloneWindows64, BuildOptions.Development, DevelopmentBuildPath + Windows);

        [MenuItem("Tools/Build/Standalone/Windows/Scripts Only")]
        private static void BuildStandaloneWindowsScriptOnly() => CreateBuild(BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.BuildScriptsOnly, DevelopmentBuildPath + Windows);

        // mac:

        [MenuItem("Tools/Build/Standalone/Mac/Release")]
        private static void BuildStandaloneMacRelease() => CreateBuild(BuildTarget.StandaloneOSX, BuildOptions.CompressWithLz4HC | BuildOptions.StrictMode, ReleaseBuildPath + Windows);

        [MenuItem("Tools/Build/Standalone/Mac/Development")]
        private static void BuildStandaloneMacDevelopment() => CreateBuild(BuildTarget.StandaloneOSX, BuildOptions.Development, DevelopmentBuildPath + Windows);

        [MenuItem("Tools/Build/Standalone/Mac/Scripts Only")]
        private static void BuildStandaloneMacScriptOnly() => CreateBuild(BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.BuildScriptsOnly, DevelopmentBuildPath + Windows);

        // linux:

        [MenuItem("Tools/Build/Standalone/Linux/Release")]
        private static void BuildStandaloneLinuxRelease() => CreateBuild(BuildTarget.StandaloneLinux64, BuildOptions.CompressWithLz4HC | BuildOptions.StrictMode, ReleaseBuildPath + Windows);

        [MenuItem("Tools/Build/Standalone/Linux/Development")]
        private static void BuildStandaloneLinuxDevelopment() => CreateBuild(BuildTarget.StandaloneLinux64, BuildOptions.Development, DevelopmentBuildPath + Windows);

        [MenuItem("Tools/Build/Standalone/Linux/Scripts Only")]
        private static void BuildStandaloneLinuxScriptOnly() => CreateBuild(BuildTarget.StandaloneLinux64, BuildOptions.Development | BuildOptions.BuildScriptsOnly, DevelopmentBuildPath + Windows);

        private static void CreateBuild(in BuildTarget target, in BuildOptions options, in string path) {

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int sceneCount = scenes.Length;
            string[] scenePathBuffer = new string[sceneCount];
            for (int i = sceneCount - 1; i >= 0; i--) {
                scenePathBuffer[i] = scenes[i].path;
            }
            buildPlayerOptions.scenes = scenePathBuffer;
            buildPlayerOptions.target = target;
            buildPlayerOptions.locationPathName = EditorUtility.SaveFilePanel("Output Path", path, "Application", "exe");
            buildPlayerOptions.options = options;
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }
}