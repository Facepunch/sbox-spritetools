using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SpriteTools;

[Icon("emoji_emotions")]
public sealed class SpriteComponent : Component, Component.ExecuteInEditor
{
    /// <summary>
    /// The sprite resource that this component uses.
    /// </summary>
    [Property]
    SpriteResource Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            if (_sprite != null)
                CurrentAnimation = _sprite.Animations.FirstOrDefault();
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
    /// Used to override the material with your own. Useful for custom shaders.
    /// Shader requires a texture parameter named "Texture".
    /// </summary>
    [Property]
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

    /// <summary>
    /// The playback speed of the animation.
    /// </summary>
    [Property] public float PlaybackSpeed { get; set; } = 1.0f;


    /// <summary>
    /// Whether or not the sprite should render itself/its shadows.
    /// </summary>
    [Property]
    [Category("Lighting")]
    public ShadowRenderType CastShadows { get; set; } = ShadowRenderType.On;

    /// <summary>
    /// A dictionary of broadcast events that this component will send (populated based on the Sprite resource)
    /// </summary>
    public Dictionary<string, Action> BroadcastEvents = new();

    /// <summary>
    /// The sprite animation that is currently playing.
    /// </summary>
    public SpriteAnimation CurrentAnimation { get; private set; }

    /// <summary>
    /// The current frame index of the animation playing.
    /// </summary>
    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        set
        {
            _currentFrameIndex = value;
            if (CurrentAnimation is not null && _currentFrameIndex >= CurrentAnimation.Frames.Count)
                _currentFrameIndex = 0;
        }
    }
    private int _currentFrameIndex = 0;
    private float _timeSinceLastFrame = 0;

    internal SceneObject SceneObject { get; set; }

    protected override void OnStart()
    {
        base.OnEnabled();

        if (Sprite is null) return;
        if (Sprite.Animations.Count > 0)
            PlayAnimation(Sprite.Animations[0].Name);

        UpdateSceneObject();
    }

    protected override void OnEnabled()
    {
        base.OnEnabled();

        if (SceneObject.IsValid())
            SceneObject.RenderingEnabled = true;
    }

    protected override void OnDisabled()
    {
        base.OnDisabled();

        if (SceneObject.IsValid())
            SceneObject.RenderingEnabled = false;
    }

    protected override void OnUpdate()
    {
        UpdateSceneObject();
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
        if (!SceneObject.IsValid())
        {
            SceneObject = new SceneObject(Scene.SceneWorld, Model.Load("models/sprite_quad.vmdl"))
            {
                Flags = { IsTranslucent = true }
            };
        }

        if (SpriteMaterial is null)
        {
            if (MaterialOverride != null)
                SpriteMaterial = MaterialOverride;
            else
                SpriteMaterial = Material.Create("spritemat", "shaders/pixelated_masked.shader");
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

        if (PlaybackSpeed > 0 && _timeSinceLastFrame >= frameRate)
        {
            var frame = CurrentFrameIndex;
            frame++;
            if (frame >= CurrentAnimation.Frames.Count)
                frame = 0;
            CurrentFrameIndex = frame;
            _timeSinceLastFrame = 0;
        }
        else if (PlaybackSpeed < 0 && _timeSinceLastFrame >= frameRate)
        {
            var frame = CurrentFrameIndex;
            frame--;
            if (frame < 0)
                frame = CurrentAnimation.Frames.Count - 1;
            CurrentFrameIndex = frame;
            _timeSinceLastFrame = 0;
        }

        var texture = Texture.Load(FileSystem.Mounted, CurrentAnimation.Frames[CurrentFrameIndex].FilePath);
        SpriteMaterial.Set("Texture", texture);

        // Add pivot to transform
        var pos = Transform.Position;
        var origin = CurrentAnimation.Origin - new Vector2(0.5f, 0.5f);
        pos -= new Vector3(origin.y, origin.x, 0) * 100f * Transform.Scale;
        pos = pos.RotateAround(Transform.Position, Transform.Rotation);
        SceneObject.Transform = new Transform(pos, Transform.Rotation, Transform.Scale);
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
        if (Sprite == null || CurrentAnimation == null)
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
                        BroadcastEvents[tag] = () => { };
                }
            }
        }
    }

    public void PlayAnimation(string animationName)
    {
        if (Sprite == null) return;

        var animation = Sprite.Animations.Find(a => a.Name == animationName);
        if (animation == null) return;

        CurrentAnimation = animation;
        CurrentFrameIndex = 0;
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
}