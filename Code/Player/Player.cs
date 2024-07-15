using System;
using Sandbox;
using SpriteTools;

namespace Quest;

public sealed class Player : Component
{
	public static Player Local { get; private set; }

	[RequireComponent] Rigidbody Rigidbody { get; set; }

	[Property, Group("References")] SpriteComponent Sprite { get; set; }

	[Property, Group("Variables")] float Gravity { get; set; } = 5f;
	[Property, Group("Variables")] float Friction { get; set; } = 6f;
	[Property, Group("Variables")] float Acceleration { get; set; } = 14f;
	[Property, Group("Variables")] float MaxSpeed { get; set; } = 320f;
	[Property, Group("Variables")] float JumpForce { get; set; } = 200f;
	[Property, Group("Variables")] Vector2 HitboxSize { get; set; } = new Vector2(0.5f, 0.7f);

	public bool IsGrounded { get; private set; }
	public Vector2 WishVelocity;

	Vector2 Velocity = Vector2.Zero;
	BBox Bounds
	{
		get
		{
			var bbox = Sprite.Bounds;
			var mins = bbox.Mins;
			var maxs = bbox.Maxs;
			var size = new Vector3(HitboxSize.y, HitboxSize.x, 1);
			mins *= size;
			maxs *= size;
			return new BBox(mins, maxs).Rotate(Sprite.Transform.LocalRotation);
		}
	}

	protected override void OnAwake()
	{
		Local = this;
	}

	protected override void OnUpdate()
	{
		if (Input.Pressed("Jump"))
		{
			Jump();
		}
		else if (Input.Released("Jump"))
		{
			ReleaseJump();
		}

		UpdateAnimations();
		UpdateCamera();

		var targetScale = GetTargetScale();
		Sprite.Transform.LocalScale = new Vector3(
			MathX.Lerp(Sprite.Transform.LocalScale.x, targetScale.x, Time.Delta * 10f),
			MathX.Lerp(Sprite.Transform.LocalScale.y, targetScale.y, Time.Delta * 10f),
			1
		);
	}

	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		ApplyHalfGravity();
		ApplyMovement();
		Move();
		ApplyHalfGravity();
	}

	void BuildWishVelocity()
	{
		WishVelocity = Vector3.Zero;

		if (Input.Down("Right")) WishVelocity += Vector2.Right;
		if (Input.Down("Left")) WishVelocity += Vector2.Left;

		if (WishVelocity.x != 0)
		{
			Sprite.SpriteFlags = WishVelocity.x > 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		}
	}

	void ApplyHalfGravity()
	{
		Velocity.y += Gravity * 0.5f * Time.Delta;
	}

	void ApplyMovement()
	{
		if (WishVelocity.x > 0 && Velocity.x > -MaxSpeed * Time.Delta)
		{
			Velocity.x -= Acceleration * Time.Delta;
		}
		else if (WishVelocity.x < 0 && Velocity.x < MaxSpeed * Time.Delta)
		{
			Velocity.x += Acceleration * Time.Delta;
		}
		else if (WishVelocity.x == 0)
		{
			// Slow Down
			if (MathF.Abs(Velocity.x) < Friction * Time.Delta)
			{
				Velocity.x = 0;
			}
			else
			{
				Velocity.x -= MathF.Sign(Velocity.x) * Friction * Time.Delta;
			}
		}
	}

	void Move()
	{
		var xVel = new Vector3(Velocity.x, 0, 0);
		var yVel = new Vector3(0, 0, Velocity.y);

		// Horizontal Collision Check
		{
			var tr = Scene.Trace.Box(Bounds, Transform.Local.Position, Transform.Local.Position + xVel)
				.WithoutTags("player", "trigger")
				.Run();

			if (tr.Hit)
			{
				Transform.Position = Transform.Position.WithX(tr.EndPosition.x);
				WishVelocity.x = 0;
			}
			else
			{
				Transform.LocalPosition += xVel;
			}
		}


		// Vertical Collision Check
		{
			var tr = Scene.Trace.Box(Bounds, Transform.Local.Position, Transform.Local.Position + yVel)
				.WithoutTags("player", "trigger")
				.Run();

			if (tr.Hit)
			{
				bool wasGrounded = IsGrounded;
				if (Velocity.y < 0)
				{
					IsGrounded = true;
					if (!wasGrounded)
					{
						Land();
					}
				}

				Transform.Position = Transform.Position.WithZ(tr.EndPosition.z);
				Velocity.y = 0;
			}
			else
			{
				Transform.LocalPosition += yVel;
				IsGrounded = false;
			}
		}
	}

	void Jump()
	{
		if (!IsGrounded) return;

		Velocity.y = JumpForce;
	}

	void ReleaseJump()
	{
		if (Velocity.y > 0)
		{
			Velocity.y /= 2f;
		}
	}

	void Land()
	{
		Sprite.Transform.LocalScale = new Vector3(0.5f, 1.5f, 1f);
	}

	Vector3 GetTargetScale()
	{
		var scale = Vector3.One;

		return scale;
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (Sprite is null)
			return;

		using (Gizmo.Scope("Player"))
		{
			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.LineBBox(Bounds);
		}
	}

	void UpdateAnimations()
	{
		if (Velocity.y < Gravity * Time.Delta * 2f) Sprite.PlayAnimation("fall");
		else if (Velocity.y > -Gravity * Time.Delta) Sprite.PlayAnimation("jump");
		else if (Velocity.x != 0) Sprite.PlayAnimation("run");
		else Sprite.PlayAnimation("idle");
	}

	void UpdateCamera()
	{
		var camPos = Transform.Position;

		camPos += Vector3.Up * 120f;
		camPos = camPos.WithY(Scene.Camera.Transform.Position.y);

		Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo(camPos, 10 * Time.Delta);
	}

}
























