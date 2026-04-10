using System.Linq;
using MouthOfTruth.Game.App;
using MouthOfTruth.Game.Presentation.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MouthOfTruth.Editor
{
    public static class BuildMainSceneEditor
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";

        public static void Run()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1.0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.tag = "MainCamera";

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();

            GameObject appObject = new GameObject("MouthOfTruthApp");
            appObject.AddComponent<MouthOfTruthGameView>();
            appObject.AddComponent<MouthOfTruthAppController>();

            EditorSceneManager.SaveScene(scene, MAIN_SCENE_PATH);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MAIN_SCENE_PATH, true),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
