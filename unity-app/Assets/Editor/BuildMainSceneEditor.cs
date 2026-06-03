using System;
using System.Collections.Generic;
using System.Linq;
using MouthOfTruth.Game.App;
using MouthOfTruth.Game.Presentation;
using MouthOfTruth.Game.Presentation.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MouthOfTruth.Editor
{
    public static class BuildMainSceneEditor
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";
        private const string DUNGEON_DEMO_SCENE_PATH = "Assets/ThirdParty/Environment/DungeonModularPack/Scenes/DemoScene.unity";
        private const string DUNGEON_WALL_MATERIAL_PATH = "Assets/ThirdParty/Environment/DungeonModularPack/Materials/M_Wall.mat";
        private const string GENERATED_MATERIAL_DIRECTORY_PATH = "Assets/Materials/GeneratedEnvironment";
        private const string TORCH_PREFAB_PATH = "Assets/ThirdParty/Environment/DungeonModularPack/Prefabs/Torch_B.prefab";
        private const string ARCH_PREFAB_PATH = "Assets/ThirdParty/Environment/DungeonModularPack/Prefabs/Arch_A.prefab";
        private const string RED_RUNNER_TEXTURE_PATH = "Assets/StreamingAssets/art/environment/floor_red_carpet_runner.png";
        private const string RED_RUNNER_MATERIAL_PATH = "Assets/Materials/GeneratedEnvironment/M_FloorRedRunner.mat";
        private static readonly string[] THIRD_PARTY_MODEL_DIRECTORIES =
        {
            "Assets/ThirdParty/Environment/DungeonModularPack/Meshes",
            "Assets/ThirdParty/Environment/PersianCarpetUrp/Models",
        };
        private static readonly string[] REQUIRED_THIRD_PARTY_ASSET_PATHS =
        {
            DUNGEON_DEMO_SCENE_PATH,
            DUNGEON_WALL_MATERIAL_PATH,
            TORCH_PREFAB_PATH,
            ARCH_PREFAB_PATH,
            "Assets/ThirdParty/Environment/DungeonModularPack/Meshes",
            "Assets/ThirdParty/Environment/PersianCarpetUrp/Models",
        };

        private const float CARD_ANCHOR_SPACING = 3.3f;
        private const float CARD_DEPTH_OFFSET = 7.2f;
        private const float CAMERA_DEPTH_OFFSET = 17.5f;
        private const float CAMERA_HEIGHT_OFFSET = 4.4f;
        private const float STAGE_FORWARD_MARGIN = 4.2f;

        [MenuItem("Mouth Of Truth/Build Main Scene")]
        public static void Run()
        {
            ConfigureUniversalRenderPipelineEditor.Run();
            validateRequiredThirdPartyAssets();
            normalizeThirdPartyModelImports();
            Scene sourceScene = EditorSceneManager.OpenScene(DUNGEON_DEMO_SCENE_PATH, OpenSceneMode.Single);
            Transform sourceEnvironmentRoot = findRequiredRoot(sourceScene, "Models");
            Bounds sourceEnvironmentBounds = calculateCombinedBounds(sourceEnvironmentRoot);
            Camera sourceSceneCamera = findSourceSceneCameraOrNull(sourceScene);
            SourceSceneCameraLayout sourceSceneCameraLayout = captureSourceSceneCameraLayout(sourceSceneCamera);
            Vector3 sourceSceneForward = getProjectedHorizontalForward(sourceSceneCamera, sourceEnvironmentBounds.center);
            List<GameObject> sourceSceneRootClones = cloneSourceSceneRoots(sourceScene);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            foreach (GameObject sourceSceneRootClone in sourceSceneRootClones)
            {
                SceneManager.MoveGameObjectToScene(sourceSceneRootClone, scene);
            }

            EditorSceneManager.CloseScene(sourceScene, true);

            unpackScenePrefabInstances(scene);

            Transform environmentRoot = findRequiredRoot(scene, "Models");
            Bounds environmentBounds = calculateCombinedBounds(environmentRoot);
            CorridorAxes corridorAxes = determineCorridorAxes(environmentBounds, sourceSceneForward);

            configureMainCamera(scene, environmentBounds, corridorAxes, sourceSceneCameraLayout);
            ensureEventSystem(scene);
            ensureApplicationRoot(scene);
            buildPresentationStage(scene, environmentBounds, corridorAxes);
            configureEnvironmentLighting(scene);
            unpackScenePrefabInstances(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MAIN_SCENE_PATH);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MAIN_SCENE_PATH, true),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void validateRequiredThirdPartyAssets()
        {
            foreach (string requiredAssetPath in REQUIRED_THIRD_PARTY_ASSET_PATHS)
            {
                if (AssetDatabase.IsValidFolder(requiredAssetPath) || AssetDatabase.LoadMainAssetAtPath(requiredAssetPath) != null)
                {
                    continue;
                }

                throw new BuildFailedException(
                    $"Required Unity Asset Store asset is missing: {requiredAssetPath}\n"
                    + "Restore the third-party environment assets by following THIRD_PARTY_ASSETS.md.");
            }
        }

        private static void normalizeThirdPartyModelImports()
        {
            foreach (string modelDirectoryPath in THIRD_PARTY_MODEL_DIRECTORIES)
            {
                string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { modelDirectoryPath });

                foreach (string modelGuid in modelGuids)
                {
                    string modelAssetPath = AssetDatabase.GUIDToAssetPath(modelGuid);
                    ModelImporter modelImporter = AssetImporter.GetAtPath(modelAssetPath) as ModelImporter;

                    if (modelImporter == null)
                    {
                        continue;
                    }

                    if (modelImporter.materialImportMode == ModelImporterMaterialImportMode.None)
                    {
                        continue;
                    }

                    modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                    modelImporter.SaveAndReimport();
                }
            }
        }

        private static Camera findSourceSceneCameraOrNull(Scene sourceScene)
        {
            foreach (GameObject rootGameObject in sourceScene.GetRootGameObjects())
            {
                Camera rootCamera = rootGameObject.GetComponent<Camera>();

                if (rootCamera != null)
                {
                    return rootCamera;
                }

                Camera childCamera = rootGameObject.GetComponentInChildren<Camera>(true);

                if (childCamera != null)
                {
                    return childCamera;
                }
            }

            return null;
        }

        private static List<GameObject> cloneSourceSceneRoots(Scene sourceScene)
        {
            List<GameObject> rootClones = new List<GameObject>();

            foreach (GameObject rootGameObject in sourceScene.GetRootGameObjects())
            {
                if (rootGameObject.name == "Main Camera")
                {
                    continue;
                }

                GameObject rootClone = UnityEngine.Object.Instantiate(rootGameObject);
                rootClone.name = rootGameObject.name;
                rootClones.Add(rootClone);
            }

            return rootClones;
        }

        private static Vector3 getProjectedHorizontalForward(Camera sourceSceneCamera, Vector3 environmentCenter)
        {
            if (sourceSceneCamera == null)
            {
                return Vector3.zero;
            }

            Vector3 projectedCameraForward = Vector3.ProjectOnPlane(sourceSceneCamera.transform.forward, Vector3.up);

            if (projectedCameraForward.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Vector3 normalizedForward = projectedCameraForward.normalized;
            Vector3 directionToEnvironmentCenter = Vector3.ProjectOnPlane(environmentCenter - sourceSceneCamera.transform.position, Vector3.up);

            if (directionToEnvironmentCenter.sqrMagnitude > 0.0001f && Vector3.Dot(normalizedForward, directionToEnvironmentCenter.normalized) < 0.0f)
            {
                normalizedForward = -normalizedForward;
            }

            return normalizedForward;
        }

        private static SourceSceneCameraLayout captureSourceSceneCameraLayout(Camera sourceSceneCamera)
        {
            if (sourceSceneCamera == null)
            {
                return SourceSceneCameraLayout.Invalid;
            }

            return new SourceSceneCameraLayout(sourceSceneCamera.transform.position, sourceSceneCamera.transform.rotation, sourceSceneCamera.fieldOfView, sourceSceneCamera.backgroundColor);
        }

        private static void unpackScenePrefabInstances(Scene scene)
        {
            HashSet<GameObject> outermostPrefabRoots = new HashSet<GameObject>();

            foreach (GameObject rootGameObject in scene.GetRootGameObjects())
            {
                foreach (Transform childTransform in rootGameObject.GetComponentsInChildren<Transform>(true))
                {
                    GameObject candidate = childTransform.gameObject;

                    if (PrefabUtility.IsPartOfPrefabInstance(candidate) == false)
                    {
                        continue;
                    }

                    GameObject outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(candidate);

                    if (outermostRoot != null)
                    {
                        outermostPrefabRoots.Add(outermostRoot);
                    }
                }
            }

            foreach (GameObject outermostPrefabRoot in outermostPrefabRoots)
            {
                PrefabUtility.UnpackPrefabInstance(outermostPrefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
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

        private static void configureMainCamera(Scene scene, Bounds environmentBounds, CorridorAxes corridorAxes, SourceSceneCameraLayout sourceSceneCameraLayout)
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
            mainCamera.nearClipPlane = 0.01f;
            mainCamera.farClipPlane = 200.0f;

            if (sourceSceneCameraLayout.IsValid)
            {
                mainCamera.backgroundColor = sourceSceneCameraLayout.BackgroundColor;
                mainCamera.fieldOfView = sourceSceneCameraLayout.FieldOfView;
                cameraObject.transform.position = sourceSceneCameraLayout.Position;
                cameraObject.transform.rotation = sourceSceneCameraLayout.Rotation;
                return;
            }

            mainCamera.backgroundColor = new Color(0.04f, 0.03f, 0.03f, 1.0f);
            mainCamera.fieldOfView = 42.0f;

            Vector3 stageLookTarget = calculateStageBasePosition(environmentBounds, corridorAxes)
                + Vector3.up * 2.2f;
            Vector3 cameraPosition = stageLookTarget
                - (corridorAxes.Forward * CAMERA_DEPTH_OFFSET)
                + (Vector3.up * CAMERA_HEIGHT_OFFSET);

            cameraObject.transform.position = cameraPosition;
            cameraObject.transform.LookAt(stageLookTarget);
        }

        private static void buildPresentationStage(Scene scene, Bounds environmentBounds, CorridorAxes corridorAxes)
        {
            GameObject stageRoot = getOrCreateRootObject(scene, "MouthOfTruthStage");
            destroyChildren(stageRoot.transform);

            Vector3 stageBasePosition = calculateStageBasePosition(environmentBounds, corridorAxes);
            float floorY = environmentBounds.min.y;

            Transform scenicRoot = createChild(stageRoot.transform, "ScenicStage");
            createPodium(scenicRoot, stageBasePosition, floorY);
            createStageAccents(scenicRoot, stageBasePosition, corridorAxes, floorY);
            createStageRunner(scenicRoot, environmentBounds, corridorAxes, floorY, stageBasePosition);

            Transform cardAnchorRoot = createChild(stageRoot.transform, "CardPresentationAnchors");
            Transform leftCardAnchor = createChild(cardAnchorRoot, "LeftCard");
            Transform centerCardAnchor = createChild(cardAnchorRoot, "CenterCard");
            Transform rightCardAnchor = createChild(cardAnchorRoot, "RightCard");
            Vector3 cardAnchorBasePosition = stageBasePosition - (corridorAxes.Forward * CARD_DEPTH_OFFSET) + (Vector3.up * 1.75f);
            centerCardAnchor.position = cardAnchorBasePosition;
            leftCardAnchor.position = cardAnchorBasePosition - (corridorAxes.Lateral * CARD_ANCHOR_SPACING);
            rightCardAnchor.position = cardAnchorBasePosition + (corridorAxes.Lateral * CARD_ANCHOR_SPACING);
            CardPresentationAnchorSet cardPresentationAnchorSet = ensureComponent<CardPresentationAnchorSet>(cardAnchorRoot.gameObject);
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

        private static void createStageAccents(Transform parentTransform, Vector3 stageBasePosition, CorridorAxes corridorAxes, float floorY)
        {
            GameObject archPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARCH_PREFAB_PATH);
            GameObject torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TORCH_PREFAB_PATH);

            if (archPrefab != null)
            {
                GameObject archInstance = instantiatePrefab(archPrefab, parentTransform, "StageArch");
                archInstance.transform.position = new Vector3(stageBasePosition.x, floorY + 0.1f, stageBasePosition.z + 0.25f);
                alignLongAxisToForward(archInstance.transform, corridorAxes.Forward, isLongAxisX: true);
                archInstance.transform.localScale = new Vector3(1.18f, 1.18f, 1.18f);
            }

            if (torchPrefab == null)
            {
                return;
            }

            createTorchPair(parentTransform, stageBasePosition, corridorAxes, floorY, torchPrefab);
        }

        private static void createTorchPair(Transform parentTransform, Vector3 stageBasePosition, CorridorAxes corridorAxes, float floorY, GameObject torchPrefab)
        {
            Vector3 leftTorchPosition = stageBasePosition - (corridorAxes.Lateral * 4.25f) + (Vector3.up * 2.2f);
            Vector3 rightTorchPosition = stageBasePosition + (corridorAxes.Lateral * 4.25f) + (Vector3.up * 2.2f);

            GameObject leftTorch = instantiatePrefab(torchPrefab, parentTransform, "StageTorchLeft");
            leftTorch.transform.position = leftTorchPosition;
            alignLongAxisToForward(leftTorch.transform, corridorAxes.Forward, isLongAxisX: false);

            GameObject rightTorch = instantiatePrefab(torchPrefab, parentTransform, "StageTorchRight");
            rightTorch.transform.position = rightTorchPosition;
            alignLongAxisToForward(rightTorch.transform, corridorAxes.Forward, isLongAxisX: false);
        }

        private static void createStageRunner(Transform parentTransform, Bounds environmentBounds, CorridorAxes corridorAxes, float floorY, Vector3 stageBasePosition)
        {
            Material runnerMaterial = getOrCreateRunnerMaterialOrNull();

            if (runnerMaterial == null)
            {
                return;
            }

            float corridorExtent = corridorAxes.GetExtent(environmentBounds);
            float startOffset = -corridorExtent + 2.0f;
            float endOffset = Vector3.Dot(stageBasePosition - environmentBounds.center, corridorAxes.Forward.normalized)
                + 0.75f;
            float runnerLength = Mathf.Max(8.0f, endOffset - startOffset);
            float runnerMidpointOffset = (startOffset + endOffset) * 0.5f;

            GameObject runnerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            runnerObject.name = "StageRunner";
            runnerObject.transform.SetParent(parentTransform, false);
            runnerObject.transform.position = environmentBounds.center
                + (corridorAxes.Forward * runnerMidpointOffset)
                + (Vector3.up * (floorY + 0.02f));
            runnerObject.transform.rotation = Quaternion.FromToRotation(Vector3.forward, corridorAxes.Forward.normalized);
            runnerObject.transform.localScale = new Vector3(3.25f, 0.035f, runnerLength);
            runnerObject.GetComponent<Renderer>().sharedMaterial = runnerMaterial;
        }

        private static Material getOrCreateRunnerMaterialOrNull()
        {
            ensureFolderHierarchy(GENERATED_MATERIAL_DIRECTORY_PATH);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RED_RUNNER_MATERIAL_PATH);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(RED_RUNNER_TEXTURE_PATH);
            Shader shader = findFirstAvailableShaderOrNull(
                new[]
                {
                    "Universal Render Pipeline/Lit",
                    "Universal Render Pipeline/Simple Lit",
                    "Standard",
                });

            if (texture == null || shader == null)
            {
                return null;
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, RED_RUNNER_MATERIAL_PATH);
            }

            material.shader = shader;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.58f, 0.50f, 0.46f, 0.86f));
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.1f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0.0f);
            }

            material.mainTextureScale = new Vector2(1.0f, 6.0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void configureEnvironmentLighting(Scene scene)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.17f, 0.17f, 0.20f, 1.0f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.10f, 0.10f, 0.12f, 1.0f);
            RenderSettings.fogDensity = 0.015f;

            foreach (Light light in Resources.FindObjectsOfTypeAll<Light>())
            {
                if (light == null || light.gameObject.scene != scene)
                {
                    continue;
                }

                switch (light.type)
                {
                    case LightType.Directional:
                        light.color = new Color(0.78f, 0.81f, 0.88f, 1.0f);
                        light.intensity = 0.16f;
                        light.shadows = LightShadows.Soft;
                        light.shadowStrength = 0.48f;
                        break;

                    case LightType.Point:
                    case LightType.Spot:
                        light.color = new Color(0.92f, 0.68f, 0.40f, 1.0f);
                        light.intensity = Mathf.Max(3.0f, light.intensity);
                        light.range = Mathf.Max(6.0f, light.range);
                        light.shadows = LightShadows.Soft;
                        light.shadowStrength = 0.50f;
                        break;

                    default:
                        break;
                }

                EditorUtility.SetDirty(light);
            }
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

        private static void sanitizeRendererMaterials(Transform rootTransform)
        {
            Dictionary<Material, Material> sanitizedMaterialsBySource = new Dictionary<Material, Material>();
            Renderer[] renderers = rootTransform.GetComponentsInChildren<Renderer>(true);
            Material defaultSourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(DUNGEON_WALL_MATERIAL_PATH);
            Material fallbackSafeMaterial = defaultSourceMaterial != null ? getOrCreateSafeMaterial(defaultSourceMaterial) : null;

            foreach (Renderer renderer in renderers)
            {
                Material[] sourceSharedMaterials = renderer.sharedMaterials;

                if (sourceSharedMaterials == null || sourceSharedMaterials.Length == 0)
                {
                    if (fallbackSafeMaterial != null)
                    {
                        renderer.sharedMaterials = new[] { fallbackSafeMaterial };
                        EditorUtility.SetDirty(renderer);
                    }

                    continue;
                }

                bool wasUpdated = false;
                Material[] sanitizedSharedMaterials = new Material[sourceSharedMaterials.Length];

                for (int materialIndex = 0; materialIndex < sourceSharedMaterials.Length; materialIndex += 1)
                {
                    Material sourceMaterial = sourceSharedMaterials[materialIndex];

                    if (sourceMaterial == null)
                    {
                        sanitizedSharedMaterials[materialIndex] = fallbackSafeMaterial;
                        wasUpdated = fallbackSafeMaterial != null;
                        continue;
                    }

                    if (shouldSanitizeMaterial(sourceMaterial) == false)
                    {
                        sanitizedSharedMaterials[materialIndex] = sourceMaterial;
                        continue;
                    }

                    Material sanitizedMaterial;
                    if (sanitizedMaterialsBySource.TryGetValue(sourceMaterial, out sanitizedMaterial) == false)
                    {
                        sanitizedMaterial = getOrCreateSafeMaterial(sourceMaterial);
                        sanitizedMaterialsBySource[sourceMaterial] = sanitizedMaterial;
                    }

                    sanitizedSharedMaterials[materialIndex] = sanitizedMaterial;
                    wasUpdated = true;
                }

                if (wasUpdated)
                {
                    renderer.sharedMaterials = sanitizedSharedMaterials;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static Shader findFirstAvailableShaderOrNull(IReadOnlyList<string> shaderNames)
        {
            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }

        private static bool shouldSanitizeMaterial(Material material)
        {
            Shader shader = material.shader;
            string shaderName = shader != null ? shader.name : string.Empty;

            return shaderName.Contains("DungeonKitShader", StringComparison.OrdinalIgnoreCase)
                || shaderName.StartsWith("LB Shader/", StringComparison.OrdinalIgnoreCase)
                || shaderName.Equals("Universal Render Pipeline/Lit", StringComparison.Ordinal)
                || shaderName.Equals("Universal Render Pipeline/Simple Lit", StringComparison.Ordinal);
        }

        private static Material getOrCreateSafeMaterial(Material sourceMaterial)
        {
            ensureFolderHierarchy(GENERATED_MATERIAL_DIRECTORY_PATH);

            string sanitizedMaterialAssetPath = $"{GENERATED_MATERIAL_DIRECTORY_PATH}/{sourceMaterial.name}_SceneSafe.mat";
            string existingMaterialAssetPath = $"{GENERATED_MATERIAL_DIRECTORY_PATH}/{sourceMaterial.name}_SceneSafe.mat";
            Shader safeShader = findFirstAvailableShaderOrNull(
                new[]
                {
                    "Universal Render Pipeline/Unlit",
                    "Universal Render Pipeline/Lit",
                    "Unlit/Texture",
                    "Unlit/Color",
                });

            if (safeShader == null)
            {
                throw new InvalidOperationException("빌드용 안전 셰이더를 찾을 수 없습니다.");
            }

            Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(existingMaterialAssetPath);

            if (existingMaterial != null)
            {
                if (existingMaterial.shader != safeShader)
                {
                    existingMaterial.shader = safeShader;
                }

                synchronizeSafeMaterial(existingMaterial, sourceMaterial);
                return existingMaterial;
            }

            Material safeMaterial = new Material(safeShader);
            synchronizeSafeMaterial(safeMaterial, sourceMaterial);
            AssetDatabase.CreateAsset(safeMaterial, sanitizedMaterialAssetPath);
            return safeMaterial;
        }

        private static void synchronizeSafeMaterial(Material safeMaterial, Material sourceMaterial)
        {
            if (safeMaterial == null || sourceMaterial == null)
            {
                return;
            }

            Texture baseTexture = getFirstAvailableTextureOrNull(sourceMaterial, "_BaseMap", "_MainTex", "_BaseColorMap");

            if (baseTexture != null && safeMaterial.HasProperty("_BaseMap"))
            {
                safeMaterial.SetTexture("_BaseMap", baseTexture);
            }

            if (baseTexture != null && safeMaterial.HasProperty("_MainTex"))
            {
                safeMaterial.SetTexture("_MainTex", baseTexture);
            }

            Color baseColor = getFirstAvailableColor(sourceMaterial, Color.white, "_BaseColor", "_Color");

            if (safeMaterial.HasProperty("_BaseColor"))
            {
                safeMaterial.SetColor("_BaseColor", baseColor);
            }

            if (safeMaterial.HasProperty("_Color"))
            {
                safeMaterial.SetColor("_Color", baseColor);
            }

            EditorUtility.SetDirty(safeMaterial);
        }

        private static Texture getFirstAvailableTextureOrNull(Material material, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    Texture texture = material.GetTexture(propertyName);

                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static Color getFirstAvailableColor(Material material, Color fallbackColor, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    return material.GetColor(propertyName);
                }
            }

            return fallbackColor;
        }

        private static void ensureFolderHierarchy(string folderPath)
        {
            string[] folderSegments = folderPath.Split('/');
            string currentPath = folderSegments[0];

            for (int folderIndex = 1; folderIndex < folderSegments.Length; folderIndex += 1)
            {
                string nextPath = $"{currentPath}/{folderSegments[folderIndex]}";

                if (AssetDatabase.IsValidFolder(nextPath) == false)
                {
                    AssetDatabase.CreateFolder(currentPath, folderSegments[folderIndex]);
                }

                currentPath = nextPath;
            }
        }

        private static CorridorAxes determineCorridorAxes(Bounds environmentBounds, Vector3 sourceSceneForward)
        {
            if (sourceSceneForward.sqrMagnitude > 0.0001f)
            {
                Vector3 sourceSceneLateral = Vector3.Cross(Vector3.up, sourceSceneForward).normalized;
                return new CorridorAxes(sourceSceneForward, sourceSceneLateral);
            }

            bool isForwardAlongZ = environmentBounds.size.z >= environmentBounds.size.x;
            Vector3 forward = isForwardAlongZ ? Vector3.forward : Vector3.right;
            Vector3 lateral = isForwardAlongZ ? Vector3.right : Vector3.forward;
            return new CorridorAxes(forward, lateral);
        }

        private static Vector3 calculateStageBasePosition(Bounds environmentBounds, CorridorAxes corridorAxes)
        {
            float stageForwardOffset = corridorAxes.GetExtent(environmentBounds) - STAGE_FORWARD_MARGIN;
            Vector3 stagePosition = environmentBounds.center + (corridorAxes.Forward * stageForwardOffset);
            stagePosition.y = environmentBounds.min.y;
            return stagePosition;
        }

        private static void alignLongAxisToForward(Transform targetTransform, Vector3 forwardAxis, bool isLongAxisX)
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(forwardAxis, Vector3.up).normalized;

            if (projectedForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 sourceLongAxis = isLongAxisX ? Vector3.right : Vector3.forward;
            targetTransform.rotation = Quaternion.FromToRotation(sourceLongAxis, projectedForward);
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

        private static GameObject instantiatePrefab(GameObject prefab, Transform parentTransform, string objectName)
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
            public CorridorAxes(Vector3 forward, Vector3 lateral)
            {
                Forward = forward;
                Lateral = lateral;
            }

            public Vector3 Forward { get; }

            public Vector3 Lateral { get; }

            public float GetExtent(Bounds bounds)
            {
                Vector3 normalizedForward = Forward.normalized;

                return (Mathf.Abs(normalizedForward.x) * bounds.extents.x) + (Mathf.Abs(normalizedForward.y) * bounds.extents.y) + (Mathf.Abs(normalizedForward.z) * bounds.extents.z);
            }
        }

        private readonly struct SourceSceneCameraLayout
        {
            public static SourceSceneCameraLayout Invalid =>
                new SourceSceneCameraLayout(Vector3.zero, Quaternion.identity, 0.0f, Color.black);

            public SourceSceneCameraLayout(Vector3 position, Quaternion rotation, float fieldOfView, Color backgroundColor)
            {
                Position = position;
                Rotation = rotation;
                FieldOfView = fieldOfView;
                BackgroundColor = backgroundColor;
            }

            public Vector3 Position { get; }

            public Quaternion Rotation { get; }

            public float FieldOfView { get; }

            public Color BackgroundColor { get; }

            public bool IsValid => FieldOfView > 0.0f;
        }
    }
}
