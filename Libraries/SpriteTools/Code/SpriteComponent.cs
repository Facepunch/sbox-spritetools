using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.Json.Serialization;
using Sandbox;

namespace SpriteTools;

[Icon("emoji_emotions")]
public sealed class SpriteComponent : Component, Component.ExecuteInEditor
{
    /// <summary>
    /// The sprite resource that this component uses.
    /// </summary>
    [Property]
    public SpriteResource Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            if (_sprite != null)
            {
                PlayAnimation(StartingAnimationName);
            }
            else
                CurrentAnimation = null;
            UpdateSprite();
        }
    }
    SpriteResource _sprite;

    /// <summary>
    /// The color tint of the Sprite.
    /// </summary>
    [Property]
    [Category("Visuals")]
    public Color Tint
    {
        get => SceneObject?.ColorTint ?? Color.White;
        set
        {
            if (SceneObject != null)
                SceneObject.ColorTint = value;
        }
    }

    /// <summary>
    /// The color of the sprite when it is flashing.
    /// </summary>
    [Property]
    [Category("Visuals")]
    public Color FlashTint
    {
        get => _flashTint;
        set
        {
            _flashTint = value;
            SpriteMaterial?.Set("g_vFlashColor", value);
            SpriteMaterial?.Set("g_flFlashAmount", value.a);
        }
    }
    Color _flashTint = Color.White.WithAlpha(0);

    /// <summary>
    /// Used to override the material with your own. Useful for custom shaders.
    /// Shader requires a texture parameter named "Texture".
    /// </summary>
    [Property]
    [Category("Visuals")]
    public Material MaterialOverride
    {
        get => _materialOverride;
        set
        {
            _materialOverride = value;
            SpriteMaterial = null;
        }
    }
    Material _materialOverride;
    private Material SpriteMaterial { get; set; }
    public Material Material => SpriteMaterial;

    /// <summary>
    /// The playback speed of the animation.
    /// </summary>
    [Property] public float PlaybackSpeed { get; set; } = 1.0f;


    /// <summary>
    /// Whether or not the sprite should render itself/its shadows.
    /// </summary>
    [Property]
    [Category("Visuals")]
    public ShadowRenderType CastShadows { get; set; } = ShadowRenderType.On;

    [Property]
    [Category("Visuals")]
    public SpriteFlags SpriteFlags
    {
        get => _spriteFlags;
        set
        {
            if (_spriteFlags == value) return;
            _spriteFlags = value;
            ApplySpriteFlags();
        }
    }
    private SpriteFlags _spriteFlags = SpriteFlags.None;

    /// <summary>
    /// A dictionary of broadcast events that this component will send (populated based on the Sprite resource)
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, Action<SpriteComponent>> BroadcastEvents = new();

    /// <summary>
    /// The sprite animation that is currently playing.
    /// </summary>
    [JsonIgnore]
    public SpriteAnimation CurrentAnimation
    {
        get => _currentAnimation;
        set
        {
            PlayAnimation(value.Name);
        }
    }
    [JsonIgnore]
    SpriteAnimation _currentAnimation;

    [Property, Category("Sprite"), Title("Current Animation"), AnimationName]
    private string StartingAnimationName
    {
        get => CurrentAnimation?.Name ?? "";
        set
        {
            if (Sprite == null) return;
            var animation = Sprite.Animations.Find(a => a.Name.ToLowerInvariant() == value.ToLowerInvariant());
            if (animation == null) return;
            PlayAnimation(animation.Name);
            _startingAnimationName = value.ToLowerInvariant();
        }
    }
    string _startingAnimationName = "";


    [Property, Category("Sprite"), JsonIgnore]
    BroadcastControls _broadcastEvents = new();

    [Property, Category("Sprite")]
    public bool CreateAttachPoints
    {
        get => _createAttachPoints;
        set
        {
            if (_createAttachPoints != value)
            {
                _createAttachPoints = value;

                AttachPoints.Clear();
                if (value)
                {
                    BuildAttachPoints();
                }
            }
        }
    }
    bool _createAttachPoints = false;
    Dictionary<string, GameObject> AttachPoints = new();

    /// <summary>
    /// Invoked when a broadcast event is triggered.
    /// </summary>
    [Property, Group("Sprite")]
    public Action<string> OnBroadcastEvent { get; set; }

    [Property, Group("Sprite")]
    public Action<string> OnAnimationComplete { get; set; }

    /// <summary>
    /// The current frame index of the animation playing.
    /// </summary>
    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        set
        {
            _currentFrameIndex = value;
            if (CurrentAnimation is not null)
            {
                if (_currentFrameIndex >= CurrentAnimation.Frames.Count)
                    _currentFrameIndex = 0;
                ApplyMaterialOffset();
            }
        }
    }
    private int _currentFrameIndex = 0;
    private float _timeSinceLastFrame = 0;
    private bool _flipHorizontal = false;
    private bool _flipVertical = false;

    internal SceneObject SceneObject { get; set; }
    TextureAtlas CurrentTexture { get; set; }

    protected override void OnStart()
    {
        base.OnStart();

        if (Sprite is null) return;
        if (Sprite.Animations.Count > 0)
        {
            var anim = Sprite.Animations.FirstOrDefault(x => x.Name.ToLowerInvariant() == StartingAnimationName);
            if (anim is null)
                anim = Sprite.Animations.FirstOrDefault();
            PlayAnimation(anim.Name);
        }

        UpdateSceneObject();
        ApplySpriteFlags();
        FlashTint = _flashTint;
    }

    protected override void OnAwake()
    {
        base.OnAwake();

        SceneObject ??= new SceneObject(Scene.SceneWorld, Model.Load("models/sprite_quad_1_sided.vmdl"))
        {
            Flags = { IsTranslucent = true }
        };
    }

    protected override void OnEnabled()
    {
        base.OnEnabled();

        if (SceneObject.IsValid())
            SceneObject.RenderingEnabled = true;

        if (CreateAttachPoints)
            BuildAttachPoints();
    }

    protected override void OnDisabled()
    {
        base.OnDisabled();

        if (SceneObject.IsValid())
            SceneObject.RenderingEnabled = false;
    }

    protected override void DrawGizmos()
    {
        base.DrawGizmos();

        if (Game.IsPlaying) return;

        // Move bbox by origin
        var bbox = SceneObject.LocalBounds;
        bbox = bbox.Rotate(SceneObject.Transform.Rotation);
        var pos = (Transform.Position - SceneObject.Transform.Position) * Transform.Rotation;
        bbox = bbox.Translate(pos);
        Gizmo.Hitbox.BBox(bbox);

        if (Gizmo.IsHovered)
        {
            using (Gizmo.Scope("hover"))
            {
                Gizmo.Draw.Color = Color.Orange;
                Gizmo.Draw.LineBBox(bbox);
            }
        }
    }

    internal void UpdateSceneObject()
    {
        if (SpriteMaterial is null)
        {
            if (MaterialOverride != null)
                SpriteMaterial = MaterialOverride.CreateCopy();
            else
                SpriteMaterial = Material.Create("spritemat", "shaders/sprite_2d.shader");
            if (CurrentTexture is not null)
            {
                SpriteMaterial.Set("Texture", CurrentTexture);
                ApplyMaterialOffset();
            }
            SceneObject.SetMaterialOverride(SpriteMaterial);
        }

        SceneObject.RenderingEnabled = true;
        SceneObject.Flags.ExcludeGameLayer = CastShadows == ShadowRenderType.ShadowsOnly;
        SceneObject.Flags.CastShadows = CastShadows == ShadowRenderType.On || CastShadows == ShadowRenderType.ShadowsOnly;

        if (CurrentAnimation == null)
        {
            SceneObject.Transform = new Transform(Transform.Position, Transform.Rotation, Transform.Scale);
            SceneObject.RenderingEnabled = false;
            return;
        }

        var frameRate = (1f / ((PlaybackSpeed == 0) ? 0 : (CurrentAnimation.FrameRate * Math.Abs(PlaybackSpeed))));
        _timeSinceLastFrame += ((Game.IsPlaying) ? Time.Delta : RealTime.Delta);

        var lastFrame = CurrentAnimation.Frames.Count;
        if (PlaybackSpeed > 0 && _timeSinceLastFrame >= frameRate)
        {
            if (CurrentAnimation.Looping || CurrentFrameIndex < lastFrame - 1)
            {
                var frame = CurrentFrameIndex;
                frame++;
                if (CurrentAnimation.Looping && frame >= lastFrame)
                    frame = 0;
                else if (frame >= lastFrame - 1)
                    OnAnimationComplete?.Invoke(CurrentAnimation.Name);
                CurrentFrameIndex = frame;
                var currentFrame = CurrentAnimation.Frames[CurrentFrameIndex];
                foreach (var tag in currentFrame.Events)
                {
                    BroadcastEvent(tag);
                }
                _timeSinceLastFrame = 0;
            }
        }
        else if (PlaybackSpeed < 0 && _timeSinceLastFrame >= frameRate)
        {
            if (CurrentAnimation.Looping || CurrentFrameIndex > 0)
            {
                var frame = CurrentFrameIndex;
                frame--;
                if (CurrentAnimation.Looping && frame < 0)
                    frame = lastFrame - 1;
                else if (frame <= 0)
                    OnAnimationComplete?.Invoke(CurrentAnimation.Name);
                CurrentFrameIndex = frame;
                _timeSinceLastFrame = 0;
            }
        }

        // var texture = Texture.Load(FileSystem.Mounted, CurrentAnimation.Frames[CurrentFrameIndex].FilePath);
        // if (texture is not null)
        //     SpriteMaterial.Set("Texture", texture);

        // Add pivot to transform
        var pos = Transform.Position;
        var scale = Transform.Scale;
        var origin = CurrentAnimation.Origin - new Vector2(0.5f, 0.5f);
        pos -= new Vector3(origin.y, origin.x, 0) * 100f * scale;
        pos = pos.RotateAround(Transform.Position, Transform.Rotation);
        SceneObject.Transform = new Transform(pos, Transform.Rotation, scale);
    }

    internal void UpdateAttachments()
    {
        if (AttachPoints is not null && AttachPoints.Count > 0)
        {
            foreach (var attachment in AttachPoints)
            {
                var attachPos = CurrentAnimation.GetAttachmentPosition(attachment.Key, CurrentFrameIndex);
                var origin = CurrentAnimation.Origin - new Vector2(0.5f, 0.5f);
                var pos = (new Vector3(attachPos.y, attachPos.x, 0) - (Vector3.One.WithZ(0) / 2f) - new Vector3(origin.y, origin.x, 0)) * 100f;
                pos *= Transform.LocalScale;
                // pos = pos.RotateAround(Transform.Position, Transform.Rotation);
                attachment.Value.Transform.LocalPosition = pos;
            }
        }
    }

    void ApplyMaterialOffset()
    {
        if (CurrentTexture is null) return;
        if (SpriteMaterial is null) return;
        var offset = CurrentTexture.GetFrameOffset(CurrentFrameIndex);
        var tiling = CurrentTexture.GetFrameTiling();
        if (_flipHorizontal)
        {
            offset.x = -offset.x - tiling.x;
        }
        if (_flipVertical)
        {
            offset.y = -offset.y - tiling.y;
        }
        SpriteMaterial.Set("g_vTiling", tiling);
        SpriteMaterial.Set("g_vOffset", offset);
    }

    void ApplySpriteFlags()
    {
        _flipHorizontal = _spriteFlags.HasFlag(SpriteFlags.HorizontalFlip);
        _flipVertical = _spriteFlags.HasFlag(SpriteFlags.VerticalFlip);
        var targetModel = _spriteFlags.HasFlag(SpriteFlags.DrawBackface) ? "models/sprite_quad_2_sided.vmdl" : "models/sprite_quad_1_sided.vmdl";
        if (SceneObject is not null && SceneObject.Model.ResourcePath != targetModel)
            SceneObject.Model = Model.Load(targetModel);
        ApplyMaterialOffset();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneObject?.Delete();
        SceneObject = null;
    }

    internal void UpdateSprite()
    {
        BroadcastEvents.Clear();
        if (Sprite == null)
        {
            CurrentAnimation = null;
            return;
        }

        foreach (var animation in Sprite.Animations)
        {
            foreach (var frame in animation.Frames)
            {
                foreach (var tag in frame.Events)
                {
                    if (!BroadcastEvents.ContainsKey(tag))
                        BroadcastEvents[tag] = (_) => { };
                }
            }
        }
    }

    public void PlayAnimation(string animationName)
    {
        if (Sprite == null) return;

        var animation = Sprite.Animations.FirstOrDefault(a => a.Name.ToLowerInvariant() == animationName.ToLowerInvariant());
        if (animation == null) return;

        _currentAnimation = animation;
        CurrentFrameIndex = 0;

        var atlas = TextureAtlas.FromAnimation(animation);
        CurrentTexture = atlas;
        SpriteMaterial?.Set("Texture", CurrentTexture);
        ApplyMaterialOffset();
    }

    void BroadcastEvent(string tag)
    {
        OnBroadcastEvent?.Invoke(tag);
        if (BroadcastEvents.ContainsKey(tag))
            BroadcastEvents[tag]?.Invoke(this);
    }

    internal void BuildAttachPoints()
    {
        if (Sprite is null) return;
        var attachments = Sprite.GetAttachmentNames();
        foreach (var attachment in attachments)
        {
            var go = GameObject.Children.FirstOrDefault(x => x.Name == attachment);
            if (go is null)
            {
                go = Scene.CreateObject();
                go.Parent = GameObject;
            }

            AttachPoints[attachment] = go;
            go.Flags |= GameObjectFlags.Bone;
            go.Name = attachment;
        }
    }

    public enum ShadowRenderType
    {
        [Icon("wb_shade")]
        [Description("Render the sprite with shadows (default)")]
        On,
        [Icon("wb_twilight")]
        [Description("Render the sprite without shadows")]
        Off,
        [Icon("hide_source")]
        [Title("Shadows Only")]
        [Description("Render ONLY the sprites shadows")]
        ShadowsOnly
    }

    public class BroadcastControls { }
    public class AnimationNameAttribute : Attribute
    {
        public string Parameter { get; set; } = "Sprite";
    }
}
