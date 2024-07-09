using System;
using System.Collections.Generic;
using Sandbox;
using Editor;
using Editor.Assets;
using System.Threading.Tasks;
using System.Linq;

using static Editor.Inspectors.AssetInspector;

namespace SpriteTools;

[CanEdit("asset:sprite")]
public class SpriteInspector : Widget, IAssetInspector
{
	private SpriteResource Sprite;
	private AssetPreview AssetPreview;

	private readonly ExpandGroup AnimationGroup;
	private readonly AnimationList Animations;

	public SpriteInspector(Widget parent) : base(parent)
	{
		Layout = Layout.Column();
		Layout.Margin = 4;
		Layout.Spacing = 4;

		AnimationGroup = new ExpandGroup(this);
		AnimationGroup.StateCookieName = $"{nameof(SpriteInspector)}.{nameof(AnimationGroup)}";
		AnimationGroup.Icon = "directions_run";
		AnimationGroup.Title = $"Animations";
		AnimationGroup.Visible = false;
		Layout.Add(AnimationGroup);

		Animations = new AnimationList(AnimationGroup);
		Animations.ItemSelected = PlayAnimation;
		AnimationGroup.SetWidget(Animations);
	}

	private void PlayAnimation(string name)
	{
		if (AssetPreview is null)
			return;

		if (AssetPreview is PreviewSprite preview)
		{
			preview.SetAnimation(name);
		}
	}

	public void SetAssetPreview(AssetPreview preview)
	{
		AssetPreview = preview;
	}

	public void SetAsset(Asset asset)
	{
		Sprite = asset.LoadResource<SpriteResource>();
		if (Sprite == null) return;

		if (Sprite.Animations.Count > 0)
		{
			AnimationGroup.Visible = true;
			Animations.SetSprite(Sprite);
		}

		AnimationGroup.Update();
	}

	private class AnimationList : ItemList
	{
		public override string ItemName => "Animation";
		public override string ItemIcon => "animgraph_editor/single_frame_icon.png";

		public AnimationList(Widget parent) : base(parent)
		{

		}

		public override void SetSprite(SpriteResource sprite)
		{
			Items = Enumerable.Range(0, sprite.Animations.Count)
				.Select(x => sprite.Animations[x].Name)
				.OrderBy(x => x)
				.ToList();

			ListView.SetItems(Items);
		}
	}

	private abstract class ItemList : Widget
	{
		protected readonly ListView ListView;
		protected List<string> Items;

		public abstract string ItemName { get; }
		public abstract string ItemIcon { get; }

		public Action<string> ItemSelected { get; set; }

		public abstract void SetSprite(SpriteResource model);

		public ItemList(Widget parent) : base(parent)
		{
			Layout = Layout.Column();
			Layout.Margin = 4;
			Layout.Spacing = 4;

			ListView = new ListView(this)
			{
				ItemSize = new Vector2(0, 25),
				Margin = new(4, 4, 16, 4),
				ItemPaint = PaintAnimationItem,
				ItemContextMenu = ShowItemContext,
				ToggleSelect = true,
				ItemSelected = (o) => ItemSelected?.Invoke(o as string),
				ItemDeselected = (o) => ItemSelected?.Invoke(null),
			};

			var filter = new LineEdit(this)
			{
				PlaceholderText = $"Filter {ItemName}s..",
				FixedHeight = 25
			};

			filter.TextEdited += (t) =>
			{
				ListView.SetItems(Items == null || Items.Count == 0 ? null : string.IsNullOrWhiteSpace(t) ? Items :
					Items.Where(x => x.Contains(t, StringComparison.OrdinalIgnoreCase)));
			};

			Layout.Add(filter);
			Layout.Add(ListView, 1);
		}

		private void ShowItemContext(object obj)
		{
			if (obj is not string name) return;

			var m = new Menu();

			m.AddOption("Copy", "content_copy", () =>
			{
				EditorUtility.Clipboard.Copy(name);
			});

			m.OpenAt(Editor.Application.CursorPosition);
		}

		private void PaintAnimationItem(VirtualWidget v)
		{
			if (v.Object is not string name)
				return;

			var rect = v.Rect;

			Paint.Antialiasing = true;

			var fg = Theme.White.Darken(0.2f);

			if (Paint.HasSelected)
			{
				fg = Theme.White;
				Paint.ClearPen();
				Paint.SetBrush(Theme.Primary.WithAlpha(0.5f));
				Paint.DrawRect(rect, 2);
				Paint.SetBrush(Theme.Primary.WithAlpha(0.4f));
			}
			else if (Paint.HasMouseOver)
			{
				Paint.ClearPen();
				Paint.SetBrush(Theme.Primary.WithAlpha(0.25f));
				Paint.DrawRect(rect, 2);
			}

			var iconRect = rect.Shrink(8, 4);
			iconRect.Width = iconRect.Height;
			Paint.Draw(iconRect, ItemIcon);

			var textRect = rect.Shrink(4);
			textRect.Left = iconRect.Right + 8;

			Paint.SetDefaultFont();
			Paint.SetPen(fg);
			Paint.DrawText(textRect, $"{name}", TextFlag.LeftCenter);
		}
	}

}
