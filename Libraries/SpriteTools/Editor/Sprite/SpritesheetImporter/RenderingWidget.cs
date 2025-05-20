using Editor;
using Sandbox;
using System;
using System.Linq;

namespace SpriteTools.SpritesheetImporter;

public class RenderingWidget : SpriteRenderingWidget
{
	SpritesheetImporter Importer;

	float planeWidth;
	float planeHeight;
	float startX;
	float startY;
	float frameWidth;
	float frameHeight;
	float xSeparation;
	float ySeparation;
	Vector3 startMovePosition;

	bool isCreating = false;
	Vector2 startCreatePosition;
	Vector2 endCreatePosition;

	SpritesheetImporterFrame Selected = null;

	RealTimeSince timeSinceLastCornerHover = 0;

	public RenderingWidget ( SpritesheetImporter importer, Widget parent ) : base( parent )
	{
		Importer = importer;
		AcceptDrops = false;
		IsDraggable = false;
	}

	protected override void OnMousePress ( MouseEvent e )
	{
		base.OnMousePress( e );
	}

	protected override void OnMouseMove ( MouseEvent e )
	{
		base.OnMouseMove( e );
	}

	protected override void OnMouseReleased ( MouseEvent e )
	{
		base.OnMouseReleased( e );
	}

	void CommitSettings ()
	{

	}

	[EditorEvent.Frame]
	public void Frame ()
	{
		if ( Importer.Settings.NumberOfFrames <= 0 ) Importer.Settings.NumberOfFrames = 1;
		if ( Importer.Settings.FramesPerRow <= 0 ) Importer.Settings.FramesPerRow = 1;
		if ( Importer.Settings.FramesPerRow > Importer.Settings.NumberOfFrames )
		{
			Importer.Settings.NumberOfFrames = Importer.Settings.FramesPerRow;
		}

		SceneInstance.Input.IsHovered = IsUnderMouse;
		SceneInstance.UpdateInputs( Camera, this );

		if ( timeSinceLastCornerHover > 0.025f )
		{
			Cursor = CursorShape.Arrow;
		}


		using ( SceneInstance.Push() )
		{
			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.LineThickness = 2f;

			planeWidth = 100f * TextureRect.Transform.Scale.y;
			planeHeight = 100f * TextureRect.Transform.Scale.x;

			startX = Importer.Settings.HorizontalCellOffset * Importer.Settings.FrameWidth + Importer.Settings.HorizontalPixelOffset;
			startY = Importer.Settings.VerticalCellOffset * Importer.Settings.FrameHeight + Importer.Settings.VerticalPixelOffset;
			startX = ( startX / TextureSize.x * planeWidth ) - ( planeWidth / 2f );
			startY = ( startY / TextureSize.y * planeHeight ) - ( planeHeight / 2f );
			frameWidth = Importer.Settings.FrameWidth / TextureSize.x * planeWidth;
			frameHeight = Importer.Settings.FrameHeight / TextureSize.y * planeHeight;
			xSeparation = Importer.Settings.HorizontalSeparation / TextureSize.x * planeWidth;
			ySeparation = Importer.Settings.VerticalSeparation / TextureSize.y * planeHeight;

			int framesPerRow = Math.Clamp( Importer.Settings.FramesPerRow, 1, (int)TextureSize.x / Importer.Settings.FrameWidth );

			var isManualMode = Importer.PageIndex == 1;
			if ( !isManualMode )
			{
				using ( Gizmo.Scope( "import_settings" ) )
				{
					for ( int i = 0; i < Importer.Settings.NumberOfFrames; i++ )
					{
						int cellX = i % framesPerRow;
						int cellY = i / framesPerRow;

						float x = startX + ( cellX ) * ( frameWidth + xSeparation );
						float y = startY + ( cellY ) * ( frameHeight + ySeparation );

						// Draw Box
						Gizmo.Draw.Line( new Vector3( y, x, 0 ), new Vector3( y, x + frameWidth, 0 ) );
						Gizmo.Draw.Line( new Vector3( y, x + frameWidth, 0 ), new Vector3( y + frameHeight, x + frameWidth, 0 ) );
						Gizmo.Draw.Line( new Vector3( y + frameHeight, x + frameWidth, 0 ), new Vector3( y + frameHeight, x, 0 ) );
						Gizmo.Draw.Line( new Vector3( y + frameHeight, x, 0 ), new Vector3( y, x, 0 ) );
					}
				}
			}

			if ( isManualMode || Importer.HasModified )
				using ( Gizmo.Scope( "committed_frames" ) )
				{
					Gizmo.Draw.Color = new Color( 0.1f, 0.4f, 1f );
					Gizmo.Draw.LineThickness = 3f;

					var frames = Importer.Frames.ToList();
					foreach ( var frame in frames )
					{
						FrameControl( frame, isManualMode );
					}
				}


			if ( isManualMode )
			{
				using ( Gizmo.Scope( "background" ) )
				{
					var planeBBox = new BBox( new Vector3( -planeHeight / 2f, -planeWidth / 2f, -10 ), new Vector3( planeHeight, planeWidth, 0f ) / 2f );
					Gizmo.Hitbox.BBox( planeBBox );
					//Gizmo.Draw.Color = Color.Red.WithAlpha( 0.6f );
					//Gizmo.Draw.SolidBox( planeBBox );

					if ( Gizmo.Pressed.This )
					{
						var rayPos = Gizmo.CurrentRay.Position;
						if ( Gizmo.WasLeftMousePressed )
						{
							if ( Selected is null )
							{
								isCreating = true;
								startCreatePosition = RayPositionToPixel( rayPos );
							}
							Selected = null;
						}

						if ( isCreating )
						{
							endCreatePosition = RayPositionToPixel( rayPos );
						}
					}
					else
					{
						if ( isCreating )
						{
							var start = startCreatePosition;
							var end = endCreatePosition;
							var size = new Vector2( MathF.Abs( end.y - start.y ), MathF.Abs( end.x - start.x ) );
							var topLeft = new Vector2( MathF.Min( start.y, end.y ), MathF.Min( start.x, end.x ) );
							if ( size.x > 0 && size.y > 0 )
							{
								var rect = new Rect( topLeft, size );
								var newFrame = new SpritesheetImporterFrame( rect );
								Importer.Frames.Add( newFrame );
							}
						}
						isCreating = false;
					}
				}

				if ( isCreating )
				{
					using ( Gizmo.Scope( "creating" ) )
					{
						var start = PixelToRayPosition( startCreatePosition );
						var end = PixelToRayPosition( endCreatePosition );
						var size = ( end - start );
						var bbox = BBox.FromPositionAndSize( start + size / 2f, size );
						Gizmo.Draw.Color = Color.Yellow;
						Gizmo.Draw.LineThickness = 3f;
						Gizmo.Draw.LineBBox( bbox );
					}
				}
			}
		}
	}

