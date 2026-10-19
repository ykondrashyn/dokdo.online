using System.Collections.Generic;

namespace Dokdo
{
    public static class SceneIds
    {
        public const string Load = "load";
        public const string Intro = "Intro";
        public const string Main = "Main";

        public const string LoadPath = "Assets/0.my/Scenes/load.unity";
        public const string IntroPath = "Assets/0.my/Scenes/Intro.unity";
        public const string MainPath = "Assets/0.my/Scenes/Main.unity";

        private static readonly string[] Names = { Load, Intro, Main };
        private static readonly string[] Paths = { LoadPath, IntroPath, MainPath };

        public static IReadOnlyList<string> BuildOrder => Names;
        public static IReadOnlyList<string> BuildPaths => Paths;
    }
}
