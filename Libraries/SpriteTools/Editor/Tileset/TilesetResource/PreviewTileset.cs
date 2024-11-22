using System;
using Sandbox;
using Editor;
using Editor.Assets;
using System.Threading.Tasks;
using System.Linq;

namespace SpriteTools;

[AssetPreview("tileset")]
class PreviewTileset : AssetPreview
{
    SceneObject so;
    Material previewMat;
    TilesetResource tileset;

    /// <summary>
    /// Use the eval, because in sequences we want to find a frame with the most action
    /// </summary>
    public override bool UsePixelEvaluatorForThumbs => true;

    /// <summary>
    /// Only render a video if we have animations
    /// </summary>
    public override bool IsAnimatedPreview => false;

    public PreviewTileset(Asset asset) : base(asset)
    {
        tileset = ResourceLibrary.Get<TilesetResource>(Asset.Path);
    }

    public override Task InitializeAsset()
    {
        var image = Texture.Load(Editor.FileSystem.Content, tileset?.FilePath);
        if (image is null) return Task.CompletedTask;

        Camera.Position = Vector3.Up * 100;
        Camera.Angles = new Angles(90, 180, 0);
        Camera.Ortho = true;
        Camera.OrthoHeight = 100f;
        Camera.BackgroundColor = Color.Transparent;

        so = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
        so.Transform = Transform.Zero;
        previewMat = Material.Load("materials/sprite_2d.vmat").CreateCopy();
        previewMat.Set("Texture", image);
        previewMat.Set("g_flFlashAmount", 0f);
        so.Flags.WantsFrameBufferCopy = true;
        so.Flags.IsTranslucent = true;
        so.Flags.IsOpaque = false;
        so.Flags.CastShadows = false;

        var aspect = image.Width / (float)image.Height;
        if (aspect < 1)
        {
            so.Transform = so.Transform.WithScale(new Vector3(1, aspect, 1));
        }
        else
        {
            so.Transform = so.Transform.WithScale(new Vector3(1f / aspect, 1, 1));
        }

        so.SetMaterialOverride(previewMat);

        return Task.CompletedTask;
    }

    public override void UpdateScene(float cycle, float timeStep)
    {

    }
}
