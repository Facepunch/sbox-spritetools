using Sandbox;
using Editor;
using System.Linq;
using System.Collections.Generic;

namespace SpriteTools;

internal static class SpriteResourceMenu
{
    static SpriteImportSettings ImportSettings;

    [Event("asset.contextmenu", Priority = 50)]
    public static void OnSpriteResourceAssetContext(AssetContextMenu e)
    {
        // Are all the files we have selected image assets?
        if (!e.SelectedList.All(x => x.AssetType == AssetType.ImageFile))
            return;

        e.Menu.AddOption($"Create 2D Sprite", "emoji_emotions", action: () => CreateSpriteResourceUsingImageFiles(e.SelectedList));

        if (e.SelectedList.Count > 1)
        {
            e.Menu.AddOption($"Create {e.SelectedList.Count} 2D Sprites", "emoji_emotions", action: () => CreateSpriteResourcesPopup(e.SelectedList));
        }
    }

    private static void CreateSpriteResourceUsingImageFiles(IEnumerable<Asset> assets)
    {
        var asset = assets.First();
        var assetName = asset.Name;

        var fd = new FileDialog(null);
        fd.Title = "Create Sprite Resource from Image Files..";
        fd.Directory = System.IO.Path.GetDirectoryName(asset.AbsolutePath);
        fd.DefaultSuffix = ".sprite";
        fd.SelectFile($"{assetName}.sprite");
        fd.SetFindFile();
        fd.SetModeSave();
        fd.SetNameFilter("2D Sprite (*.sprite)");

        if (!fd.Execute())
            return;

        var paths = assets.Select(x => System.IO.Path.ChangeExtension(x.Path, System.IO.Path.GetExtension(x.AbsolutePath)));
        asset = AssetSystem.CreateResource("sprite", fd.SelectedFile);
        var sprite = asset.LoadResource<SpriteResource>();
        var anim = sprite.Animations.FirstOrDefault();
        anim.Name = "default";
        anim.Frames ??= new();
        anim.Frames.Clear();
        foreach (var path in paths)
        {
            var frame = new SpriteAnimationFrame(path);
            anim.Frames.Add(frame);
        }
        anim.Looping = true;

        asset.SaveToDisk(sprite);
        MainAssetBrowser.Instance?.UpdateAssetList();
        MainAssetBrowser.Instance?.FocusOnAsset(asset);
        EditorUtility.InspectorObject = asset;
    }

    private static void CreateSpriteResourcesUsingImageFiles(IEnumerable<Asset> assets)
    {
        foreach (var asset in assets)
        {
            var newAsset = AssetSystem.CreateResource("sprite", System.IO.Path.ChangeExtension(asset.AbsolutePath, ".sprite"));
            var sprite = newAsset.LoadResource<SpriteResource>();
            var anim = sprite.Animations.FirstOrDefault();
            anim.Name = ImportSettings.AnimationName;
            anim.Origin = ImportSettings.Origin;
            anim.Frames ??= new();
            anim.Frames.Clear();
            var frame = new SpriteAnimationFrame(System.IO.Path.ChangeExtension(asset.Path, System.IO.Path.GetExtension(asset.AbsolutePath)));
            anim.Frames.Add(frame);
            anim.Looping = true;

            newAsset.SaveToDisk(sprite);
        }

        MainAssetBrowser.Instance?.UpdateAssetList();
    }

    private static void CreateSpriteResourcesPopup(IEnumerable<Asset> assets)
    {
        ImportSettings = new();

        var popup = new Dialog();

        popup.Window.MinimumWidth = 500;
        popup.Window.Height = 200;
        popup.WindowTitle = "Create Sprite Resources from Image Files...";
        popup.Window.SetWindowIcon("info");
        popup.Window.SetModal(true, true);

        popup.Layout = Layout.Column();
        popup.Layout.Margin = 20;
        popup.Layout.Spacing = 20;

        popup.Layout.Add(new Label("Set import settings for each sprite", popup));

        var controlSheet = new ControlSheet();
        controlSheet.AddProperty(ImportSettings, x => x.AnimationName);
        controlSheet.AddProperty(ImportSettings, x => x.Origin);
        popup.Layout.Add(controlSheet);
        popup.Layout.AddStretchCell();

        var okButton = new Button.Primary("Import", popup);
        okButton.Icon = "download";
        okButton.MinimumWidth = 64;
        okButton.MouseLeftPress += () =>
        {
            CreateSpriteResourcesUsingImageFiles(assets);
            popup.Close();
        };


        var cancelButton = new Button("Cancel", popup);
        cancelButton.MinimumWidth = 64;
        cancelButton.MouseLeftPress += () => popup.Close();

        var buttonLayout = Layout.Row();
        buttonLayout.Spacing = 5;
        popup.Layout.Add(buttonLayout);
        buttonLayout.AddStretchCell();
        buttonLayout.Add(okButton);
        buttonLayout.Add(cancelButton);

        popup.Show();
    }

    public class SpriteImportSettings
    {
        public string AnimationName { get; set; } = "default";
        public Vector2 Origin { get; set; } = Vector2.One * 0.5f;
    }
}