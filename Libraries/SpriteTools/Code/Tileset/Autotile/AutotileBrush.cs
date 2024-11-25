using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sandbox;

namespace SpriteTools;

public class AutotileBrush
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Is47Tiles { get; set; } = false;
    [Property, Placeholder("Autotile Brush")] public string Name { get; set; }
    public Tile[] Tiles { get; set; }

    public AutotileBrush() : this(false) { }

    public AutotileBrush(bool is47Tiles = false)
    {
        Is47Tiles = is47Tiles;

        var tileCount = is47Tiles ? 47 : 16;
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

        // tesat infibfdiusbfdsi

        if (Is47Tiles)
        {
            switch (bitmask)
            {
                case 0:
                case 1: selectedTile = Tiles[46]; break;
                case 2: selectedTile = Tiles[44]; break;
                case 3: selectedTile = Tiles[44]; break;
                case 4: selectedTile = Tiles[46]; break;
                case 5: selectedTile = Tiles[46]; break;
                case 6: selectedTile = Tiles[44]; break;
                case 7: selectedTile = Tiles[44]; break;
                case 8: selectedTile = Tiles[45]; break;
                case 9: selectedTile = Tiles[45]; break;
                case 10: selectedTile = Tiles[39]; break;
                case 11: selectedTile = Tiles[38]; break;
                case 12: selectedTile = Tiles[45]; break;
                case 13: selectedTile = Tiles[45]; break;
                case 14: selectedTile = Tiles[39]; break;
                case 15: selectedTile = Tiles[38]; break;
                case 16: selectedTile = Tiles[43]; break;
                case 17: selectedTile = Tiles[43]; break;
                case 18: selectedTile = Tiles[41]; break;
                case 19: selectedTile = Tiles[41]; break;
                case 20: selectedTile = Tiles[43]; break;
                case 21: selectedTile = Tiles[43]; break;
                case 22: selectedTile = Tiles[40]; break;
                case 23: selectedTile = Tiles[40]; break;
                case 24: selectedTile = Tiles[33]; break;
                case 25: selectedTile = Tiles[33]; break;
                case 26: selectedTile = Tiles[31]; break;
                case 27: selectedTile = Tiles[30]; break;
                case 28: selectedTile = Tiles[33]; break;
                case 29: selectedTile = Tiles[33]; break;
                case 30: selectedTile = Tiles[29]; break;
                case 31: selectedTile = Tiles[28]; break;
                case 32: selectedTile = Tiles[46]; break;
                case 33: selectedTile = Tiles[46]; break;
                case 34: selectedTile = Tiles[44]; break;
                case 35: selectedTile = Tiles[44]; break;
                case 36: selectedTile = Tiles[46]; break;
                case 37: selectedTile = Tiles[46]; break;
                case 38: selectedTile = Tiles[44]; break;
                case 39: selectedTile = Tiles[44]; break;
                case 40: selectedTile = Tiles[45]; break;
                case 41: selectedTile = Tiles[45]; break;
                case 42: selectedTile = Tiles[39]; break;
                case 43: selectedTile = Tiles[38]; break;
                case 44: selectedTile = Tiles[45]; break;
                case 45: selectedTile = Tiles[45]; break;
                case 46: selectedTile = Tiles[39]; break;
                case 47: selectedTile = Tiles[38]; break;
                case 48: selectedTile = Tiles[43]; break;
                case 49: selectedTile = Tiles[43]; break;
                case 50: selectedTile = Tiles[41]; break;
                case 51: selectedTile = Tiles[41]; break;
                case 52: selectedTile = Tiles[43]; break;
                case 53: selectedTile = Tiles[43]; break;
                case 54: selectedTile = Tiles[40]; break;
                case 55: selectedTile = Tiles[40]; break;
                case 56: selectedTile = Tiles[33]; break;
                case 57: selectedTile = Tiles[33]; break;
                case 58: selectedTile = Tiles[31]; break;
                case 59: selectedTile = Tiles[30]; break;
                case 60: selectedTile = Tiles[33]; break;
                case 61: selectedTile = Tiles[33]; break;
                case 62: selectedTile = Tiles[29]; break;
                case 63: selectedTile = Tiles[28]; break;
                case 64: selectedTile = Tiles[42]; break;
                case 65: selectedTile = Tiles[42]; break;
                case 66: selectedTile = Tiles[32]; break;
                case 67: selectedTile = Tiles[32]; break;
                case 68: selectedTile = Tiles[42]; break;
                case 69: selectedTile = Tiles[42]; break;
                case 70: selectedTile = Tiles[32]; break;
                case 71: selectedTile = Tiles[32]; break;
                case 72: selectedTile = Tiles[37]; break;
                case 73: selectedTile = Tiles[37]; break;
                case 74: selectedTile = Tiles[27]; break;
                case 75: selectedTile = Tiles[25]; break;
                case 76: selectedTile = Tiles[37]; break;
                case 77: selectedTile = Tiles[37]; break;
                case 78: selectedTile = Tiles[27]; break;
                case 79: selectedTile = Tiles[25]; break;
                case 80: selectedTile = Tiles[35]; break;
                case 81: selectedTile = Tiles[35]; break;
                case 82: selectedTile = Tiles[19]; break;
                case 83: selectedTile = Tiles[19]; break;
                case 84: selectedTile = Tiles[35]; break;
                case 85: selectedTile = Tiles[35]; break;
                case 86: selectedTile = Tiles[18]; break;
                case 87: selectedTile = Tiles[18]; break;
                case 88: selectedTile = Tiles[23]; break;
                case 89: selectedTile = Tiles[23]; break;
                case 90: selectedTile = Tiles[15]; break;
                case 91: selectedTile = Tiles[14]; break;
                case 92: selectedTile = Tiles[23]; break;
                case 93: selectedTile = Tiles[23]; break;
                case 94: selectedTile = Tiles[13]; break;
                case 95: selectedTile = Tiles[12]; break;
                case 96: selectedTile = Tiles[42]; break;
                case 97: selectedTile = Tiles[42]; break;
                case 98: selectedTile = Tiles[32]; break;
                case 99: selectedTile = Tiles[32]; break;
                case 100: selectedTile = Tiles[42]; break;
                case 101: selectedTile = Tiles[42]; break;
                case 102: selectedTile = Tiles[32]; break;
                case 103: selectedTile = Tiles[32]; break;
                case 104: selectedTile = Tiles[36]; break;
                case 105: selectedTile = Tiles[36]; break;
                case 106: selectedTile = Tiles[26]; break;
                case 107: selectedTile = Tiles[24]; break;
                case 108: selectedTile = Tiles[36]; break;
                case 109: selectedTile = Tiles[36]; break;
                case 110: selectedTile = Tiles[26]; break;
                case 111: selectedTile = Tiles[24]; break;
                case 112: selectedTile = Tiles[35]; break;
                case 113: selectedTile = Tiles[35]; break;
                case 114: selectedTile = Tiles[19]; break;
                case 115: selectedTile = Tiles[19]; break;
                case 116: selectedTile = Tiles[35]; break;
                case 117: selectedTile = Tiles[35]; break;
                case 118: selectedTile = Tiles[18]; break;
                case 119: selectedTile = Tiles[18]; break;
                case 120: selectedTile = Tiles[21]; break;
                case 121: selectedTile = Tiles[21]; break;
                case 122: selectedTile = Tiles[7]; break;
                case 123: selectedTile = Tiles[6]; break;
                case 124: selectedTile = Tiles[21]; break;
                case 125: selectedTile = Tiles[21]; break;
                case 126: selectedTile = Tiles[5]; break;
                case 127: selectedTile = Tiles[4]; break;
                case 128: selectedTile = Tiles[46]; break;
                case 129: selectedTile = Tiles[46]; break;
                case 130: selectedTile = Tiles[44]; break;
                case 131: selectedTile = Tiles[44]; break;
                case 132: selectedTile = Tiles[46]; break;
                case 133: selectedTile = Tiles[46]; break;
                case 134: selectedTile = Tiles[44]; break;
                case 135: selectedTile = Tiles[44]; break;
                case 136: selectedTile = Tiles[45]; break;
                case 137: selectedTile = Tiles[45]; break;
                case 138: selectedTile = Tiles[39]; break;
                case 139: selectedTile = Tiles[38]; break;
                case 140: selectedTile = Tiles[45]; break;
                case 141: selectedTile = Tiles[45]; break;
                case 142: selectedTile = Tiles[39]; break;
                case 143: selectedTile = Tiles[38]; break;
                case 144: selectedTile = Tiles[43]; break;
                case 145: selectedTile = Tiles[43]; break;
                case 146: selectedTile = Tiles[41]; break;
                case 147: selectedTile = Tiles[41]; break;
                case 148: selectedTile = Tiles[43]; break;
                case 149: selectedTile = Tiles[43]; break;
                case 150: selectedTile = Tiles[40]; break;
                case 151: selectedTile = Tiles[40]; break;
                case 152: selectedTile = Tiles[33]; break;
                case 153: selectedTile = Tiles[33]; break;
                case 154: selectedTile = Tiles[31]; break;
                case 155: selectedTile = Tiles[30]; break;
                case 156: selectedTile = Tiles[33]; break;
                case 157: selectedTile = Tiles[33]; break;
                case 158: selectedTile = Tiles[29]; break;
                case 159: selectedTile = Tiles[28]; break;
                case 160: selectedTile = Tiles[46]; break;
                case 161: selectedTile = Tiles[46]; break;
                case 162: selectedTile = Tiles[44]; break;
                case 163: selectedTile = Tiles[44]; break;
                case 164: selectedTile = Tiles[46]; break;
                case 165: selectedTile = Tiles[46]; break;
                case 166: selectedTile = Tiles[44]; break;
                case 167: selectedTile = Tiles[44]; break;
                case 168: selectedTile = Tiles[45]; break;
                case 169: selectedTile = Tiles[45]; break;
                case 170: selectedTile = Tiles[39]; break;
                case 171: selectedTile = Tiles[38]; break;
                case 172: selectedTile = Tiles[45]; break;
                case 173: selectedTile = Tiles[45]; break;
                case 174: selectedTile = Tiles[39]; break;
                case 175: selectedTile = Tiles[38]; break;
                case 176: selectedTile = Tiles[43]; break;
                case 177: selectedTile = Tiles[43]; break;
                case 178: selectedTile = Tiles[41]; break;
                case 179: selectedTile = Tiles[41]; break;
                case 180: selectedTile = Tiles[43]; break;
                case 181: selectedTile = Tiles[43]; break;
                case 182: selectedTile = Tiles[40]; break;
                case 183: selectedTile = Tiles[40]; break;
                case 184: selectedTile = Tiles[33]; break;
                case 185: selectedTile = Tiles[33]; break;
                case 186: selectedTile = Tiles[31]; break;
                case 187: selectedTile = Tiles[30]; break;
                case 188: selectedTile = Tiles[33]; break;
                case 189: selectedTile = Tiles[33]; break;
                case 190: selectedTile = Tiles[29]; break;
                case 191: selectedTile = Tiles[28]; break;
                case 192: selectedTile = Tiles[42]; break;
                case 193: selectedTile = Tiles[42]; break;
                case 194: selectedTile = Tiles[32]; break;
                case 195: selectedTile = Tiles[32]; break;
                case 196: selectedTile = Tiles[42]; break;
                case 197: selectedTile = Tiles[42]; break;
                case 198: selectedTile = Tiles[32]; break;
                case 199: selectedTile = Tiles[32]; break;
                case 200: selectedTile = Tiles[37]; break;
                case 201: selectedTile = Tiles[37]; break;
                case 202: selectedTile = Tiles[27]; break;
                case 203: selectedTile = Tiles[25]; break;
                case 204: selectedTile = Tiles[37]; break;
                case 205: selectedTile = Tiles[37]; break;
                case 206: selectedTile = Tiles[27]; break;
                case 207: selectedTile = Tiles[25]; break;
                case 208: selectedTile = Tiles[34]; break;
                case 209: selectedTile = Tiles[34]; break;
                case 210: selectedTile = Tiles[17]; break;
                case 211: selectedTile = Tiles[17]; break;
                case 212: selectedTile = Tiles[34]; break;
                case 213: selectedTile = Tiles[34]; break;
                case 214: selectedTile = Tiles[16]; break;
                case 215: selectedTile = Tiles[16]; break;
                case 216: selectedTile = Tiles[22]; break;
                case 217: selectedTile = Tiles[22]; break;
                case 218: selectedTile = Tiles[11]; break;
                case 219: selectedTile = Tiles[10]; break;
                case 220: selectedTile = Tiles[22]; break;
                case 221: selectedTile = Tiles[22]; break;
                case 222: selectedTile = Tiles[9]; break;
                case 223: selectedTile = Tiles[8]; break;
                case 224: selectedTile = Tiles[42]; break;
                case 225: selectedTile = Tiles[42]; break;
                case 226: selectedTile = Tiles[32]; break;
                case 227: selectedTile = Tiles[32]; break;
                case 228: selectedTile = Tiles[42]; break;
                case 229: selectedTile = Tiles[42]; break;
                case 230: selectedTile = Tiles[32]; break;
                case 231: selectedTile = Tiles[32]; break;
                case 232: selectedTile = Tiles[36]; break;
                case 233: selectedTile = Tiles[36]; break;
                case 234: selectedTile = Tiles[26]; break;
                case 235: selectedTile = Tiles[24]; break;
                case 236: selectedTile = Tiles[36]; break;
                case 237: selectedTile = Tiles[36]; break;
                case 238: selectedTile = Tiles[26]; break;
                case 239: selectedTile = Tiles[24]; break;
                case 240: selectedTile = Tiles[34]; break;
                case 241: selectedTile = Tiles[34]; break;
                case 242: selectedTile = Tiles[17]; break;
                case 243: selectedTile = Tiles[17]; break;
                case 244: selectedTile = Tiles[34]; break;
                case 245: selectedTile = Tiles[34]; break;
                case 246: selectedTile = Tiles[16]; break;
                case 247: selectedTile = Tiles[16]; break;
                case 248: selectedTile = Tiles[20]; break;
                case 249: selectedTile = Tiles[20]; break;
                case 250: selectedTile = Tiles[3]; break;
                case 251: selectedTile = Tiles[2]; break;
                case 252: selectedTile = Tiles[20]; break;
                case 253: selectedTile = Tiles[20]; break;
                case 254: selectedTile = Tiles[1]; break;
                case 255: selectedTile = Tiles[0]; break;

            }
        }

        if (selectedTile is null) return null;

        int randomIndex = Random.Shared.Int(0, selectedTile.Tiles.Count - 1);
        return selectedTile.Tiles[randomIndex];
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