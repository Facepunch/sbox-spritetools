using System;
using Sandbox;
using Editor;
using Editor.Assets;
using System.Threading.Tasks;
using System.Linq;

namespace SpriteTools;

[AssetPreview("sprite")]
class PreviewSprite : AssetPreview
{
    SceneObject so;
    Material previewMat;
    int sequences;
    SpriteResource sprite;

    /// <summary>
    /// Use the eval, because in sequences we want to find a frame with the most action
    /// </summary>
    public override bool UsePixelEvaluatorForThumbs => true;

    /// <summary>
    /// Only render a video if we have animations
    /// </summary>
    public override bool IsAnimatedPreview => (sprite?.Animations?.FirstOrDefault()?.Frames?.Count ?? 0) > 1;

    public PreviewSprite(Asset asset) : base(asset)
    {
        sprite = SpriteResource.Load(Asset.Path);
    }

    public override Task InitializeAsset()
    {
        Camera.Position = Vector3.Up * 100;
        Camera.Angles = new Angles(90, 180, 0);
        Camera.Ortho = true;
        Camera.OrthoHeight = 100f;
        Camera.BackgroundColor = Color.Transparent;

        so = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
        so.Transform = Transform.Zero;
        var previewMat = Material.Load("materials/sprite_2d.vmat").CreateCopy();
        previewMat.Set("Texture", Color.Transparent);
        previewMat.Set("g_flFlashAmount", 0f);
        so.Flags.WantsFrameBufferCopy = true;
        so.Flags.IsTranslucent = true;
        so.Flags.IsOpaque = false;
        so.Flags.CastShadows = false;

        var atlas = TextureAtlas.FromAnimation(sprite.Animations.FirstOrDefault());

        if (atlas is not null)
        {
            previewMat.Set("Texture", atlas);
            var offset = atlas.GetFrameOffset(0);
            var tiling = atlas.GetFrameTiling();
            previewMat.Set("g_vOffset", offset);
            previewMat.Set("g_vTiling", tiling);

            PrimarySceneObject = so;

            sequences = sprite.Animations.FirstOrDefault().Frames.Count;
            if (sequences < 1)
                sequences = 1;
        }

        so.SetMaterialOverride(previewMat);

        return Task.CompletedTask;
    }

    public override void UpdateScene(float cycle, float timeStep)
    {
    }

}
