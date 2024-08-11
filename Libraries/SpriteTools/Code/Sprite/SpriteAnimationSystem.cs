using System.Linq;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace SpriteTools;

public class SpriteAnimationSystem : GameObjectSystem
{

    public SpriteAnimationSystem(Scene scene) : base(scene)
    {
        Listen(Stage.UpdateBones, 15, UpdateSpriteAnimation, "UpdateSpriteAnimation");

        var sprites = ResourceLibrary.GetAll<SpriteResource>().ToArray();
        foreach (var sprite in sprites)
        {
            foreach (var anim in sprite.Animations)
            {
                TextureAtlas.FromAnimation(anim);
            }
        }
    }

    void UpdateSpriteAnimation()
    {
        SpriteComponent[] sprites = Scene.GetAllComponents<SpriteComponent>().ToArray();

        Parallel.ForEach(sprites, sprite =>
        {
            sprite.UpdateSceneObject();
        });

        foreach (var sprite in sprites)
        {
            sprite.RunBroadcastQueue();
        }
    }
}
