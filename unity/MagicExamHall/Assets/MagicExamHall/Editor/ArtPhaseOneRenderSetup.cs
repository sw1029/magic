using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MagicExamHall.Editor
{
    public static class ArtPhaseOneRenderSetup
    {
        private const string SettingsFolder = "Assets/MagicExamHall/Settings";
        private const string RendererPath = SettingsFolder + "/MagicExamHall_2DRenderer.asset";
        private const string PipelinePath = SettingsFolder + "/MagicExamHall_URP2D.asset";
        private const string SpriteMaterialPath = "Assets/MagicExamHall/Resources/MagicExamHallMaterials/PixelSpriteDefault.mat";
        private const string DefaultPostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
        private const string UrpSpriteLitShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";
        private const string UrpSpriteUnlitShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        [MenuItem("Magic Exam Hall/Apply Art Phase 1 Render Setup")]
        public static void Apply()
        {
            EnsureSettingsFolder();
            var rendererData = Ensure2DRendererData();
            var pipelineAsset = EnsurePipelineAsset(rendererData);
            BindPipeline(pipelineAsset);
            EnsureSpriteLitMaterial();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Art Phase 1 render setup applied: {PipelinePath}");
        }

        private static Renderer2DData Ensure2DRendererData()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            ResourceReloader.ReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);
            RebuildBlendStyles(rendererData);

            var serialized = new SerializedObject(rendererData);
            SetObject(serialized, "m_PostProcessData", AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath));
            SetFloat(serialized, "m_HDREmulationScale", 1f);
            SetFloat(serialized, "m_LightRenderTextureScale", 0.5f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(rendererData);
            return rendererData;
        }

        private static UniversalRenderPipelineAsset EnsurePipelineAsset(Renderer2DData rendererData)
        {
            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
            }

            pipelineAsset.supportsHDR = false;
            pipelineAsset.msaaSampleCount = 1;
            pipelineAsset.renderScale = 1f;
            ResourceReloader.ReloadAllNullIn(pipelineAsset, UniversalRenderPipelineAsset.packagePath);

            var serialized = new SerializedObject(pipelineAsset);
            var rendererList = serialized.FindProperty("m_RendererDataList");
            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            SetInt(serialized, "m_DefaultRendererIndex", 0);
            SetInt(serialized, "m_RendererType", (int)RendererType._2DRenderer);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(pipelineAsset);
            return pipelineAsset;
        }

        private static void BindPipeline(RenderPipelineAsset pipelineAsset)
        {
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            var currentQuality = QualitySettings.GetQualityLevel();
            var qualityNames = QualitySettings.names;
            for (var index = 0; index < qualityNames.Length; index++)
            {
                QualitySettings.SetQualityLevel(index, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            QualitySettings.SetQualityLevel(currentQuality, false);
        }

        private static void EnsureSpriteLitMaterial()
        {
            var shader = FindFirstShader(UrpSpriteLitShader, UrpSpriteUnlitShader, "Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("Could not find a sprite shader for PixelSpriteDefault.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SpriteMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_Color"))
            {
                material.color = Color.white;
            }

            EditorUtility.SetDirty(material);
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/MagicExamHall"))
            {
                AssetDatabase.CreateFolder("Assets", "MagicExamHall");
            }

            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets/MagicExamHall", "Settings");
            }
        }

        private static void RebuildBlendStyles(Renderer2DData rendererData)
        {
            var rebuild = typeof(Renderer2DData).GetMethod("RebuildBlendStyles", BindingFlags.Instance | BindingFlags.NonPublic);
            rebuild?.Invoke(rendererData, new object[] { true });
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static Shader FindFirstShader(params string[] shaderNames)
        {
            foreach (var shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }
    }
}
