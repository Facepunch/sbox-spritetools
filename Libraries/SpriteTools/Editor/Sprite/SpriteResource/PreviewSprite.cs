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
    SpriteResource sprite;
    TextureAtlas atlas;
    int frame = 0;
    int frames = 1;
    float frameTime = 1f;
    float timer = 0f;

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
        previewMat = Material.Load("materials/sprite_2d.vmat").CreateCopy();
        previewMat.Set("Texture", Color.Transparent);
        previewMat.Set("g_flFlashAmount", 0f);
        so.Flags.WantsFrameBufferCopy = true;
        so.Flags.IsTranslucent = true;
        so.Flags.IsOpaque = false;
        so.Flags.CastShadows = false;

        var anim = sprite.Animations.FirstOrDefault();
        Init(anim);

        so.SetMaterialOverride(previewMat);

        return Task.CompletedTask;
    }

    public override void UpdateScene(float cycle, float timeStep)
    {
        timer += timeStep;
        if (timer >= frameTime)
        {
            frame = (frame + 1) % frames;
            UpdateFrame();

            timer -= frameTime;
        }
    }

    void Init(SpriteAnimation anim)
    {
        atlas = TextureAtlas.FromAnimation(anim);
        if (atlas is not null)
        {
            previewMat.Set("Texture", atlas);
            frame = 0;
            UpdateFrame();

            var aspectRatio = atlas.AspectRatio;
            if (aspectRatio > 1f)
                so.Transform = so.Transform.WithScale(new Vector3(1f / aspectRatio, 1f, 1f));
            else
                so.Transform = so.Transform.WithScale(new Vector3(1f, aspectRatio, 1f));

            frameTime = 1f / anim.FrameRate;
            frames = anim.Frames.Count;
            if (frames < 1)
                frames = 1;
        }
    }

    void UpdateFrame()
    {
        if (atlas is null) return;
        var offset = atlas.GetFrameOffset(frame);
        var tiling = atlas.GetFrameTiling();
        previewMat.Set("g_vOffset", offset);
        previewMat.Set("g_vTiling", tiling);
        so.SetMaterialOverride(previewMat);
    }

    public void SetAnimation(string name)
    {
        var anim = sprite.Animations.FirstOrDefault(x => x.Name == name);
        if (anim is null) return;
        Init(anim);
    }

}
