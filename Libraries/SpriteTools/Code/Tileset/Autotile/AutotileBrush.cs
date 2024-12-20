using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Sandbox;

namespace SpriteTools;

public class AutotileBrush
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Property, Placeholder("Autotile Brush")] public string Name { get; set; }
    public AutotileType AutotileType { get; set; } = AutotileType.Bitmask2x2Edge;
    [Hide] public Tile[] Tiles { get; set; }
    [Hide] public int TileCount => (AutotileType == AutotileType.Bitmask3x3Complete) ? 255 : (AutotileType == AutotileType.Bitmask3x3 ? 47 : 15);
    [Hide, JsonIgnore] public TilesetResource Tileset { get; set; }

    public AutotileBrush() : this(AutotileType.Bitmask2x2Edge) { }

    public AutotileBrush(AutotileType type)
    {
        AutotileType = type;

        var tileCount = TileCount;
        Tiles = new Tile[tileCount];
        for (int i = 0; i < tileCount; i++)
        {
            Tiles[i] = new Tile();
        }
    }

    /// <summary>
    /// Set the autotile type for this brush. This will reset all existing tiles in the brush.
    /// </summary>
    /// <param name="autotileType"></param>
    public void SetAutotileType(AutotileType autotileType)
    {
        AutotileType = autotileType;

        var tileCount = TileCount;
        Tiles = new Tile[tileCount];
        for (int i = 0; i < tileCount; i++)
        {
            Tiles[i] = new Tile();
        }
    }

    public TileReference GetTileFromBitmask(int bitmask)
    {
        if (bitmask < 0)
            return null;

        Tile selectedTile = null;

        switch (AutotileType)
        {
            case AutotileType.Bitmask2x2Edge:
                selectedTile = GetTile2x2Edge(bitmask);
                break;
            // case AutotileType.Bitmask2x2Corner:
            //     selectedTile = GetTile2x2Corner(bitmask);
            //     break;
            case AutotileType.Bitmask3x3:
                selectedTile = GetTile3x3Minimal(bitmask);
                break;
            case AutotileType.Bitmask3x3Complete:
                selectedTile = GetTile3x3Complete(bitmask);
                break;
        }

        if (selectedTile is null) return null;
        if ((selectedTile.Tiles?.Count ?? 0) == 0) return null;

        // TODO: Random weights
        int randomIndex = Random.Shared.Int(0, selectedTile.Tiles.Count - 1);
        return selectedTile.Tiles[randomIndex];
    }

    Tile GetTile2x2Edge(int bitmask)
    {
        // return Tiles[0];
        switch (bitmask)
        {
            // Top-Left Corner
            case 208:
            case 209:
            case 211:
            case 212:
            case 213:
            case 217:
            case 221:
            case 240:
            case 241:
            case 243:
            case 244:
            case 245:
                return Tiles[7];
            // Top-Right Corner
            case 104:
            case 105:
            case 108:
            case 109:
            case 110:
            case 124:
            case 125:
            case 232:
            case 233:
            case 236:
            case 237:
            case 238:
                return Tiles[11];
            // Bottom-Left Corner
            case 22:
            case 23:
            case 54:
            case 55:
            case 62:
            case 118:
            case 119:
            case 150:
            case 151:
            case 182:
            case 183:
            case 190:
                return Tiles[13];
            // Bottom-Right Corner
            case 11:
            case 15:
            case 43:
            case 47:
            case 139:
            case 143:
            case 155:
            case 171:
            case 175:
            case 187:
            case 203:
            case 207:
                return Tiles[14];
            // Top
            case 248:
            case 249:
            case 252:
            case 253:
                return Tiles[3];
            // Down
            case 31:
            case 63:
            case 159:
            case 191:
                return Tiles[12];
            // Left
            case 214:
            case 215:
            case 246:
            case 247:
                return Tiles[5];
            // Right
            case 107:
            case 111:
            case 235:
            case 239:
                return Tiles[10];
            // Inner Top-Left
            case 254:
                return Tiles[1];
            // Inner Top-Right
            case 251:
                return Tiles[2];
            // Inner Bottom-Left
            case 223:
                return Tiles[4];
            // Inner Bottom-Right
            case 127:
                return Tiles[8];
            // Inner Diagonal (Top-Left and Bottom-Right)
            case 126:
                return Tiles[9];
            // Inner Diagonal (Top-Right and Bottom-Left)
            case 216:
            case 219:
                return Tiles[6];
            // Inside
            case 255:
                return Tiles[0];
            default: return Tiles[0];
        }
    }

    Tile GetTile2x2Corner(int bitmask)
    {
        switch (bitmask)
        {
            case 0: return Tiles[0];
            case 1: return Tiles[1];
            case 2: return Tiles[2];
            case 3: return Tiles[3];
            case 4: return Tiles[4];
            case 5: return Tiles[5];
            case 6: return Tiles[6];
            case 7: return Tiles[7];
            case 8: return Tiles[8];
            case 9: return Tiles[9];
            case 10: return Tiles[10];
            case 11: return Tiles[11];
            case 12: return Tiles[12];
            case 13: return Tiles[13];
            case 14: return Tiles[14];
            case 15: return Tiles[15];
            default: return null;
        }
    }

    Tile GetTile3x3Minimal(int bitmask)
    {
        switch (bitmask)
        {
            case 0:
            case 1: return Tiles[46];
            case 2: return Tiles[44];
            case 3: return Tiles[44];
            case 4: return Tiles[46];
            case 5: return Tiles[46];
            case 6: return Tiles[44];
            case 7: return Tiles[44];
            case 8: return Tiles[45];
            case 9: return Tiles[45];
            case 10: return Tiles[39];
            case 11: return Tiles[38];
            case 12: return Tiles[45];
            case 13: return Tiles[45];
            case 14: return Tiles[39];
            case 15: return Tiles[38];
            case 16: return Tiles[43];
            case 17: return Tiles[43];
            case 18: return Tiles[41];
            case 19: return Tiles[41];
            case 20: return Tiles[43];
            case 21: return Tiles[43];
            case 22: return Tiles[40];
            case 23: return Tiles[40];
            case 24: return Tiles[33];
            case 25: return Tiles[33];
            case 26: return Tiles[31];
            case 27: return Tiles[30];
            case 28: return Tiles[33];
            case 29: return Tiles[33];
            case 30: return Tiles[29];
            case 31: return Tiles[28];
            case 32: return Tiles[46];
            case 33: return Tiles[46];
            case 34: return Tiles[44];
            case 35: return Tiles[44];
            case 36: return Tiles[46];
            case 37: return Tiles[46];
            case 38: return Tiles[44];
            case 39: return Tiles[44];
            case 40: return Tiles[45];
            case 41: return Tiles[45];
            case 42: return Tiles[39];
            case 43: return Tiles[38];
            case 44: return Tiles[45];
            case 45: return Tiles[45];
            case 46: return Tiles[39];
            case 47: return Tiles[38];
            case 48: return Tiles[43];
            case 49: return Tiles[43];
            case 50: return Tiles[41];
            case 51: return Tiles[41];
            case 52: return Tiles[43];
            case 53: return Tiles[43];
            case 54: return Tiles[40];
            case 55: return Tiles[40];
            case 56: return Tiles[33];
            case 57: return Tiles[33];
            case 58: return Tiles[31];
            case 59: return Tiles[30];
            case 60: return Tiles[33];
            case 61: return Tiles[33];
            case 62: return Tiles[29];
            case 63: return Tiles[28];
            case 64: return Tiles[42];
            case 65: return Tiles[42];
            case 66: return Tiles[32];
            case 67: return Tiles[32];
            case 68: return Tiles[42];
            case 69: return Tiles[42];
            case 70: return Tiles[32];
            case 71: return Tiles[32];
            case 72: return Tiles[37];
            case 73: return Tiles[37];
            case 74: return Tiles[27];
            case 75: return Tiles[25];
            case 76: return Tiles[37];
            case 77: return Tiles[37];
            case 78: return Tiles[27];
            case 79: return Tiles[25];
            case 80: return Tiles[35];
            case 81: return Tiles[35];
            case 82: return Tiles[19];
            case 83: return Tiles[19];
            case 84: return Tiles[35];
            case 85: return Tiles[35];
            case 86: return Tiles[18];
            case 87: return Tiles[18];
            case 88: return Tiles[23];
            case 89: return Tiles[23];
            case 90: return Tiles[15];
            case 91: return Tiles[14];
            case 92: return Tiles[23];
            case 93: return Tiles[23];
            case 94: return Tiles[13];
            case 95: return Tiles[12];
            case 96: return Tiles[42];
            case 97: return Tiles[42];
            case 98: return Tiles[32];
            case 99: return Tiles[32];
            case 100: return Tiles[42];
            case 101: return Tiles[42];
            case 102: return Tiles[32];
            case 103: return Tiles[32];
            case 104: return Tiles[36];
            case 105: return Tiles[36];
            case 106: return Tiles[26];
            case 107: return Tiles[24];
            case 108: return Tiles[36];
            case 109: return Tiles[36];
            case 110: return Tiles[26];
            case 111: return Tiles[24];
            case 112: return Tiles[35];
            case 113: return Tiles[35];
            case 114: return Tiles[19];
            case 115: return Tiles[19];
            case 116: return Tiles[35];
            case 117: return Tiles[35];
            case 118: return Tiles[18];
            case 119: return Tiles[18];
            case 120: return Tiles[21];
            case 121: return Tiles[21];
            case 122: return Tiles[7];
            case 123: return Tiles[6];
            case 124: return Tiles[21];
            case 125: return Tiles[21];
            case 126: return Tiles[5];
            case 127: return Tiles[4];
            case 128: return Tiles[46];
            case 129: return Tiles[46];
            case 130: return Tiles[44];
            case 131: return Tiles[44];
            case 132: return Tiles[46];
            case 133: return Tiles[46];
            case 134: return Tiles[44];
            case 135: return Tiles[44];
            case 136: return Tiles[45];
            case 137: return Tiles[45];
            case 138: return Tiles[39];
            case 139: return Tiles[38];
            case 140: return Tiles[45];
            case 141: return Tiles[45];
            case 142: return Tiles[39];
            case 143: return Tiles[38];
            case 144: return Tiles[43];
            case 145: return Tiles[43];
            case 146: return Tiles[41];
            case 147: return Tiles[41];
            case 148: return Tiles[43];
            case 149: return Tiles[43];
            case 150: return Tiles[40];
            case 151: return Tiles[40];
            case 152: return Tiles[33];
            case 153: return Tiles[33];
            case 154: return Tiles[31];
            case 155: return Tiles[30];
            case 156: return Tiles[33];
            case 157: return Tiles[33];
            case 158: return Tiles[29];
            case 159: return Tiles[28];
            case 160: return Tiles[46];
            case 161: return Tiles[46];
            case 162: return Tiles[44];
            case 163: return Tiles[44];
            case 164: return Tiles[46];
            case 165: return Tiles[46];
            case 166: return Tiles[44];
            case 167: return Tiles[44];
            case 168: return Tiles[45];
            case 169: return Tiles[45];
            case 170: return Tiles[39];
            case 171: return Tiles[38];
            case 172: return Tiles[45];
            case 173: return Tiles[45];
            case 174: return Tiles[39];
            case 175: return Tiles[38];
            case 176: return Tiles[43];
            case 177: return Tiles[43];
            case 178: return Tiles[41];
            case 179: return Tiles[41];
            case 180: return Tiles[43];
            case 181: return Tiles[43];
            case 182: return Tiles[40];
            case 183: return Tiles[40];
            case 184: return Tiles[33];
            case 185: return Tiles[33];
            case 186: return Tiles[31];
            case 187: return Tiles[30];
            case 188: return Tiles[33];
            case 189: return Tiles[33];
            case 190: return Tiles[29];
            case 191: return Tiles[28];
            case 192: return Tiles[42];
            case 193: return Tiles[42];
            case 194: return Tiles[32];
            case 195: return Tiles[32];
            case 196: return Tiles[42];
            case 197: return Tiles[42];
            case 198: return Tiles[32];
            case 199: return Tiles[32];
            case 200: return Tiles[37];
            case 201: return Tiles[37];
            case 202: return Tiles[27];
            case 203: return Tiles[25];
            case 204: return Tiles[37];
            case 205: return Tiles[37];
            case 206: return Tiles[27];
            case 207: return Tiles[25];
            case 208: return Tiles[34];
            case 209: return Tiles[34];
            case 210: return Tiles[17];
            case 211: return Tiles[17];
            case 212: return Tiles[34];
            case 213: return Tiles[34];
            case 214: return Tiles[16];
            case 215: return Tiles[16];
            case 216: return Tiles[22];
            case 217: return Tiles[22];
            case 218: return Tiles[11];
            case 219: return Tiles[10];
            case 220: return Tiles[22];
            case 221: return Tiles[22];
            case 222: return Tiles[9];
            case 223: return Tiles[8];
            case 224: return Tiles[42];
            case 225: return Tiles[42];
            case 226: return Tiles[32];
            case 227: return Tiles[32];
            case 228: return Tiles[42];
            case 229: return Tiles[42];
            case 230: return Tiles[32];
            case 231: return Tiles[32];
            case 232: return Tiles[36];
            case 233: return Tiles[36];
            case 234: return Tiles[26];
            case 235: return Tiles[24];
            case 236: return Tiles[36];
            case 237: return Tiles[36];
            case 238: return Tiles[26];
            case 239: return Tiles[24];
            case 240: return Tiles[34];
            case 241: return Tiles[34];
            case 242: return Tiles[17];
            case 243: return Tiles[17];
            case 244: return Tiles[34];
            case 245: return Tiles[34];
            case 246: return Tiles[16];
            case 247: return Tiles[16];
            case 248: return Tiles[20];
            case 249: return Tiles[20];
            case 250: return Tiles[3];
            case 251: return Tiles[2];
            case 252: return Tiles[20];
            case 253: return Tiles[20];
            case 254: return Tiles[1];
            case 255: return Tiles[0];
            default: return null;
        }
    }

    Tile GetTile3x3Complete(int bitmask)
    {
        if (bitmask < 0 || bitmask >= Tiles.Length)
            return null;
        return Tiles[bitmask];
    }

    public override int GetHashCode()
    {
        int val = 0;

        foreach (var tile in Tiles)
        {
            if (tile?.Tiles is null) continue;
            foreach (var tileRef in tile.Tiles)
            {
                if (tileRef is null) continue;
                val += HashCode.Combine(tileRef.GetTilePosition());
            }
        }

        return HashCode.Combine(Name, AutotileType, val);
    }

    public class Tile
    {
        // [InlineEditor, WideMode(HasLabel = false)]
        [Property] public List<TileReference> Tiles { get; set; }
    }
    public class TileReference
    {
        [Hide] public TilesetResource Tileset { get; set; }
        public Guid Id { get; set; }
        public Vector2Int Position { get; set; }
        public float Weight { get; set; } = 10f;

        public Vector2Int GetTilePosition()
        {
            if (Tileset is null) return -1;
            foreach (var tile in Tileset.Tiles)
            {
                if (tile is null) continue;
                if (tile.Id == Id) return tile.Position;
            }
            return -1;
        }

        public TileReference()
        {
            Id = Guid.NewGuid();
        }

        public TileReference(Guid guid)
        {
            Id = guid;
        }
    }

}