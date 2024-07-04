using System;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class SpritesheetPreview : NativeRenderingWidget
{
	SceneWorld World;
	SceneObject TextureRect;
	Material PreviewMaterial;

	public SpritesheetPreview(Widget parent) : base(parent)
	{
		World = EditorUtility.CreateSceneWorld();
		new SceneCamera
		{
			World = World,
			AmbientLightColor = Color.White * 1f,
			ZNear = 0.1f,
			ZFar = 1000f,
			Position = new Vector3(0, 0, 500),
			Angles = new Angles(90, 180, 0),
			Ortho = true,
			OrthoHeight = 512f,
			AntiAliasing = true,
			BackgroundColor = Theme.ControlBackground
		};

		new SceneDirectionalLight(World, new Angles(90, 0, 0), Color.White);

		var backgroundMat = Material.Load("materials/sprite_editor_transparent.vmat");
		var background = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
		background.SetMaterialOverride(backgroundMat);
		background.Position = new Vector3(0, 0, -1);

		PreviewMaterial = Material.Load("materials/spritegraph.vmat").CreateCopy();
		PreviewMaterial.Set("Texture", Color.Transparent);
		PreviewMaterial.Set("g_flFlashAmount", 0f);

		TextureRect = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
		TextureRect.SetMaterialOverride(PreviewMaterial);
		TextureRect.Flags.WantsFrameBufferCopy = true;
		TextureRect.Flags.IsTranslucent = true;
		TextureRect.Flags.IsOpaque = false;
		TextureRect.Flags.CastShadows = false;
	}

	protected override void OnPaint()
	{
		base.OnPaint();


	}
}
