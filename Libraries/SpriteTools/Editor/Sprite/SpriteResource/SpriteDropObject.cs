using System.Linq;
using Editor;
using Sandbox;
using System.Threading;
using System.Threading.Tasks;
using Editor.ShaderGraph.Nodes;

namespace SpriteTools;

[DropObject("sprite", "sprite")]
partial class SpriteDropObject : BaseDropObject
{
	SpriteResource sprite;
	Texture texture;
	Vector2 origin;

	protected override async Task Initialize(string dragData, CancellationToken token)
	{
		Asset asset = await InstallAsset(dragData, token);

		if (asset is null)
			return;

		if (token.IsCancellationRequested)
			return;

		sprite = asset.LoadResource<SpriteResource>();
		if (sprite is null) return;
		var anim = sprite.Animations.FirstOrDefault();
		if (anim is null) return;

		origin = anim.Origin - 0.5f;
		texture = sprite.GetPreviewTexture();
		PackageStatus = null;
	}

	public override void OnUpdate()
	{
		using var scope = Gizmo.Scope("DropObject", traceTransform.WithRotation(Rotation.Identity));

		Gizmo.Draw.Color = Color.White;
		if (texture is not null)
		{
			// origin vector in respect to the camera
			var camRot = SceneViewportWidget.LastSelected.State.CameraRotation * Rotation.From(90, 0, 0);
			var originVec = camRot.Backward * origin.y + camRot.Right * origin.x;
			Gizmo.Draw.Sprite(originVec * 100f, new Vector2(100f, 100f), texture, true);
		}
		else
		{
			Gizmo.Draw.Color = Color.White.WithAlpha(0.3f);
			Gizmo.Draw.Sprite(Bounds.Center, 16, "materials/gizmo/downloads.png");
		}
	}

	public override async Task OnDrop()
	{
		await WaitForLoad();

		if (sprite is null)
			return;

		var DragObject = new GameObject();
		DragObject.Name = sprite.ResourceName;
		DragObject.Transform.World = traceTransform;
		DragObject.Transform.Rotation = SceneViewportWidget.LastSelected.State.CameraRotation * new Angles(0, -90, 90);

		GameObject = DragObject;

		var spriteComponent = GameObject.Components.GetOrCreate<SpriteComponent>();
		spriteComponent.Sprite = sprite;

		EditorScene.Selection.Clear();
		EditorScene.Selection.Add(DragObject);
	}
}