	void FrameControl ( SpritesheetImporterFrame frame, bool isManualMode )
	{
		if ( frame is null ) return;
		bool isSelected = Selected == frame; ;
		using ( Gizmo.Scope( "frame_" + frame.Id ) )
		{
			var x = startX + frame.Rect.Position.x;
			var y = startY + frame.Rect.Position.y;
			var width = frame.Rect.Size.x;
			var height = frame.Rect.Size.y;

			var visualX = x - startX;
			var visualY = y - startY;
			visualX = ( visualX / TextureSize.x * planeWidth ) - ( planeWidth / 2f );
			visualY = ( visualY / TextureSize.y * planeHeight ) - ( planeHeight / 2f );
			var visualWidth = width;
			var visualHeight = height;
			visualWidth = ( visualWidth / TextureSize.x * planeWidth );
			visualHeight = ( visualHeight / TextureSize.y * planeHeight );

			var bbox = BBox.FromPositionAndSize( new Vector3( visualY + visualHeight / 2f, visualX + visualWidth / 2f, 1f ), new Vector3( visualHeight, visualWidth, 1f ) );
			Gizmo.Hitbox.BBox( bbox );

			if ( isManualMode )
			{

				if ( isSelected || Gizmo.Pressed.This )
				{
					Gizmo.Draw.LineThickness = 4;
					Gizmo.Draw.Color = Color.Yellow;
				}

				var visualRayPos = ( Gizmo.CurrentRay.Position / new Vector3( planeWidth, planeHeight, 1f ) ) * new Vector3( TextureSize.x, TextureSize.y, 1f );

				if ( Gizmo.WasLeftMousePressed )
				{
					startMovePosition = visualRayPos;
				}

				if ( Gizmo.Pressed.This )
				{
					Cursor = CursorShape.SizeAll;
					timeSinceLastCornerHover = 0f;
					var preDelta = startMovePosition - visualRayPos;
					//preDelta = ( preDelta * new Vector3( TextureSize.x, TextureSize.y, 1 ) ) / new Vector2( planeWidth, planeHeight );
					var deltaf = new Vector2( -preDelta.y, -preDelta.x );
					//deltaf = ( deltaf / new Vector2( planeWidth, planeHeight ) );// * new Vector2( TextureSize.x, TextureSize.y );
					if ( Math.Abs( deltaf.x ) >= 1f )
					{
						int xx = (int)deltaf.x;
						if ( xx != 0 && CanExpand( frame, xx, 0 ) )
						{
							startMovePosition += new Vector3( 0, xx );
							var rect = frame.Rect;
							rect.Position = frame.Rect.Position + new Vector2( xx, 0 );
							frame.Rect = rect;
							Importer.HasModified = true;
						}
					}
					if ( Math.Abs( deltaf.y ) >= 1f )
					{
						int yy = (int)deltaf.y;
						if ( yy != 0 && CanExpand( frame, 0, yy ) )
						{
							startMovePosition += new Vector3( yy, 0 );
							var rect = frame.Rect;
							rect.Position = frame.Rect.Position + new Vector2( 0, yy );
							frame.Rect = rect;
							Importer.HasModified = true;
						}
					}
				}

				if ( Gizmo.IsHovered )
				{
					Cursor = CursorShape.Finger;
					timeSinceLastCornerHover = 0f;
					using ( Gizmo.Scope( "hover" ) )
					{
						Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha( 0.5f );
						Gizmo.Draw.SolidBox( bbox );
					}
					if ( Gizmo.WasLeftMousePressed )
					{
						Selected = frame;
					}
					else if ( Gizmo.WasRightMousePressed )
					{
						Importer.Frames.Remove( frame );
						Importer.HasModified = true;
					}
				}

				if ( isSelected )
				{
					using ( Gizmo.Scope( "selected" ) )
					{
						Gizmo.Draw.Color = Color.Orange;
						Gizmo.Draw.LineThickness = 3;

						// Draggable Corners
						for ( int i = -1; i <= 1; i++ )
						{
							for ( int j = -1; j <= 1; j++ )
							{
								if ( i == 0 && j == 0 ) continue;
								DraggableCorner( frame, i, j, visualX + visualWidth * ( i + 1 ) / 2f, visualY + visualHeight * ( j + 1 ) / 2f );
							}
						}
					}
				}
			}

			DrawBox( visualX, visualY, visualWidth, visualHeight );
		}
	}

