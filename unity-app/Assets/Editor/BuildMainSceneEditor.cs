using System;
using System.Collections.Generic;
using System.Linq;
using MouthOfTruth.Game.App;
using MouthOfTruth.Game.Presentation;
using MouthOfTruth.Game.Presentation.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace MouthOfTruth.Editor
{
    public static class BuildMainSceneEditor
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";
        private const string DUNGEON_DEMO_SCENE_PATH =
            "Assets/ThirdParty/Environment/DungeonModularPack/Scenes/DemoScene.unity";
        private const string DUNGEON_WALL_MATERIAL_PATH =
            "Assets/ThirdParty/Environment/DungeonModularPack/Materials/M_Wall.mat";
        private const string TORCH_PREFAB_PATH =
            "Assets/ThirdParty/Environment/DungeonModularPack/Prefabs/Torch_B.prefab";
        private const string ARCH_PREFAB_PATH =
            "Assets/ThirdParty/Environment/DungeonModularPack/Prefabs/Arch_A.prefab";
        private const string CARPET_PREFAB_DIRECTORY_PATH =
            "Assets/ThirdParty/Environment/PersianCarpetUrp/Prefab";

        private const float CARD_ANCHOR_SPACING = 3.3f;
        private const float CARD_DEPTH_OFFSET = 7.2f;
        private const float CAMERA_DEPTH_OFFSET = 17.5f;
        private const float CAMERA_HEIGHT_OFFSET = 4.4f;
        private const float STAGE_FORWARD_MARGIN = 4.2f;

        private readonly struct CarpetPrefabCandidate
        {
            public CarpetPrefabCandidate(GameObject prefab, bool isLongAxisX, float areaScore)
            {
                Prefab = prefab;
                IsLongAxisX = isLongAxisX;
                AreaScore = areaScore;
            }

            public GameObject Prefab { get; }

            public bool IsLongAxisX { get; }

            public float AreaScore { get; }
        }

        [MenuItem("Mouth Of Truth/Build Main Scene")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.OpenScene(DUNGEON_DEMO_SCENE_PATH, OpenSceneMode.Single);
            Transform environmentRoot = findRequiredRoot(scene, "Models");
            Bounds environmentBounds = calculateCombinedBounds(environmentRoot);
            CorridorAxes corridorAxes = determineCorridorAxes(environmentBounds);

            configureMainCamera(scene, environmentBounds, corridorAxes);
            ensureEventSystem(scene);
            ensureApplicationRoot(scene);
            buildPresentationStage(scene, environmentBounds, corridorAxes);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MAIN_SCENE_PATH);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MAIN_SCENE_PATH, true),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ensureApplicationRoot(Scene scene)
        {
            GameObject appObject = getOrCreateRootObject(scene, "MouthOfTruthApp");
            ensureComponent<MouthOfTruthGameView>(appObject);
            ensureComponent<MouthOfTruthAppController>(appObject);
        }

        private static void ensureEventSystem(Scene scene)
        {
            EventSystem existingEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();

            if (existingEventSystem != null)
            {
                ensureComponent<StandaloneInputModule>(existingEventSystem.gameObject);
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void configureMainCamera(
            Scene scene,
            Bounds environmentBounds,
            CorridorAxes corridorAxes)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            }

            GameObject cameraObject = mainCamera != null
                ? mainCamera.gameObject
                : new GameObject("Main Camera");

            if (mainCamera == null)
            {
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
            }

            cameraObject.name = "Main Camera";
            cameraObject.tag = "MainCamera";
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.04f, 0.03f, 0.03f, 1.0f);
            mainCamera.fieldOfView = 42.0f;
            mainCamera.nearClipPlane = 0.01f;
            mainCamera.farClipPlane = 200.0f;

            Vector3 stageLookTarget = calculateStageBasePosition(environmentBounds, corridorAxes)
                + Vector3.up * 2.2f;
            Vector3 cameraPosition = stageLookTarget
                - (corridorAxes.Forward * CAMERA_DEPTH_OFFSET)
                + (Vector3.up * CAMERA_HEIGHT_OFFSET);

            cameraObject.transform.position = cameraPosition;
            cameraObject.transform.LookAt(stageLookTarget);
        }

        private static void buildPresentationStage(
            Scene scene,
            Bounds environmentBounds,
            CorridorAxes corridorAxes)
        {
            GameObject stageRoot = getOrCreateRootObject(scene, "MouthOfTruthStage");
            destroyChildren(stageRoot.transform);

            Vector3 stageBasePosition = calculateStageBasePosition(environmentBounds, corridorAxes);
            float floorY = environmentBounds.min.y;

            Transform scenicRoot = createChild(stageRoot.transform, "ScenicStage");
            createPodium(scenicRoot, stageBasePosition, floorY);
            createStageAccents(scenicRoot, stageBasePosition, corridorAxes, floorY);
            createRunnerCarpets(scenicRoot, environmentBounds, corridorAxes, floorY);

            Transform cardAnchorRoot = createChild(stageRoot.transform, "CardPresentationAnchors");
            Transform leftCardAnchor = createChild(cardAnchorRoot, "LeftCard");
            Transform centerCardAnchor = createChild(cardAnchorRoot, "CenterCard");
            Transform rightCardAnchor = createChild(cardAnchorRoot, "RightCard");
            Vector3 cardAnchorBasePosition =
                stageBasePosition
                - (corridorAxes.Forward * CARD_DEPTH_OFFSET)
                + (Vector3.up * 1.75f);
            centerCardAnchor.position = cardAnchorBasePosition;
            leftCardAnchor.position = cardAnchorBasePosition - (corridorAxes.Lateral * CARD_ANCHOR_SPACING);
            rightCardAnchor.position = cardAnchorBasePosition + (corridorAxes.Lateral * CARD_ANCHOR_SPACING);
            CardPresentationAnchorSet cardPresentationAnchorSet =
                ensureComponent<CardPresentationAnchorSet>(cardAnchorRoot.gameObject);
            cardPresentationAnchorSet.Configure(leftCardAnchor, centerCardAnchor, rightCardAnchor);

            Transform mouthAnchorRoot = createChild(stageRoot.transform, "MouthAnchors");
            Transform truthMouthAnchor = createChild(mouthAnchorRoot, "TruthMouth");
            Transform mouthFrontAnchor = createChild(mouthAnchorRoot, "MouthFrontAnchor");
            Transform mouthInnerAnchor = createChild(mouthAnchorRoot, "MouthInnerAnchor");
            truthMouthAnchor.position = stageBasePosition + new Vector3(0.0f, 1.9f, 0.0f);
            mouthFrontAnchor.position = truthMouthAnchor.position
                - (corridorAxes.Forward * 0.42f)
                - (Vector3.up * 1.55f);
            mouthInnerAnchor.position = truthMouthAnchor.position - (Vector3.up * 0.45f);
            MouthAnchorSet mouthAnchorSet = ensureComponent<MouthAnchorSet>(mouthAnchorRoot.gameObject);
            mouthAnchorSet.Configure(truthMouthAnchor, mouthFrontAnchor, mouthInnerAnchor);
        }

        private static void createPodium(Transform parentTransform, Vector3 stageBasePosition, float floorY)
        {
            GameObject podiumObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            podiumObject.name = "TruthMouthPodium";
            podiumObject.transform.SetParent(parentTransform, false);
            podiumObject.transform.position = new Vector3(stageBasePosition.x, floorY + 0.55f, stageBasePosition.z);
            podiumObject.transform.localScale = new Vector3(2.4f, 0.55f, 2.4f);

            Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(DUNGEON_WALL_MATERIAL_PATH);

            if (wallMaterial != null)
            {
                Renderer podiumRenderer = podiumObject.GetComponent<Renderer>();
                podiumRenderer.sharedMaterial = wallMaterial;
            }
        }

        private static void createStageAccents(
            Transform parentTransform,
            Vector3 stageBasePosition,
            CorridorAxes corridorAxes,
            float floorY)
        {
            GameObject archPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARCH_PREFAB_PATH);
            GameObject torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TORCH_PREFAB_PATH);

            if (archPrefab != null)
            {
                GameObject archInstance = instantiatePrefab(archPrefab, parentTransform, "StageArch");
                archInstance.transform.position = new Vector3(
                    stageBasePosition.x,
                    floorY + 0.1f,
                    stageBasePosition.z + 0.25f);
                alignLongAxisToForward(archInstance.transform, corridorAxes.Forward, isLongAxisX: true);
                archInstance.transform.localScale = new Vector3(1.18f, 1.18f, 1.18f);
            }

            if (torchPrefab == null)
            {
                return;
            }

            createTorchPair(parentTransform, stageBasePosition, corridorAxes, floorY, torchPrefab);
        }

        private static void createTorchPair(
            Transform parentTransform,
            Vector3 stageBasePosition,
            CorridorAxes corridorAxes,
            float floorY,
            GameObject torchPrefab)
        {
            Vector3 leftTorchPosition =
                stageBasePosition
                - (corridorAxes.Lateral * 4.25f)
                + (Vector3.up * 2.2f);
            Vector3 rightTorchPosition =
                stageBasePosition
                + (corridorAxes.Lateral * 4.25f)
                + (Vector3.up * 2.2f);

            GameObject leftTorch = instantiatePrefab(torchPrefab, parentTransform, "StageTorchLeft");
            leftTorch.transform.position = leftTorchPosition;
            alignLongAxisToForward(leftTorch.transform, corridorAxes.Forward, isLongAxisX: false);

            GameObject rightTorch = instantiatePrefab(torchPrefab, parentTransform, "StageTorchRight");
            rightTorch.transform.position = rightTorchPosition;
            alignLongAxisToForward(rightTorch.transform, corridorAxes.Forward, isLongAxisX: false);
        }

        private static void createRunnerCarpets(
            Transform parentTransform,
            Bounds environmentBounds,
            CorridorAxes corridorAxes,
            float floorY)
        {
            List<CarpetPrefabCandidate> candidates = loadRunnerCarpetCandidates();

            if (candidates.Count == 0)
            {
                return;
            }

            float corridorLength = corridorAxes.GetExtent(environmentBounds) * 2.0f;
            int runnerCount = Mathf.Clamp(Mathf.RoundToInt(corridorLength / 5.5f), 4, 6);
            float startOffset = 3.0f;
            float endOffset = STAGE_FORWARD_MARGIN + 2.8f;
            float usableLength = Mathf.Max(4.0f, corridorLength - startOffset - endOffset);

            for (int index = 0; index < runnerCount; index++)
            {
                CarpetPrefabCandidate candidate = candidates[index % candidates.Count];
                float normalizedProgress = runnerCount == 1
                    ? 0.5f
                    : index / (runnerCount - 1.0f);
                float offset = -corridorAxes.GetExtent(environmentBounds)
                    + startOffset
                    + (usableLength * normalizedProgress);
                Vector3 worldPosition = environmentBounds.center
                    + (corridorAxes.Forward * offset)
                    + (Vector3.up * (floorY + 0.03f));
                worldPosition.y = floorY + 0.03f;

                GameObject carpetInstance = instantiatePrefab(
                    candidate.Prefab,
                    parentTransform,
                    $"RunnerCarpet_{index + 1:00}");
                carpetInstance.transform.position = worldPosition;
                alignLongAxisToForward(
                    carpetInstance.transform,
                    corridorAxes.Forward,
                    candidate.IsLongAxisX);
            }
        }

        private static List<CarpetPrefabCandidate> loadRunnerCarpetCandidates()
        {
            List<CarpetPrefabCandidate> candidates = new List<CarpetPrefabCandidate>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { CARPET_PREFAB_DIRECTORY_PATH });

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab == null)
                {
                    continue;
                }

                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

                try
                {
                    Renderer[] renderers = prefabContents.GetComponentsInChildren<Renderer>(true);

                    if (renderers.Length == 0)
                    {
                        continue;
                    }

                    Bounds bounds = renderers[0].bounds;

                    foreach (Renderer renderer in renderers.Skip(1))
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }

                    float longAxis = Mathf.Max(bounds.size.x, bounds.size.z);
                    float shortAxis = Mathf.Max(0.001f, Mathf.Min(bounds.size.x, bounds.size.z));
                    float aspectRatio = longAxis / shortAxis;

                    if (aspectRatio < 1.9f)
                    {
                        continue;
                    }

                    float areaScore = bounds.size.x * bounds.size.z;
                    bool isLongAxisX = bounds.size.x >= bounds.size.z;
                    candidates.Add(new CarpetPrefabCandidate(prefab, isLongAxisX, areaScore * aspectRatio));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }

            return candidates
                .OrderByDescending(candidate => candidate.AreaScore)
                .Take(4)
                .ToList();
        }

        private static Bounds calculateCombinedBounds(Transform rootTransform)
        {
            Renderer[] renderers = rootTransform.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("환경 루트에서 Renderer를 찾을 수 없습니다.");
            }

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static CorridorAxes determineCorridorAxes(Bounds environmentBounds)
        {
            bool isForwardAlongZ = environmentBounds.size.z >= environmentBounds.size.x;
            Vector3 forward = isForwardAlongZ ? Vector3.forward : Vector3.right;
            Vector3 lateral = isForwardAlongZ ? Vector3.right : Vector3.forward;
            return new CorridorAxes(forward, lateral, isForwardAlongZ);
        }

        private static Vector3 calculateStageBasePosition(Bounds environmentBounds, CorridorAxes corridorAxes)
        {
            float stageForwardOffset = corridorAxes.GetExtent(environmentBounds) - STAGE_FORWARD_MARGIN;
            Vector3 stagePosition = environmentBounds.center + (corridorAxes.Forward * stageForwardOffset);
            stagePosition.y = environmentBounds.min.y;
            return stagePosition;
        }

        private static void alignLongAxisToForward(
            Transform targetTransform,
            Vector3 forwardAxis,
            bool isLongAxisX)
        {
            float yRotationDegrees;

            if (forwardAxis == Vector3.forward)
            {
                yRotationDegrees = isLongAxisX ? 90.0f : 0.0f;
            }
            else
            {
                yRotationDegrees = isLongAxisX ? 0.0f : 90.0f;
            }

            targetTransform.rotation = Quaternion.Euler(0.0f, yRotationDegrees, 0.0f);
        }

        private static Transform findRequiredRoot(Scene scene, string rootObjectName)
        {
            GameObject rootObject = scene.GetRootGameObjects()
                .FirstOrDefault(candidate => candidate.name == rootObjectName);

            if (rootObject == null)
            {
                throw new InvalidOperationException(
                    $"씬 루트 '{rootObjectName}' 을(를) 찾을 수 없습니다.");
            }

            return rootObject.transform;
        }

        private static GameObject getOrCreateRootObject(Scene scene, string objectName)
        {
            GameObject existingObject = scene.GetRootGameObjects()
                .FirstOrDefault(candidate => candidate.name == objectName);

            if (existingObject != null)
            {
                return existingObject;
            }

            GameObject createdObject = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(createdObject, scene);
            return createdObject;
        }

        private static Transform createChild(Transform parentTransform, string objectName)
        {
            Transform existingChild = parentTransform.Find(objectName);

            if (existingChild != null)
            {
                return existingChild;
            }

            GameObject childObject = new GameObject(objectName);
            childObject.transform.SetParent(parentTransform, false);
            return childObject.transform;
        }

        private static GameObject instantiatePrefab(
            GameObject prefab,
            Transform parentTransform,
            string objectName)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parentTransform) as GameObject;

            if (instance == null)
            {
                throw new InvalidOperationException($"프리팹 생성에 실패했습니다: {prefab.name}");
            }

            instance.name = objectName;
            return instance;
        }

        private static T ensureComponent<T>(GameObject gameObject)
            where T : Component
        {
            T existingComponent = gameObject.GetComponent<T>();
            return existingComponent != null ? existingComponent : gameObject.AddComponent<T>();
        }

        private static void destroyChildren(Transform parentTransform)
        {
            for (int childIndex = parentTransform.childCount - 1; childIndex >= 0; childIndex--)
            {
                UnityEngine.Object.DestroyImmediate(parentTransform.GetChild(childIndex).gameObject);
            }
        }

        private readonly struct CorridorAxes
        {
            public CorridorAxes(Vector3 forward, Vector3 lateral, bool isForwardAlongZ)
            {
                Forward = forward;
                Lateral = lateral;
                IsForwardAlongZ = isForwardAlongZ;
            }

            public Vector3 Forward { get; }

            public Vector3 Lateral { get; }

            public bool IsForwardAlongZ { get; }

            public float GetExtent(Bounds bounds)
            {
                return IsForwardAlongZ ? bounds.extents.z : bounds.extents.x;
            }
        }
    }
}
