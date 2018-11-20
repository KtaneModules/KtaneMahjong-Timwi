using System.Linq;
using KModkit;
using Mahjong;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Mahjong
/// Created by Timwi
/// </summary>
public class MahjongModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable[] Tiles;
    public Texture[] TileTextures;
    public Texture[] TileTexturesHighlighted;
    public MeshRenderer CountingTile;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private int[] _matchRow1;
    private int[] _matchRow2;
    private int[] _countingRow;

    private LayoutInfo _layout;
    private bool[] _taken;
    private int? _selectedTile;

    //[UnityEditor.MenuItem("A/B")]
    //public static void Menu()
    //{
    //    var m = FindObjectOfType<MahjongModule>();
    //}

    string tileName(int ix)
    {
        return TileTextures[ix].name.Replace(" normal", "");
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var rnd = RuleSeedable.GetRNG();
        var skip = rnd.Next(0, 100);
        for (var i = 0; i < skip; i++)
            rnd.NextDouble();

        var tilesIxs = Enumerable.Range(0, 42).ToList();
        rnd.ShuffleFisherYates(tilesIxs);
        _matchRow1 = tilesIxs.Take(14).ToArray();
        _matchRow2 = tilesIxs.Skip(14).Take(14).ToArray();
        _countingRow = tilesIxs.Skip(28).Take(14).ToArray();

        var sn = Bomb.GetSerialNumber().Select(ch => (ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 10) % 14).ToArray();
        for (int i = 0; i < 3; i++)
        {
            Debug.LogFormat(@"[Mahjong #{0}] Swapping {1} and {2}", _moduleId, tileName(_matchRow1[sn[2 * i]]), tileName(_matchRow2[sn[2 * i + 1]]));
            var t = _matchRow1[sn[2 * i]];
            _matchRow1[sn[2 * i]] = _matchRow2[sn[2 * i + 1]];
            _matchRow2[sn[2 * i + 1]] = t;
        }

        var offset = Rnd.Range(0, 14);
        CountingTile.material.mainTexture = TileTextures[_countingRow[offset]];
        Debug.LogFormat(@"[Mahjong #{0}] Counting tile is {1} ⇒ shift is {2} to the right", _moduleId, tileName(_countingRow[offset]), offset);

        // Decide on a layout
        var layoutRaw = @"///.1.1..1.1.1/..2....242/.1.1..1.1.1";

        // Find out which locations have a tile. Also place the tiles’ actual game objects in the right places (we’ll assign their textures later).
        _layout = LayoutInfo.Create(layoutRaw, _moduleId, Tiles);
        var solution = _layout.FindSolutionPath(_moduleId);
        _taken = new bool[_layout.Tiles.Length];

        Debug.LogFormat(@"[Mahjong #{0}] Solution:", _moduleId);
        var pairIxs = Enumerable.Range(0, 2 * solution.Count).ToArray().Shuffle();
        for (int i = 0; i < solution.Count; i++)
        {
            _layout.Tiles[solution[i].Ix1].MeshRenderer.material.mainTexture = TileTextures[_matchRow1[pairIxs[i]]];
            _layout.Tiles[solution[i].Ix2].MeshRenderer.material.mainTexture = TileTextures[_matchRow2[pairIxs[i]]];
            Debug.LogFormat(@"[Mahjong #{0}] — {1} and {2}", _moduleId, tileName(_matchRow1[pairIxs[i]]), tileName(_matchRow2[pairIxs[i]]));
        }

        for (int i = 0; i < Tiles.Length; i++)
            Tiles[i].OnInteract = clickTile(i);
    }

    private KMSelectable.OnInteractHandler clickTile(int i)
    {
        return delegate
        {
            return false;
        };
    }
}
