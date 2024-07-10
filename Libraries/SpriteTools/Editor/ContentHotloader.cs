using Sandbox;
using Editor;
using System.Threading.Tasks;

namespace SpriteTools;

public static class ContentHotloader
{
    [Event("content.changed")]
    public static async void OnContentChanged(string path)
    {
        if (path.EndsWith(".sprite"))
        {
            await Task.Delay(100);

            foreach (var session in SceneEditorSession.All)
            {
                var sprites = session.Scene.GetAllComponents<SpriteComponent>();
                foreach (var sprite in sprites)
                {
                    sprite.Sprite = sprite.Sprite;
                }
            }
        }
        else if (path.Contains("."))
        {
            TextureAtlas.ClearCache(path);

            await Task.Delay(100);

            foreach (var session in SceneEditorSession.All)
            {
                var sprites = session.Scene.GetAllComponents<SpriteComponent>();
                foreach (var sprite in sprites)
                {
                    sprite.PlayAnimation(sprite.CurrentAnimation.Name, true);
                }
            }
        }
    }
}