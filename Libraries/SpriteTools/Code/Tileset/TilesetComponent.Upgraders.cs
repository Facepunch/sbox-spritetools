using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sandbox;

namespace SpriteTools;

public partial class TilesetComponent
{
    public override int ComponentVersion => 1;

    [JsonUpgrader(typeof(TilesetComponent), 1)]
    static void Upgrader_v1(JsonObject json)
    {
        Log.Info("we upgrading...");

        if (json.ContainsKey("LayerDistance"))
        {
            var distance = json["LayerDistance"].GetValue<float>();
            Log.Info("Distance found!");

            if (json.ContainsKey("Layers"))
            {
                Log.Info("we have Layers...");
                var layerList = json["Layers"].AsArray();
                for (int i = 0; i < layerList.Count; i++)
                {
                    Log.Info("Checking a layer..");
                    var layer = layerList[i];
                    if (layer is null) continue;
                    var layerObj = layer.AsObject();
                    layerObj["Height"] = distance * i;
                }
            }
            Log.Info("Jobs done!");
        }
    }
}