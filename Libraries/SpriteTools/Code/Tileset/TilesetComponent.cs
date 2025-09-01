using Sandbox;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SpriteTools;

[Category( "2D" )]
[Title( "2D Tileset" )]
[Icon( "calendar_view_month" )]
[Tint( EditorTint.Yellow )]
public partial class TilesetComponent : Component, Component.ExecuteInEditor
{
	/// <summary>
	/// The Layers within the TilesetComponent
	/// </summary>
	[Property, Group( "Layers" )]
	public List<Layer> Layers
	{
		get => _layers;
		set
		{
			_layers = value;
			foreach ( var layer in _layers )
			{
				layer.TilesetComponent = this;
			}
		}
	}
	List<Layer> _layers;

	[Property, WideMode( HasLabel = false )]
	ComponentControls InternalControls { get; set; }

	/// <summary>
	/// Whether or not the component should generate a collider based on the specified Collision Layer
	/// </summary>
	[Property, FeatureEnabled( "Collision" )]
	public bool HasCollider
	{
		get => _hasCollider;
		set
		{
			if ( value == _hasCollider ) return;
			_hasCollider = value;
			if ( value ) CreateCollider();
			else DestroyCollider();
		}
	}
	bool _hasCollider;

	/// <inheritdoc cref="Collider.Static" />
	[Property, Feature( "Collision" )]
	public bool Static
	{
		get => _static;
		set
		{
			if ( value == _static ) return;
			_static = value;
			if ( Collider.IsValid() ) Collider.Static = value;
		}
	}
	private bool _static = true;

	/// <inheritdoc cref="Collider.IsTrigger" />
	[Property, Feature( "Collision" )]
	public bool IsTrigger
	{
		get => _isTrigger;
		set
		{
			if ( value == _isTrigger ) return;
			_isTrigger = value;
			if ( Collider.IsValid() ) Collider.IsTrigger = value;
		}
	}
	private bool _isTrigger = false;

	/// <summary>
	/// The width of the generated collider
	/// </summary>
	[Property, Feature( "Collision" )]
	public float ColliderWidth
	{
		get => _colliderWidth;
		set
		{
			if ( value < 0f ) _colliderWidth = 0f;
			else if ( value == _colliderWidth ) return;
			_colliderWidth = value;
			Collider?.RebuildMesh();
		}
	}
	float _colliderWidth;

	/// <inheritdoc cref="Collider.Friction" />
	[Property, Feature( "Collision" ), Group( "Surface Properties" )]
	[Range( 0f, 1f, true, true ), Step( 0.01f )]
	public float? Friction
	{
		get => _friction;
		set
		{
			if ( value == _friction ) return;
			_friction = value;
			if ( Collider.IsValid() ) Collider.Friction = value;
		}
	}
	private float? _friction;

	/// <inheritdoc cref="Collider.Surface" />
	[Property, Feature( "Collision" ), Group( "Surface Properties" )]
	public Surface Surface
	{
		get => _surface;
		set
		{
			if ( value == _surface ) return;
			_surface = value;
			if ( Collider.IsValid() ) Collider.Surface = value;
		}
	}
	private Surface _surface;

	/// <inheritdoc cref="Collider.SurfaceVelocity" />
	[Property, Feature( "Collision" ), Group( "Surface Properties" )]
	public Vector3 SurfaceVelocity
	{
		get => _surfaceVelocity;
		set
		{
			if ( value == _surfaceVelocity ) return;
			_surfaceVelocity = value;
			if ( Collider.IsValid() ) Collider.SurfaceVelocity = value;
		}
	}
	private Vector3 _surfaceVelocity;

	[Property, Feature( "Collision" ), Group( "Trigger Actions" ), ShowIf( nameof( IsTrigger ), true )]
	public Action<Collider> OnTriggerEnter { get; set; }

	[Property, Feature( "Collision" ), Group( "Trigger Actions" ), ShowIf( nameof( IsTrigger ), true )]
	public Action<Collider> OnTriggerExit { get; set; }