/*

using System;
using Sandbox;
using SpriteTools;

namespace Quest;

public sealed class Player : Component
{
	public static Player Local { get; private set; }

	[Property, Group("References")] SpriteComponent Sprite { get; set; }

	[Property, Group("Variables")] float Gravity { get; set; } = 5f;
	[Property, Group("Variables")] float Friction { get; set; } = 6f;
	[Property, Group("Variables")] float Acceleration { get; set; } = 14f;
	[Property, Group("Variables")] float MaxSpeed { get; set; } = 320f;
	[Property, Group("Variables")] float JumpForce { get; set; } = 200f;
	[Property, Group("Variables")] Vector2 HitboxSize { get; set; } = new Vector2(0.5f, 0.7f);

	public Vector2 Velocity;
	public Vector2 WishVelocity;

	BBox Bounds
	{
		get
		{
			var bbox = Sprite.Bounds;
			var mins = bbox.Mins;
			var maxs = bbox.Maxs;
			var size = new Vector3(HitboxSize.y, HitboxSize.x, 1);
			mins *= size;
			maxs *= size;
			return new BBox(mins, maxs).Rotate(Sprite.Transform.LocalRotation);
		}
	}

	protected override void OnAwake()
	{
		Local = this;
	}

	protected override void OnUpdate()
	{
		if (Input.Pressed("jump"))
		{

		}
	}

	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		ApplyHalfGravity();
		ApplyMovement();
		Move();
		ApplyHalfGravity();
	}

	void BuildWishVelocity()
	{
		WishVelocity = Vector3.Zero;

		if (Input.Down("Right")) WishVelocity += Vector2.Right;
		if (Input.Down("Left")) WishVelocity += Vector2.Left;
	}

	void ApplyHalfGravity()
	{
		Velocity.y += Gravity * 0.5f * Time.Delta;
	}

	void ApplyMovement()
	{
		if (WishVelocity.x > 0 && Velocity.x < MaxSpeed)
		{
			WishVelocity.x += Friction * Time.Delta;
		}
		else if (WishVelocity.x < 0 && Velocity.x > -MaxSpeed)
		{
			WishVelocity.x -= Friction * Time.Delta;
		}
		else if (WishVelocity.x == 0)
		{
			// Slow Down
			if (MathF.Abs(Velocity.x) < Friction * Time.Delta)
			{
				Velocity.x = 0;
			}
			else
			{
				Velocity.x -= MathF.Sign(Velocity.x) * Friction * Time.Delta;
			}
		}
	}

	void Move()
	{
		var xVel = new Vector3(Velocity.x, 0, 0) * Time.Delta;
		var yVel = new Vector3(0, 0, Velocity.y) * Time.Delta;

		// Horizontal Collision Check
		{
			var tr = Scene.Trace.Box(Bounds, Transform.Local.Position, Transform.Local.Position + xVel)
				.WithoutTags("player", "trigger")
				.Run();
			if (tr.Hit)
			{
				Transform.Position = Transform.Position.WithX(tr.EndPosition.x);
				WishVelocity.x = 0;
			}
		}

		// Vertical Collision Check
		{
			var tr = Scene.Trace.Box(Bounds, Transform.Local.Position + xVel, Transform.Local.Position + xVel + yVel)
				.WithoutTags("player", "trigger")
				.Run();

			if (tr.Hit)
			{
				Transform.Position = Transform.Position.WithY(tr.EndPosition.y);
				Velocity.y = 0;
			}
		}

		Transform.Position += new Vector3(Velocity.x, 0, Velocity.y) * Time.Delta;
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (Sprite is null)
			return;

		using (Gizmo.Scope("Player"))
		{
			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.LineBBox(Bounds);
		}
	}


}

*/