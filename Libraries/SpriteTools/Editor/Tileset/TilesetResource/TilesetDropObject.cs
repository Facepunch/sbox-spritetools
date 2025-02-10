using System.Linq;
using Editor;
using Sandbox;
using System.Threading;
using System.Threading.Tasks;

namespace SpriteTools;

[DropObject("tileset", "tileset")]
partial class TilesetDropObject : BaseDropObject
{
	TilesetResource tileset;

	protected override async Task Initialize(string dragData, CancellationToken token)
	{
		Asset asset = await InstallAsset(dragData, token);

		if (asset is null)
			return;

		if (token.IsCancellationRequested)
			return;

		tileset = asset.LoadResource<TilesetResource>();
		PackageStatus = null;
	}

	public override void OnUpdate()
	{
	}

	public override async Task OnDrop()
	{
		await WaitForLoad();

		if (tileset is null)
			return;

		var undoScope = SceneEditorSession.Active.UndoScope("Drag Tileset").WithGameObjectCreations();
		using (undoScope.Push())
		{
			var DragObject = new GameObject();
			DragObject.Name = tileset.ResourceName;
			DragObject.Transform.World = new Transform(Vector3.Zero, Rotation.Identity, 1);

			GameObject = DragObject;

			var tilesetComponent = GameObject.Components.GetOrCreate<TilesetComponent>();
			var layer = new TilesetComponent.Layer();
			layer.TilesetResource = tileset;
			tilesetComponent.Layers ??= new();
			tilesetComponent.Layers.Add(layer);

			EditorScene.Selection.Set(DragObject);
		}
	}
}