	void DraggableCorner ( SpritesheetImporterFrame frame, int x, int y, float xx, float yy )
	{
		int currentX = (int)frame.Rect.Position.x;
		int currentY = (int)frame.Rect.Position.y;
		float xi = currentX + x / 2f;
		float yi = currentY + y / 2f;
		float width = (int)frame.Rect.Size.x;
		float height = (int)frame.Rect.Size.y;
		var radius = MathX.Remap( targetZoom, 1, 1000, 0.2f, 4f );

		// Can Expand Logic
		bool canExpandX = CanExpand( frame, x, 0 );
		bool canExpandY = CanExpand( frame, 0, y );

		// Can Shrink Logic
		bool canShrinkX = !( x != 0 && frame.Rect.Size.x == 1 );
		bool canShrinkY = !( y != 0 && frame.Rect.Size.y == 1 );

		bool canDrag = ( canExpandX && x != 0 ) || ( canExpandY && y != 0 ) || ( canShrinkX && x != 0 ) || ( canShrinkY && y != 0 );

		using ( Gizmo.Scope( $"corner_{x}_{y}" ) )
		{
			if ( !canDrag )
			{
				Gizmo.Draw.LineThickness = 1;
				Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha( 0.2f );
			}

			if ( canDrag )
			{
				var bbox = BBox.FromPositionAndSize( new Vector3( yy, xx, 2f ), new Vector3( radius, radius, radius ) * 1.5f );
				Gizmo.Hitbox.BBox( bbox );

				if ( Gizmo.Pressed.This )
				{
					Gizmo.Draw.Color = Color.Lerp( Gizmo.Draw.Color, Color.Red, 0.3f );

					var preDelta = bbox.Center - Gizmo.CurrentRay.Position;
					preDelta = ( preDelta * new Vector3( TextureSize.x, TextureSize.y, 1 ) ) / new Vector2( planeWidth, planeHeight );
					var delta = new Vector2( -preDelta.y, -preDelta.x );//Gizmo.Pressed.CursorDelta;
					var position = frame.Rect.Position;
					var size = frame.Rect.Size;

					// Horizontal check
					if ( x != 0 )
					{
						if ( Math.Abs( delta.x ) >= 1f )
						{
							var am = (int)Math.Abs( delta.x );
							// Expanding
							if ( Math.Sign( delta.x ) == Math.Sign( x ) )
							{
								if ( canExpandX )
								{
									// Expanding Backwards
									if ( delta.x < 0 )
									{
										position -= new Vector2Int( am, 0 );
										size += new Vector2Int( am, 0 );
									}
									else
									{
										size += new Vector2Int( am, 0 );
									}
									Importer.HasModified = true;
								}
							}
							// Shinking
							else if ( canShrinkX && ( size.x + delta.x ) > 1 )
							{

								// Shrinking Backwards
								if ( delta.x > 0 )
								{
									size -= new Vector2Int( am, 0 );
									position += new Vector2Int( am, 0 );
								}
								else
								{
									size -= new Vector2Int( am, 0 );
								}
								Importer.HasModified = true;
							}
						}
					}

					// Vertical check
					if ( y != 0 )
					{
						if ( Math.Abs( delta.y ) >= 1f )
						{
							var am = (int)Math.Abs( delta.y );
							if ( Math.Sign( delta.y ) == Math.Sign( y ) )
							{
								if ( canExpandY )
								{
									// Expanding
									if ( delta.y < 0 )
									{
										position -= new Vector2Int( 0, am );
										size += new Vector2Int( 0, am );
									}
									else
									{
										size += new Vector2Int( 0, am );
									}
									Importer.HasModified = true;
								}
							}
							else if ( canShrinkY && ( size.y + delta.y ) > 1 )
							{
								// Shrink
								if ( delta.y > 0 )
								{
									size -= new Vector2Int( 0, am );
									position += new Vector2Int( 0, am );
								}
								else
								{
									size -= new Vector2Int( 0, am );
								}
								Importer.HasModified = true;
							}
						}
					}

					if ( frame.Rect.Position != position || frame.Rect.Size != size )
					{
						var rect = frame.Rect;
						rect.Position = position;
						rect.Size = size;
						frame.Rect = rect;
					}
				}
			}

			if ( canDrag && Gizmo.IsHovered )
			{
				Gizmo.Draw.SolidSphere( new Vector3( yy, xx, 10f ), 0.5f, 2, 4 );
				Cursor = (x, y) switch
				{
					(-1, -1 ) => CursorShape.SizeFDiag,
					(-1, 0 ) => CursorShape.SizeH,
					(-1, 1 ) => CursorShape.SizeBDiag,
					(0, -1 ) => CursorShape.SizeV,
					(0, 1 ) => CursorShape.SizeV,
					(1, -1 ) => CursorShape.SizeBDiag,
					(1, 0 ) => CursorShape.SizeH,
					(1, 1 ) => CursorShape.SizeFDiag,
					_ => CursorShape.Arrow
				};
				timeSinceLastCornerHover = 0f;
			}
			else
			{
				Gizmo.Draw.LineCircle( new Vector3( yy, xx, 10f ), Vector3.Up, radius, 0, 360, 8 );
			}


		}
	}

