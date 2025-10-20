using Editor;
using Sandbox;
using System;

namespace SpriteTools;

public class SpriteRenderingWidget : SceneRenderingWidget
{
	public Action OnDragSelected;

	public ModelRenderer TextureRect;
	ModelRenderer BackgroundRect;
	public Material PreviewMaterial;
	public Vector2 TextureSize { get; private set; }
	public float AspectRatio { get; private set; }

	protected float targetZoom = 115f;
	Vector2 cameraGrabPos = Vector2.Zero;
	bool cameraGrabbing = false;

	protected virtual bool CanZoom => true;

	public SpriteRenderingWidget ( Widget parent ) : base( parent )
	{
		MouseTracking = true;
		FocusMode = FocusMode.Click;

		Scene = Scene.CreateEditorScene();
		using ( Scene.Push() )
		{

			var cameraObj = new GameObject( "Camera" );
			Camera = cameraObj.Components.Create<CameraComponent>();
			Camera.ZFar = 4000f;
			Camera.ZNear = 1f;
			Camera.EnablePostProcessing = false;
			Camera.Orthographic = true; ;
			Camera.OrthographicHeight = 512f;
			Camera.WorldRotation = new Angles( 90, 180, 0 );
			Camera.BackgroundColor = Theme.ControlBackground;
			var ambientLight = cameraObj.Components.Create<AmbientLight>();
			ambientLight.Color = Color.White * 1f;

			var backgroundMat = Material.Load( "materials/sprite_editor_transparent.vmat" );
			var backgroundObj = new GameObject( "Background" );
			BackgroundRect = backgroundObj.AddComponent<ModelRenderer>();
			BackgroundRect.Model = Model.Load( "models/preview_quad.vmdl" );
			BackgroundRect.MaterialOverride = backgroundMat;
			BackgroundRect.WorldPosition = new Vector3( 0, 0, -1 );

			PreviewMaterial = Material.Load( "materials/sprite_2d.vmat" ).CreateCopy();
			PreviewMaterial.Set( "Texture", Color.Transparent );
			PreviewMaterial.Set( "g_flFlashAmount", 0f );

			var textureObj = new GameObject( "Texture" );
			TextureRect = textureObj.AddComponent<ModelRenderer>();
			TextureRect.Model = Model.Load( "models/preview_quad.vmdl" );
			TextureRect.MaterialOverride = PreviewMaterial;
			TextureRect.WorldPosition = Vector3.Zero;
		}
	}

	protected override void OnWheel ( WheelEvent e )
	{
		base.OnWheel( e );

		if ( CanZoom )
			Zoom( e.Delta );
	}

	protected override void OnMousePress ( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.MiddleMouseButton )
		{
			cameraGrabbing = true;
			cameraGrabPos = e.LocalPosition;
		}
	}

	protected override void OnMouseMove ( MouseEvent e )
	{
		base.OnMouseMove( e );

		if ( cameraGrabbing )
		{
			var delta = ( cameraGrabPos - e.LocalPosition ) * ( Camera.OrthographicHeight / 512f );
			Camera.WorldPosition = new Vector3( Camera.WorldPosition.x + delta.y, Camera.WorldPosition.y + delta.x, Camera.WorldPosition.z );
			cameraGrabPos = e.LocalPosition;
		}
	}

	protected override void OnMouseReleased ( MouseEvent e )
	{
		base.OnMouseReleased( e );

		if ( e.MiddleMouseButton )
		{
			cameraGrabbing = false;
		}
	}

	protected override void PreFrame ()
	{
		Camera.OrthographicHeight = Camera.OrthographicHeight.LerpTo( targetZoom, 0.1f );
	}

	public void Zoom ( float delta )
	{
		targetZoom *= 1f - ( delta / 500f );
		targetZoom = targetZoom.Clamp( 1, 1000 );
	}

	public void Fit ()
	{
		targetZoom = 115f;
		Camera.WorldPosition = new Vector3( 0, 0, targetZoom );
	}

	public void SetTexture ( Texture texture, Rect rect = default )
	{
		if ( rect == default )
		{
			rect = new Rect( 0, 0, texture.Width, texture.Height );
		}
		PreviewMaterial.Set( "Texture", texture );
		TextureSize = new Vector2( texture.Width, texture.Height );
		TextureRect.MaterialOverride = PreviewMaterial;

		var tiling = rect.Size / TextureSize;
		var offset = rect.Position / TextureSize;
		PreviewMaterial.Set( "g_vTiling", tiling );
		PreviewMaterial.Set( "g_vOffset", offset );

		TextureSize = rect.Size;
		ResizeQuads();
	}

	void ResizeQuads ()
	{
		if ( !BackgroundRect.IsValid() || !TextureRect.IsValid() )
			return;

		// Scale the quad to be the same aspect ratio as the texture
		AspectRatio = TextureSize.x / TextureSize.y;
		var size = new Vector3( 1f / AspectRatio, 1f, 1f );
		if ( AspectRatio < 1f )
			size = new Vector3( 1f, AspectRatio, 1f );

		BackgroundRect.WorldTransform = Transform.Zero.WithScale( size ).WithPosition( new Vector3( 0, 0, -1 ) );
		TextureRect.WorldTransform = Transform.Zero.WithScale( size );
	}

	protected void UpdateInputs ()
	{
		Camera.CustomSize = Size;

		GizmoInstance.Input.IsHovered = IsUnderMouse;
		GizmoInstance.Input.Modifiers = Editor.Application.KeyboardModifiers;
		GizmoInstance.Input.CursorPosition = Editor.Application.CursorPosition;
		GizmoInstance.Input.LeftMouse = Editor.Application.MouseButtons.HasFlag( MouseButtons.Left );
		GizmoInstance.Input.RightMouse = Editor.Application.MouseButtons.HasFlag( MouseButtons.Right );

		GizmoInstance.Input.CursorPosition -= ScreenPosition;
		GizmoInstance.Input.CursorRay = Camera.ScreenPixelToRay( GizmoInstance.Input.CursorPosition );

		if ( !GizmoInstance.Input.IsHovered )
		{
			GizmoInstance.Input.LeftMouse = false;
			GizmoInstance.Input.RightMouse = false;
		}
	}
}