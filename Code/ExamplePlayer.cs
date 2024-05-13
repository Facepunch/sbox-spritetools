using System;
using Sandbox;
using SpriteTools;

public sealed class ExamplePlayer : Component
{
	[RequireComponent] CharacterController Controller { get; set; }

	[Property] SpriteComponent Sprite { get; set; }
	[Property] GameObject Body { get; set; }
	[Property] public float Speed { get; set; } = 100f;
	[Property] public float Friction { get; set; } = 10f;

	Rotation TargetRotation { get; set; }
	Vector3 WishVelocity { get; set; }

	protected override void OnStart()
	{
		TargetRotation = Rotation.FromYaw(0f);

		Sprite.OnBroadcastEvent += OnEvent;
	}

	protected override void OnDestroy()
	{
		Sprite.OnBroadcastEvent -= OnEvent;
	}

	protected override void OnFixedUpdate()
	{
		float friction = 1f - MathF.Pow(0.5f, Friction * Time.Delta);
		Vector3 input = 0;
		var rot = Scene.Camera.Transform.Rotation;
		if (Input.Down("Forward")) input += rot.Forward;
		if (Input.Down("Backward")) input += rot.Backward;
		if (Input.Down("Left"))
		{
			input += rot.Left;
			TargetRotation = Rotation.FromYaw(180f + Random.Shared.Float(-0.1f, 0.1f));
		}
		if (Input.Down("Right"))
		{
			input += rot.Right;
			TargetRotation = Rotation.FromYaw(Random.Shared.Float(-0.1f, 0.1f));
		}
		input = input.Normal.WithZ(0f);
		input *= Speed;
		WishVelocity = WishVelocity.LerpTo(input, friction);

		Controller.MoveTo(Transform.Position + WishVelocity * Time.Delta, true);

		float rotLerp = 1f - MathF.Pow(0.5f, 15f * Time.Delta);
		Transform.Rotation = Rotation.Slerp(Transform.Rotation, TargetRotation, rotLerp);

		if (input.Length > 5f)
		{
			Sprite.PlayAnimation("run");
		}
		else
		{
			Sprite.PlayAnimation("idle");
		}

		var idleScale = new Vector3(MathF.Sin(Time.Now * 2f) * 0.05f + 1f, MathF.Cos(Time.Now * 2f) * 0.05f + 1f, 1f);
		Body.Transform.LocalScale = Body.Transform.LocalScale.LerpTo(idleScale, 1f - MathF.Pow(0.5f, 10f * Time.Delta));
	}

	void OnEvent(string eventName)
	{
		switch (eventName)
		{
			case "step":
				Sound.Play("impact-melee-grass");
				Body.Transform.LocalScale = new Vector3(0.8f, 1.2f, 0.9f);
				break;
			case "bob":
				Sound.Play("ui.button.press");
				break;
		}
	}
}
