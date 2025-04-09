using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sandbox;

namespace SpriteTools;

public partial class SpriteResource
{
    public override int ResourceVersion => 1;

    [JsonUpgrader(typeof(SpriteResource), 1)]
    static void Upgrader_v1(JsonObject json)
    {
        if (json.ContainsKey("Looping"))
        {
            var wasLooping = json["Looping"].GetValue<bool>();
            json["LoopMode"] = (int)(wasLooping ? SpriteResource.LoopMode.Forward : SpriteResource.LoopMode.None);
        }
    }
}