	/// <summary>
	/// Whether or not the associated Collider is dirty. Setting this to true will rebuild the Collider on the next frame.
	/// </summary>
	public bool IsDirty
	{
		get => Collider?.IsDirty ?? false;
		set
		{
			if ( !Collider.IsValid() ) return;
			Collider.IsDirty = value;
		}
	}
	TilesetCollider Collider;
	internal List<TilesetSceneObject> _sos = new();

	protected override void OnEnabled ()
	{
		base.OnEnabled();

		CreateCollider();

		if ( Layers is null ) return;
		foreach ( var layer in Layers )
		{
			layer.TilesetComponent = this;
		}
	}

	protected override void OnDisabled ()
	{
		base.OnDisabled();

		DestroyCollider();

		foreach ( var _so in _sos )
		{
			_so.Delete();
		}
		_sos.Clear();
	}

	protected override void OnUpdate ()
	{
		base.OnUpdate();

		_sos ??= new();
		Layers ??= new();
		var _newSos = new List<TilesetSceneObject>();
		foreach ( var sos in _sos )
		{
			if ( sos is not null || sos.IsValid() )
			{
				_newSos.Add( sos );
			}
			else
			{
				sos?.Delete();
			}
		}
		_sos = _newSos;
		if ( Layers.Count != _sos.Count )
		{
			RebuildSceneObjects();
		}
	}

	protected override void OnTagsChanged ()
	{
		base.OnTagsChanged();

		foreach ( var _so in _sos )
			_so?.Tags.SetFrom( Tags );
	}

	protected override void OnPreRender ()
	{
		base.OnPreRender();

		if ( Layers is null ) return;
		if ( Layers.Count == 0 )
		{
			return;
		}

		foreach ( var _so in _sos )
		{
			if ( !_so.IsValid() ) continue;
			_so.RenderingEnabled = true;
			_so.Transform = Transform.World;
			_so.Flags.CastShadows = false;
			_so.Flags.IsOpaque = false;
			_so.Flags.IsTranslucent = true;
		}
	}

	protected override void DrawGizmos ()
	{
		base.DrawGizmos();

		var bounds = GetBounds();
		Gizmo.Hitbox.BBox( bounds );

		if ( !Gizmo.IsSelected ) return;

		using ( Gizmo.Scope( "tileset", new Transform( 0, WorldRotation.Inverse, 1 ) ) )
		{
			Gizmo.Draw.Color = Color.Yellow;
			Gizmo.Draw.LineThickness = 1f;
			Gizmo.Draw.LineBBox( bounds );
		}
	}

	public BBox GetBounds ()
	{
		var bounds = BBox.FromPositionAndSize( 0, 0 );
		foreach ( var _so in _sos )
		{
			if ( !_so.IsValid() ) continue;

			var boundSize = _so.Bounds.Size;
			if ( ( boundSize.x + boundSize.y + boundSize.z ) > ( bounds.Size.x + bounds.Size.y + bounds.Size.z ) )
			{
				bounds = _so.Bounds.Translate( -_so.Position );
			}
		}

		return bounds;
	}

	void RebuildSceneObjects ()
	{
		foreach ( var _so in _sos )
		{
			_so.Delete();
		}

		_sos = new List<TilesetSceneObject>();
		for ( int i = 0; i < Layers.Count; i++ )
		{
			_sos.Add( new TilesetSceneObject( this, Scene.SceneWorld, i ) );
		}
	}

	void CreateCollider ()
	{
		if ( !HasCollider ) return;
		if ( Collider.IsValid() ) return;
		Collider = AddComponent<TilesetCollider>();
		Collider.Flags |= ComponentFlags.Hidden | ComponentFlags.NotSaved;
		Collider.Tileset = this;
		Collider.Static = Static;
		Collider.IsTrigger = IsTrigger;
		Collider.Friction = Friction;
		Collider.Surface = Surface;
		Collider.SurfaceVelocity = SurfaceVelocity;
		Collider.OnTriggerEnter += OnTriggerEnter;
		Collider.OnTriggerExit += OnTriggerExit;
	}

