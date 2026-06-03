using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MouthOfTruth.Editor
{
    public sealed class ReleaseShaderVariantStripper : IPreprocessShaders
    {
        int IOrderedCallback.callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> shaderCompilerData)
        {
            if (BuildPipeline.isBuildingPlayer == false)
            {
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
            {
                return;
            }

            if (shader == null)
            {
                return;
            }

            if (shouldStripShaderCompletely(shader.name))
            {
                shaderCompilerData.Clear();
                return;
            }

            if (shader.name != "Universal Render Pipeline/Unlit")
            {
                return;
            }

            while (shaderCompilerData.Count > 1)
            {
                shaderCompilerData.RemoveAt(shaderCompilerData.Count - 1);
            }
        }

        private static bool shouldStripShaderCompletely(string shaderName)
        {
            return shaderName == "Universal Render Pipeline/Lit"
                || shaderName == "Universal Render Pipeline/Simple Lit";
        }
    }
}
