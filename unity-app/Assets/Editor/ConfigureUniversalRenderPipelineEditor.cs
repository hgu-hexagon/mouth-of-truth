using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MouthOfTruth.Editor
{
    public static class ConfigureUniversalRenderPipelineEditor
    {
        private const string SETTINGS_DIRECTORY_PATH = "Assets/Settings/Rendering";
        private const string PIPELINE_ASSET_PATH = SETTINGS_DIRECTORY_PATH + "/MouthOfTruthUniversalRenderPipeline.asset";
        private const string RENDERER_ASSET_PATH = SETTINGS_DIRECTORY_PATH + "/MouthOfTruthUniversalRenderer.asset";

        [MenuItem("Mouth Of Truth/Configure Universal Render Pipeline")]
        public static void Run()
        {
            ensureSettingsDirectoryExists();

            UniversalRendererData rendererData = loadOrCreateRendererData();
            UniversalRenderPipelineAsset pipelineAsset = loadOrCreatePipelineAsset(rendererData);

            ensureRendererDataResources(rendererData);
            ensurePipelineAssetConfiguration(pipelineAsset, rendererData);
            ensureGlobalSettingsAsset();
            assignPipelineAssetToProject(pipelineAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ensureSettingsDirectoryExists()
        {
            Directory.CreateDirectory(SETTINGS_DIRECTORY_PATH);
        }

        private static UniversalRendererData loadOrCreateRendererData()
        {
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RENDERER_ASSET_PATH);

            if (rendererData != null)
            {
                return rendererData;
            }

            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            ensureDefaultPostProcessData(rendererData);
            ResourceReloader.ReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);
            AssetDatabase.CreateAsset(rendererData, RENDERER_ASSET_PATH);
            EditorUtility.SetDirty(rendererData);
            return rendererData;
        }

        private static UniversalRenderPipelineAsset loadOrCreatePipelineAsset(UniversalRendererData rendererData)
        {
            UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PIPELINE_ASSET_PATH);

            if (pipelineAsset != null)
            {
                return pipelineAsset;
            }

            pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, PIPELINE_ASSET_PATH);
            EditorUtility.SetDirty(pipelineAsset);
            return pipelineAsset;
        }

        private static void ensureRendererDataResources(UniversalRendererData rendererData)
        {
            ensureDefaultPostProcessData(rendererData);
            ResourceReloader.ReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);
            EditorUtility.SetDirty(rendererData);
        }

        private static void ensureDefaultPostProcessData(UniversalRendererData rendererData)
        {
            if (rendererData.postProcessData != null)
            {
                return;
            }

            MethodInfo getDefaultPostProcessDataMethod = typeof(PostProcessData).GetMethod("GetDefaultPostProcessData", BindingFlags.Static | BindingFlags.NonPublic);

            if (getDefaultPostProcessDataMethod == null)
            {
                throw new InvalidOperationException("Unable to locate URP default post-process data provider.");
            }

            rendererData.postProcessData = getDefaultPostProcessDataMethod.Invoke(null, null) as PostProcessData;
        }

        private static void ensurePipelineAssetConfiguration(UniversalRenderPipelineAsset pipelineAsset, UniversalRendererData rendererData)
        {
            ResourceReloader.ReloadAllNullIn(pipelineAsset, UniversalRenderPipelineAsset.packagePath);

            SerializedObject serializedPipelineAsset = new SerializedObject(pipelineAsset);
            SerializedProperty rendererDataListProperty = serializedPipelineAsset.FindProperty("m_RendererDataList");

            if (rendererDataListProperty == null)
            {
                throw new InvalidOperationException("Unable to configure URP renderer data list on the project pipeline asset.");
            }

            if (rendererDataListProperty.arraySize == 0)
            {
                rendererDataListProperty.arraySize = 1;
            }

            rendererDataListProperty.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;

            SerializedProperty defaultRendererIndexProperty = serializedPipelineAsset.FindProperty("m_DefaultRendererIndex");

            if (defaultRendererIndexProperty != null)
            {
                defaultRendererIndexProperty.intValue = 0;
            }

            serializedPipelineAsset.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipelineAsset);
        }

        private static void ensureGlobalSettingsAsset()
        {
            Type globalSettingsType = typeof(UniversalRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings");
            MethodInfo ensureMethod = globalSettingsType?.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic);

            if (ensureMethod == null)
            {
                throw new InvalidOperationException("Unable to locate URP global settings Ensure method.");
            }

            ensureMethod.Invoke(null, new object[] { true });
        }

        private static void assignPipelineAssetToProject(UniversalRenderPipelineAsset pipelineAsset)
        {
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            int currentQualityLevel = QualitySettings.GetQualityLevel();

            for (int qualityIndex = 0; qualityIndex < QualitySettings.names.Length; qualityIndex += 1)
            {
                QualitySettings.SetQualityLevel(qualityIndex, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            QualitySettings.SetQualityLevel(currentQualityLevel, false);
            QualitySettings.renderPipeline = pipelineAsset;
        }
    }
}
