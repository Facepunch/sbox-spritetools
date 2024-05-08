// using Sandbox;

// namespace SpriteTools;

// public sealed class SpriteComponent : Component, Component.ExecuteInEditor
// {
// 	[Property]
// 	public Model BaseModel
// 	{
// 		get => _baseModel;
// 		set
// 		{
// 			_baseModel = value;
// 			SceneObject?.Delete();
// 			SceneObject = null;
// 		}
// 	}
// 	Model _baseModel { get; set; } = Model.Load( "models/quad.vmdl" );
// 	[Property] public SpriteResource Sprite { get; set; }
// 	Material SpriteMaterial { get; set; }
// 	internal SceneObject SceneObject { get; set; }

// 	private int currentFrameIndex = 0;
// 	private RealTimeSince timeSinceLastFrame = 0;
// 	public string CurrentAnimationName;
// 	private bool isLooping;

// 	[Property] private float FrameRateOverride { get; set; } = 0;

// 	public bool IsFlipped { get; set; }

// 	[Property] public string DefaultAnimation { get; set; } = "idle";

// 	protected override void OnStart()
// 	{
// 		base.OnStart();

// 		PlayAnimation( DefaultAnimation, true );
// 	}

// 	protected override void OnUpdate()
// 	{
// 		if ( SceneObject == null )
// 		{
// 			SceneObject = new SceneObject( Scene.SceneWorld, BaseModel )
// 			{
// 				Flags = { IsTranslucent = true }
// 			};
// 		}

// 		//if ( Sprite == null || Sprite.Animations.Count == 0 ) return;

// 		UpdateSceneObject();
// 	}

// 	private void UpdateSceneObject()
// 	{
// 		if ( SpriteMaterial == null )
// 		{
// 			SpriteMaterial = Material.Create( "spritegraph", "materials/sprites/spritegraph" );
// 			SceneObject.SetMaterialOverride( SpriteMaterial );
// 		}

// 		var animation = Sprite.Animations.Find( a => a.AnimationName == CurrentAnimationName );

// 		//if ( animation == null ) return;

// 		var frameRate = FrameRateOverride > 0 ? FrameRateOverride : animation.Animation.FrameRate;

// 		if ( timeSinceLastFrame > 1.0f / frameRate )
// 		{
// 			currentFrameIndex++;
// 			if ( currentFrameIndex >= animation.Animation.AtlasPath.Count )
// 			{
// 				if ( isLooping )
// 				{
// 					currentFrameIndex = 0;
// 				}
// 				else
// 				{
// 					currentFrameIndex = animation.Animation.AtlasPath.Count - 1; // Stop on the last frame
// 				}
// 			}
// 			timeSinceLastFrame = 0;
// 		}

// 		var texture = Texture.Load( FileSystem.Mounted, animation.Animation.AtlasPath[currentFrameIndex] );
// 		SpriteMaterial.Set( "SpriteColor", texture );
// 		SceneObject.Attributes.Set( "SpriteColor", texture );
// 		SceneObject.Attributes.Set( "SpriteFlipped", IsFlipped );


// 		SceneObject.Transform = new Transform( Transform.Position, Transform.Rotation, Transform.Scale );
// 	}

// 	public void PlayAnimation( string animationName, bool loop )
// 	{
// 		if ( CurrentAnimationName == animationName ) return;
// 		CurrentAnimationName = animationName;
// 		isLooping = loop;
// 		currentFrameIndex = 0; // Restart the animation
// 	}

// 	protected override void OnDestroy()
// 	{
// 		base.OnDestroy();
// 		SceneObject?.Delete();
// 		SceneObject = null;
// 	}
// }

// public sealed class StaticSpriteComponent : Component, Component.ExecuteInEditor
// {
// 	[Property] public AnimatedSpriteResource animatedSpriteResource { get; set; }
// 	[Property] public bool IsSelfIlluminated { get; set; }
// 	Material SpriteMaterial { get; set; }
// 	internal SceneObject SceneObject { get; set; }

// 	private int currentFrameIndex = 0;
// 	private RealTimeSince timeSinceLastFrame = 0;
// 	private string currentAnimationName;
// 	private bool isLooping;

// 	public bool IsFlipped { get; set; }

// 	[Property] public string DefaultAnimation { get; set; } = "idle";

// 	protected override void OnStart()
// 	{
// 		base.OnStart();

// 		PlayAnimation( DefaultAnimation, true );
// 	}

// 	protected override void OnUpdate()
// 	{
// 		if ( SceneObject == null )
// 		{
// 			SceneObject = new SceneObject( Scene.SceneWorld, Model.Load( "models/quad.vmdl" ) )
// 			{
// 				Flags = { IsTranslucent = true }
// 			};
// 		}

// 		if ( animatedSpriteResource == null ) return;

// 		UpdateSceneObject();
// 	}

// 	private void UpdateSceneObject()
// 	{
// 		if ( SpriteMaterial == null )
// 		{
// 			SpriteMaterial = Material.Create( "spritegraph", "materials/sprites/spritegraph" );
// 			SceneObject.SetMaterialOverride( SpriteMaterial );
// 		}

// 		var animation = animatedSpriteResource;

// 		if ( animation.AtlasPath.Count == 1 )
// 		{
// 			var singletexture = Texture.Load( FileSystem.Mounted, animation.AtlasPath[0] );
// 			SpriteMaterial.Set( "SpriteColor", singletexture );
// 			SceneObject.Attributes.Set( "SpriteColor", singletexture );
// 			SceneObject.Transform = new Transform( Transform.Position, Transform.Rotation, Transform.Scale );
// 			return;
// 		}

// 		//if ( animation == null ) return;

// 		if ( timeSinceLastFrame > 1.0f / animation.FrameRate )
// 		{
// 			currentFrameIndex++;
// 			if ( currentFrameIndex >= animation.AtlasPath.Count )
// 			{
// 				if ( isLooping )
// 				{
// 					currentFrameIndex = 0;
// 				}
// 				else
// 				{
// 					currentFrameIndex = animation.AtlasPath.Count - 1; // Stop on the last frame
// 				}
// 			}
// 			timeSinceLastFrame = 0;
// 		}

// 		var texture = Texture.Load( FileSystem.Mounted, animation.AtlasPath[currentFrameIndex] );
// 		SpriteMaterial.Set( "SpriteColor", texture );
// 		SpriteMaterial.Set( "SelfIllum", IsSelfIlluminated );
// 		SceneObject.Attributes.Set( "SpriteColor", texture );
// 		SceneObject.Attributes.Set( "SpriteFlipped", IsFlipped );
// 		SceneObject.Attributes.Set( "SelfIllum", IsSelfIlluminated );

// 		SceneObject.Transform = new Transform( Transform.Position, Transform.Rotation, Transform.Scale );
// 	}

// 	public void PlayAnimation( string animationName, bool loop )
// 	{
// 		if ( currentAnimationName == animationName ) return;
// 		currentAnimationName = animationName;
// 		isLooping = loop;
// 		currentFrameIndex = 0; // Restart the animation
// 	}

// 	protected override void OnDestroy()
// 	{
// 		base.OnDestroy();
// 		SceneObject?.Delete();
// 		SceneObject = null;
// 	}
// }