	void DestroyCollider ()
	{
		if ( Collider.IsValid() )
			Collider.Destroy();
		Collider = null;
	}

	/// <summary>
	/// Returns the Layer with the specified name
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Layer GetLayerFromName ( string name )
	{
		return Layers.FirstOrDefault( x => x.Name == name );
	}

	/// <summary>
	/// Returns the Layer at the specified index
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public Layer GetLayerFromIndex ( int index )
	{
		if ( index < 0 || index >= Layers.Count ) return null;
		return Layers[index];
	}

	public class Layer
	{
		/// <summary>
		/// The name of the Layer
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Whether or not this Layer is currently being rendered
		/// </summary>
		public bool IsVisible { get; set; }

		/// <summary>
		/// Whether or not this Layer is locked. Locked Layers will ignore any attempted changes
		/// </summary>
		public bool IsLocked { get; set; }

		/// <summary>
		/// The Tileset that this Layer uses
		/// </summary>
		[Property, Group( "Selected Layer" )] public TilesetResource TilesetResource { get; set; }

		/// <summary>
		/// The height of the Layer
		/// </summary>
		[Property, Group( "Selected Layer" )] public float? Height { get; set; } = null;

		/// <summary>
		/// Whether or not this Layer dictates the collision mesh
		/// </summary>
		[Group( "Selected Layer" ), Title( "Has Collisions" )] public bool IsCollisionLayer { get; set; }

		/// <summary>
		/// A dictionary of all Tiles in the layer by their position.
		/// </summary>
		public Dictionary<Vector2Int, Tile> Tiles { get; set; }

		/// <summary>
		/// A dictionary containing a list of positions for each Autotile Brush by their ID.
		/// </summary>
		public Dictionary<Guid, List<AutotilePosition>> Autotiles { get; set; }

		/// <summary>
		/// The TilesetComponent that this Layer belongs to
		/// </summary>
		[JsonIgnore, Hide] public TilesetComponent TilesetComponent { get; set; }

		public Layer ( string name = "Untitled Layer" )
		{
			Name = name;
			IsVisible = true;
			IsLocked = false;
			Tiles = new();
		}

		/// <summary>
		/// Returns an exact copy of the Layer
		/// </summary>
		/// <returns></returns>
		public Layer Copy ()
		{
			var layer = new Layer( Name )
			{
				IsVisible = IsVisible,
				IsLocked = IsLocked,
				Tiles = new(),
				IsCollisionLayer = false,
				TilesetComponent = TilesetComponent,
			};

			foreach ( var tile in Tiles )
			{
				layer.Tiles[tile.Key] = tile.Value.Copy();
			}

			return layer;
		}