	bool CanExpand ( SpritesheetImporterFrame frame, int x, int y )
	{
		int currentX = (int)frame.Rect.Position.x;
		int currentY = (int)frame.Rect.Position.y;
		int width = (int)frame.Rect.Size.x;
		int height = (int)frame.Rect.Size.y;

		if ( x != 0 )
		{
			int nextX = currentX + x;
			if ( nextX < 0 || ( nextX + width ) >= TextureSize.x ) return false;
		}

		if ( y != 0 )
		{
			int nextY = currentY + y;
			if ( nextY < 0 || ( nextY + height ) >= TextureSize.y ) return false;
		}

		return true;
	}

	void DrawBox ( float x, float y, float width, float height )
	{
		Gizmo.Draw.Line( new Vector3( y, x, 0 ), new Vector3( y, x + width, 0 ) );
		Gizmo.Draw.Line( new Vector3( y, x, 0 ), new Vector3( y + height, x, 0 ) );
		Gizmo.Draw.Line( new Vector3( y + height, x, 0 ), new Vector3( y + height, x + width, 0 ) );
		Gizmo.Draw.Line( new Vector3( y + height, x + width, 0 ), new Vector3( y, x + width, 0 ) );
	}

	Vector2 RayPositionToPixel ( Vector3 position )
	{
		var x = position.y + ( planeWidth / 2f );
		var y = position.x + ( planeHeight / 2f );
		x = MathF.Floor( ( x / planeWidth ) * TextureSize.x );
		y = MathF.Floor( ( y / planeHeight ) * TextureSize.y );
		return new Vector2( y, x );
	}

	Vector3 PixelToRayPosition ( Vector2 position )
	{
		var x = position.y / TextureSize.x * planeWidth - ( planeWidth / 2f );
		var y = position.x / TextureSize.y * planeHeight - ( planeHeight / 2f );
		return new Vector3( y, x, 0 );
	}
}