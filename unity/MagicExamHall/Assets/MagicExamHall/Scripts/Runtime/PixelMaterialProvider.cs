using UnityEngine;
using UnityEngine.UI;

namespace MagicExamHall
{
    public static class PixelMaterialProvider
    {
        private const string SpriteMaterialPath = "MagicExamHallMaterials/PixelSpriteDefault";
        private const string UiMaterialPath = "MagicExamHallMaterials/PixelUIDefault";
        private const string UrpSpriteLitShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";
        private const string UrpSpriteUnlitShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const string BuiltInSpriteShader = "Sprites/Default";
        private static Material spriteMaterial;
        private static Material uiMaterial;
        private static Material additiveMaterial;

        public static Material SpriteMaterial
        {
            get
            {
                if (spriteMaterial == null)
                {
                    var resourceMaterial = Resources.Load<Material>(SpriteMaterialPath);
                    spriteMaterial = IsUrpSpriteMaterial(resourceMaterial)
                        ? resourceMaterial
                        : CreateFallback(UrpSpriteLitShader, UrpSpriteUnlitShader, BuiltInSpriteShader) ?? resourceMaterial;
                }

                return spriteMaterial;
            }
        }

        public static Material UiMaterial
        {
            get
            {
                if (uiMaterial == null)
                {
                    uiMaterial = Resources.Load<Material>(UiMaterialPath) ?? Graphic.defaultGraphicMaterial ?? CreateFallback("UI/Default");
                }

                return uiMaterial;
            }
        }

        /// <summary>
        /// Material for glow halos. Under URP the legacy additive particle
        /// shaders are unsupported (render magenta), so the halo uses the URP
        /// sprite shader alpha-blended; without an SRP the legacy additive
        /// shader gives true additive light stacking.
        /// </summary>
        public static Material AdditiveMaterial
        {
            get
            {
                if (additiveMaterial == null)
                {
                    additiveMaterial = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                        ? CreateFallback(UrpSpriteUnlitShader, UrpSpriteLitShader, BuiltInSpriteShader) ?? SpriteMaterial
                        : CreateFallback("Legacy Shaders/Particles/Additive", "Particles/Additive", "Mobile/Particles/Additive", BuiltInSpriteShader) ?? SpriteMaterial;
                }

                return additiveMaterial;
            }
        }

        private static bool IsUrpSpriteMaterial(Material material)
        {
            return material != null
                && material.shader != null
                && (material.shader.name == UrpSpriteLitShader || material.shader.name == UrpSpriteUnlitShader);
        }

        private static Material CreateFallback(params string[] shaderNames)
        {
            foreach (var shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    return new Material(shader);
                }
            }

            return null;
        }
    }
}