		/// <summary>
		/// Set a tile at the specified position. Will fail if IsLocked is true.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="tileId"></param>
		/// <param name="cellPosition"></param>
		/// <param name="angle"></param>
		/// <param name="flipX"></param>
		/// <param name="flipY"></param>
		/// <param name="rebuild"></param>
		public void SetTile ( Vector2Int position, Guid tileId, Vector2Int cellPosition = default, int angle = 0, bool flipX = false, bool flipY = false, bool rebuild = true, bool removeAutotile = true )
		{
			if ( IsLocked ) return;
			var tile = new Tile( tileId, cellPosition, angle, flipX, flipY );
			Tiles[position] = tile;
			if ( rebuild && TilesetComponent.IsValid() )
				TilesetComponent.IsDirty = true;

			if ( removeAutotile && Autotiles is not null )
			{
				foreach ( var group in Autotiles )
				{
					foreach ( var autotile in group.Value )
					{
						if ( autotile.Position == position )
						{
							Autotiles[group.Key].Remove( autotile );
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// Get the Tile at the specified position
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Tile GetTile ( Vector2Int position )
		{
			return Tiles[position];
		}

		/// <summary>
		/// Get the Tile at the specified position
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Tile GetTile ( Vector3 position )
		{
			return Tiles[new Vector2Int( (int)position.x, (int)position.y )];
		}

		/// <summary>
		/// Remove the Tile at the specified position. Will fail if IsLocked is true.
		/// </summary>
		/// <param name="position"></param>
		public void RemoveTile ( Vector2Int position )
		{
			if ( IsLocked ) return;
			Tiles.Remove( position );

			if ( Autotiles is not null )
			{
				foreach ( var group in Autotiles )
				{
					foreach ( var autotile in group.Value )
					{
						if ( autotile.Position == position )
						{
							Autotiles[group.Key].Remove( autotile );
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// Set an Autotile at the specified position. Will fail if IsLocked is true.
		/// </summary>
		/// <param name="autotileBrush"></param>
		/// <param name="position"></param>
		/// <param name="enabled"></param>
		///	<param name="update"></param>
		/// <param name="isMerging"></param>
		public void SetAutotile ( AutotileBrush autotileBrush, Vector2Int position, bool enabled = true, bool update = true, bool isMerging = false )
		{
			SetAutotile( autotileBrush.Id, position, enabled, update, isMerging );
		}

		/// <summary>
		/// Set an Autotile at the specified position. Will fail if IsLocked is true.
		/// </summary>
		/// <param name="autotileId"></param>
		/// <param name="position"></param>
		/// <param name="enabled"></param>
		/// <param name="update"></param>
		/// <param name="isMerging"></param>
		public void SetAutotile ( Guid autotileId, Vector2Int position, bool enabled = true, bool update = true, bool isMerging = false )
		{
			if ( IsLocked ) return;
			Autotiles ??= new();

			foreach ( var group in Autotiles )
			{
				if ( group.Key == autotileId ) continue;
				foreach ( var autotile in group.Value )
				{
					if ( autotile.Position == position )
					{
						Autotiles[group.Key].Remove( autotile );
						break;
					}
				}
			}

			if ( !Autotiles.ContainsKey( autotileId ) )
				Autotiles[autotileId] = new List<AutotilePosition>();

			bool shouldUpdate = false;
			if ( enabled )
			{
				if ( !Autotiles[autotileId].Any( x => x.Position == position ) )
				{
					Autotiles[autotileId].Add( new( position, isMerging ) );
					shouldUpdate = true;
				}
			}
			else
			{
				var foundPos = Autotiles[autotileId].FirstOrDefault( x => x.Position == position );
				if ( foundPos is not null )
				{
					Tiles.Remove( position );
					Autotiles[autotileId].Remove( foundPos );
					shouldUpdate = true;
				}
				else
				{
					RemoveTile( position );
				}
			}

			if ( update && shouldUpdate )
			{
				UpdateAutotile( autotileId, position, !enabled, shouldMerge: isMerging );
			}
		}

		/// <summary>
		/// Update the Autotile at the specified position. Used when manually modifying the placed autotiles.
		/// </summary>
		/// <param name="autotileId"></param>
		/// <param name="position"></param>
		/// <param name="checkErased"></param>
		/// <param name="updateSurrounding"></param>
		/// <param name="shouldMerge"></param>
		public void UpdateAutotile ( Guid autotileId, Vector2Int position, bool checkErased, bool updateSurrounding = true, bool shouldMerge = false )
		{
			if ( !Autotiles.ContainsKey( autotileId ) ) return;

			var brush = TilesetResource.AutotileBrushes.FirstOrDefault( x => x.Id == autotileId );
			var autotile = Autotiles[autotileId].FirstOrDefault( x => x.Position == position );
			if ( autotile is not null )
			{
				if ( shouldMerge ) autotile.ShouldMerge = true;
				if ( autotile.ShouldMerge ) shouldMerge = true;

				var bitmask = GetAutotileBitmask( autotileId, position, shouldMerge );
				if ( bitmask == -1 )
				{
					if ( checkErased ) RemoveTile( position );
				}
				else
				{
					if ( brush is not null )
					{
						var tile = brush.GetTileFromBitmask( bitmask );
						if ( tile is not null )
						{
							SetTile( position, tile.Id, Vector2Int.Zero, 0, false, false, false, removeAutotile: false );
						}
						else
						{
							Log.Warning( $"Tile not found for bitmask {bitmask} in AutotileBrush {brush.Name}" );
						}
					}
				}
			}

			if ( updateSurrounding )
			{
				var up = position.WithY( position.y + 1 );
				var down = position.WithY( position.y - 1 );
				var left = position.WithX( position.x - 1 );
				var right = position.WithX( position.x + 1 );
				var upLeft = up.WithX( left.x );
				var upRight = up.WithX( right.x );
				var downLeft = down.WithX( left.x );
				var downRight = down.WithX( right.x );

				if ( brush is not null && brush.AutotileType == AutotileType.Bitmask2x2Edge )
				{
					ClearInvalidAutotile( autotileId, up );
					ClearInvalidAutotile( autotileId, down );
					ClearInvalidAutotile( autotileId, left );
					ClearInvalidAutotile( autotileId, right );
					ClearInvalidAutotile( autotileId, upLeft );
					ClearInvalidAutotile( autotileId, upRight );
					ClearInvalidAutotile( autotileId, downLeft );
					ClearInvalidAutotile( autotileId, downRight );
				}

				UpdateAutotile( autotileId, up, checkErased, false, shouldMerge );
				UpdateAutotile( autotileId, down, checkErased, false, shouldMerge );
				UpdateAutotile( autotileId, left, checkErased, false, shouldMerge );
				UpdateAutotile( autotileId, right, checkErased, false, shouldMerge );
				UpdateAutotile( autotileId, upLeft, checkErased, false, shouldMerge );
				UpdateAutotile( autotileId, upRight, checkErased, false, shouldMerge );
				UpdateAutotile( autotileId, downLeft, checkErased, false, shouldMerge );
				UpdateAutotile( autotileId, downRight, checkErased, false, shouldMerge );
			}
		}

		void ClearInvalidAutotile ( Guid autotileId, Vector2Int position )
		{
			if ( !Tiles.TryGetValue( position, out var tile ) ) return;

			var brush = TilesetResource.AutotileBrushes.FirstOrDefault( x => x.Id == autotileId );

			if ( brush is null ) return;
			if ( brush.AutotileType != AutotileType.Bitmask2x2Edge ) return;
			if ( !brush.Tiles.Any( x => x.Tiles.Any( y => y.Id == tile.TileId ) ) ) return;
			if ( GetAutotileBitmask( autotileId, position ) != -1 ) return;

			RemoveTile( position );
		}


		public int GetAutotileBitmask ( Guid autotileId, Vector2Int position, bool mergeAll = false )
		{
			if ( Autotiles is null || ( !mergeAll && !Autotiles.ContainsKey( autotileId ) ) ) return -1;

			List<AutotilePosition> positions = new();
			if ( mergeAll )
			{
				foreach ( var kvp in Autotiles )
				{
					positions.AddRange( kvp.Value );
				}
			}
			else
			{
				positions = Autotiles[autotileId];
			}
			int value = 0;

			var up = position.WithY( position.y + 1 );
			var down = position.WithY( position.y - 1 );
			var left = position.WithX( position.x - 1 );
			var right = position.WithX( position.x + 1 );

			var brush = TilesetResource.AutotileBrushes.FirstOrDefault( x => x.Id == autotileId );
			if ( brush is null ) return 0;

			bool is2x2 = brush.AutotileType == AutotileType.Bitmask2x2Edge;
			if ( is2x2 )
			{
				foreach ( var pos in positions )
				{
					if ( pos.Position == up ) value += 1;
					if ( pos.Position == left ) value += 2;
					if ( pos.Position == right ) value += 4;
					if ( pos.Position == down ) value += 8;
				}
				switch ( value )
				{
					case 0:
					case 1:
					case 2:
					case 4:
					case 8:
					case 9:
					case 6:
						return -1;
				}
				value = 0;
			}

			var upLeft = up.WithX( left.x );
			var upRight = up.WithX( right.x );
			var downLeft = down.WithX( left.x );
			var downRight = down.WithX( right.x );

			foreach ( var thing in positions )
			{
				var pos = thing.Position;
				if ( pos == upLeft ) value += 1;
				if ( pos == up ) value += 2;
				if ( pos == upRight ) value += 4;
				if ( pos == left ) value += 8;
				if ( pos == right ) value += 16;
				if ( pos == downLeft ) value += 32;
				if ( pos == down ) value += 64;
				if ( pos == downRight ) value += 128;
			}

			if ( is2x2 )
			{
				switch ( value )
				{
					case 46:
					case 116:
					case 147:
					case 201:
						return -1;
				}
			}

			return value;
		}

		public int GetAutotileBitmask ( Guid autotileId, Vector2Int position, Dictionary<Vector2Int, bool> overrides, bool mergeAll = false )
		{
			if ( Autotiles is null ) return -1;

			var positions = new List<Vector2Int>();
			foreach ( var thing in Autotiles )
			{
				if ( !mergeAll && thing.Key != autotileId ) continue;
				foreach ( var pos in thing.Value )
				{
					if ( !positions.Contains( pos.Position ) )
						positions.Add( pos.Position );
				}
			}
			int value = 0;

			foreach ( var ride in overrides )
			{
				if ( ride.Value )
				{
					if ( !positions.Contains( ride.Key ) )
					{
						positions.Add( ride.Key );
					}
				}
				else
				{
					if ( positions.Contains( ride.Key ) )
					{
						positions.Remove( ride.Key );
					}
				}
			}

			var up = position.WithY( position.y + 1 );
			var down = position.WithY( position.y - 1 );
			var left = position.WithX( position.x - 1 );
			var right = position.WithX( position.x + 1 );
			var upLeft = up.WithX( left.x );
			var upRight = up.WithX( right.x );
			var downLeft = down.WithX( left.x );
			var downRight = down.WithX( right.x );

			foreach ( var pos in positions )
			{
				if ( pos == upLeft ) value += 1;
				if ( pos == up ) value += 2;
				if ( pos == upRight ) value += 4;
				if ( pos == left ) value += 8;
				if ( pos == right ) value += 16;
				if ( pos == downLeft ) value += 32;
				if ( pos == down ) value += 64;
				if ( pos == downRight ) value += 128;
			}

			return value;
		}

		public class AutotilePosition
		{
			public Vector2Int Position { get; set; }
			public bool ShouldMerge { get; set; } = false;

			public AutotilePosition ( Vector2Int position, bool shouldMerge = false )
			{
				Position = position;
				ShouldMerge = shouldMerge;
			}
		}
	}

	public class Tile
	{
		public Guid TileId { get; set; } = Guid.NewGuid();
		public Vector2Int CellPosition { get; set; }
		public bool HorizontalFlip { get; set; }
		public bool VerticalFlip { get; set; }
		public int Rotation { get; set; }
		public Vector2Int BakedPosition { get; set; }

		public Tile () { }

		public Tile ( Guid tileId, Vector2Int cellPosition, int rotation, bool flipX, bool flipY )
		{
			TileId = tileId;
			CellPosition = cellPosition;
			HorizontalFlip = flipX;
			VerticalFlip = flipY;
			Rotation = rotation;
		}

		public Tile Copy ()
		{
			return new Tile( TileId, CellPosition, Rotation, HorizontalFlip, VerticalFlip );
		}
	}

	public class ComponentControls { }

}

internal sealed class TilesetSceneObject : SceneCustomObject
{
	TilesetComponent Component;
	Dictionary<TilesetResource, (TileAtlas, Material)> Materials = new();
	Material MissingMaterial;
	int LayerIndex;

	public TilesetSceneObject ( TilesetComponent component, SceneWorld world, int layerIndex ) : base( world )
	{
		Component = component;
		LayerIndex = layerIndex;

		MissingMaterial = Material.Load( "materials/sprite_2d.vmat" ).CreateCopy();
		MissingMaterial.Set( "Texture", Texture.Load( "images/missing-tile.png" ) );
		Tags.SetFrom( Component.Tags );
	}

	public override void RenderSceneObject ()
	{
		if ( Component?.Layers is null ) return;
		var Layer = Component.Layers.ElementAtOrDefault( LayerIndex );
		if ( Layer is null )
		{
			return;
		}

		var layers = Component.Layers.ToList();
		layers.Reverse();
		if ( layers.Count == 0 ) return;

		Dictionary<Vector2Int, TilesetComponent.Tile> missingTiles = new();

		if ( Layer?.IsVisible != true ) return;

		int i = 0;
		int layerIndex = layers.IndexOf( Layer );

		{
			var tileset = Layer.TilesetResource;
			if ( tileset is null ) return;
			var tilemap = tileset.TileMap;

			var combo = GetMaterial( tileset );
			if ( combo.Item1 is null || combo.Item2 is null ) return;

			var tiling = combo.Item1.GetTiling();
			var totalTiles = Layer.Tiles.Where( x => x.Value.TileId == default || tilemap.ContainsKey( x.Value.TileId ) );
			var vertex = ArrayPool<Vertex>.Shared.Rent( totalTiles.Count() * 6 );

			var minPosition = new Vector3( int.MaxValue, int.MaxValue, int.MaxValue );
			var maxPosition = new Vector3( int.MinValue, int.MinValue, int.MinValue );

			foreach ( var tile in Layer.Tiles )
			{
				var pos = tile.Key;
				Vector2Int offsetPos = Vector2Int.Zero;
				if ( tile.Value.TileId == default ) offsetPos = tile.Value.BakedPosition;
				else
				{
					if ( !tilemap.ContainsKey( tile.Value.TileId ) )
					{
						missingTiles[pos] = tile.Value;
						continue;
					}
					offsetPos = tilemap[tile.Value.TileId].Position;
				}
				var offset = combo.Item1.GetOffset( offsetPos + tile.Value.CellPosition );
				if ( tile.Value.HorizontalFlip )
					offset.x = -offset.x - tiling.x;
				if ( !tile.Value.VerticalFlip )
					offset.y = -offset.y - tiling.y;


				var size = tileset.GetTileSize();
				var position = new Vector3( pos.x, pos.y, Layer.Height ?? ( Component.Layers.Count - Component.Layers.IndexOf( Layer ) ) ) * new Vector3( size.x, size.y, 1 );

				minPosition = Vector3.Min( minPosition, position );
				maxPosition = Vector3.Max( maxPosition, position );

				var topLeft = new Vector3( position.x, position.y, position.z );
				var topRight = new Vector3( position.x + size.x, position.y, position.z );
				var bottomRight = new Vector3( position.x + size.x, position.y + size.y, position.z );
				var bottomLeft = new Vector3( position.x, position.y + size.y, position.z );

				var uvTopLeft = new Vector2( offset.x, offset.y );
				var uvTopRight = new Vector2( offset.x + tiling.x, offset.y );
				var uvBottomRight = new Vector2( offset.x + tiling.x, offset.y + tiling.y );
				var uvBottomLeft = new Vector2( offset.x, offset.y + tiling.y );

				if ( tile.Value.Rotation == 90 )
				{
					var tempUv = uvTopLeft;
					uvTopLeft = uvBottomLeft;
					uvBottomLeft = uvBottomRight;
					uvBottomRight = uvTopRight;
					uvTopRight = tempUv;
				}
				else if ( tile.Value.Rotation == 180 )
				{
					var tempUv = uvTopLeft;
					uvTopLeft = uvBottomRight;
					uvBottomRight = tempUv;
					tempUv = uvTopRight;
					uvTopRight = uvBottomLeft;
					uvBottomLeft = tempUv;
				}
				else if ( tile.Value.Rotation == 270 )
				{
					var tempUv = uvTopLeft;
					uvTopLeft = uvTopRight;
					uvTopRight = uvBottomRight;
					uvBottomRight = uvBottomLeft;
					uvBottomLeft = tempUv;
				}

				vertex[i] = new Vertex( topLeft );
				vertex[i].TexCoord0 = uvTopLeft;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex( topRight );
				vertex[i].TexCoord0 = uvTopRight;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex( bottomRight );
				vertex[i].TexCoord0 = uvBottomRight;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex( topLeft );
				vertex[i].TexCoord0 = uvTopLeft;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex( bottomRight );
				vertex[i].TexCoord0 = uvBottomRight;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex( bottomLeft );
				vertex[i].TexCoord0 = uvBottomLeft;
				vertex[i].Normal = Vector3.Up;
				i++;
			}

			Graphics.Draw( vertex, totalTiles.Count() * 6, combo.Item2, Attributes );
			ArrayPool<Vertex>.Shared.Return( vertex );

			var siz = tileset.GetTileSize();
			maxPosition += new Vector3( siz.x, siz.y, 0 );
			Bounds = new BBox( minPosition, maxPosition + Vector3.Down * 0.01f ).Rotate( Rotation ).Translate( Position );


		}

		if ( missingTiles.Count > 0 )
		{
			var uvTopLeft = new Vector2( 0, 0 );
			var uvTopRight = new Vector2( 1, 0 );
			var uvBottomRight = new Vector2( 1, 1 );
			var uvBottomLeft = new Vector2( 0, 1 );

			foreach ( var tile in missingTiles )
			{
				var material = MissingMaterial;
				var pos = tile.Key;
				var size = Component.Layers[0].TilesetResource.TileSize;
				var position = new Vector3( pos.x, pos.y, 0 ) * new Vector3( size.x, size.y, 1 );

				var topLeft = new Vector3( position.x, position.y, position.z );
				var topRight = new Vector3( position.x + size.x, position.y, position.z );
				var bottomRight = new Vector3( position.x + size.x, position.y + size.y, position.z );
				var bottomLeft = new Vector3( position.x, position.y + size.y, position.z );

				var vertex = new Vertex[]
				{
				new Vertex(topLeft) { TexCoord0 = uvTopLeft, Normal = Vector3.Up },
				new Vertex(topRight) { TexCoord0 = uvTopRight, Normal = Vector3.Up },
				new Vertex(bottomRight) { TexCoord0 = uvBottomRight, Normal = Vector3.Up },
				new Vertex(topLeft) { TexCoord0 = uvTopLeft, Normal = Vector3.Up },
				new Vertex(bottomRight) { TexCoord0 = uvBottomRight, Normal = Vector3.Up },
				new Vertex(bottomLeft) { TexCoord0 = uvBottomLeft, Normal = Vector3.Up },
				};

				Graphics.Draw( vertex, 6, material, Attributes );
			}
		}
	}

	(TileAtlas, Material) GetMaterial ( TilesetResource resource )
	{
		var texture = TileAtlas.FromTileset( resource );

		if ( Materials.TryGetValue( resource, out var combo ) )
		{
			combo.Item1 = texture;
			combo.Item2.Set( "Texture", texture );
		}
		else
		{
			var material = Material.Load( "materials/sprite_2d.vmat" ).CreateCopy();
			material.Set( "Texture", texture );
			combo.Item1 = texture;
			combo.Item2 = material;
			Materials.Add( resource, combo );
		}

		return combo;
	}
}
