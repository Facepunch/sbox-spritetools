// using System.Collections.Generic;
// using System.Linq;
// using System.Net;
// using System.Text.Json;
// using System.Text.Json.Nodes;
// using Sandbox;

// namespace SpriteTools;

// public partial class TilesetComponent
// {
//     public override int ComponentVersion => 1;

//     [JsonUpgrader(typeof(TilesetComponent), 1)]
//     static void Upgrader_v1(JsonObject json)
//     {
//         Log.Info("we upgrading...");
//         if (json.ContainsKey("Layers"))
//         {
//             Log.Info("we have Layers...");
//             var layerList = json["Layers"].AsArray();
//             foreach (var layer in layerList)
//             {
//                 Log.Info("Checking a layer..");
//                 if (layer is null) continue;
//                 var layerObj = layer.AsObject();
//                 if (!layerObj.ContainsKey("_tiles") && layerObj.ContainsKey("Tiles"))
//                 {
//                     Log.Info("This layer is getting converted!");

//                     layerObj["_tiles"] = "[]";
//                     layerObj.Remove("Tiles");
//                 }
//             }
//         }
//         Log.Info("Jobs done!");
//     }
// }