using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public ParticleSystem Smoke1;
    public ParticleSystem Smoke2;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private int[] _matchRow1;
    private int[] _matchRow2;
    private int[] _countingRow;

    private LayoutInfo _layout;
    private bool[] _taken;
    private int? _selectedTile;

    string tileName(int ix)
    {
        return TileTextures[ix].name.Replace(" normal", "");
    }

    string tileShortName(int ix)
    {
        var name = tileName(ix);
        return name.Contains(' ') ? name.Where(ch => (ch < 'a' || ch > 'z') && ch != ' ').JoinString() : name.Substring(0, 2);
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[Mahjong #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);

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

        Debug.LogFormat(@"[Mahjong #{0}] After swaps, match rows are:", _moduleId);
        Debug.LogFormat(@"[Mahjong #{0}] Row 1: {1}", _moduleId, _matchRow1.Select(tileShortName).JoinString(" "));
        Debug.LogFormat(@"[Mahjong #{0}] Row 2: {1}", _moduleId, _matchRow2.Select(tileShortName).JoinString(" "));

        var offset = Rnd.Range(0, 14);
        CountingTile.material.mainTexture = TileTextures[_countingRow[offset]];
        Debug.LogFormat(@"[Mahjong #{0}] Counting tile is {1} ⇒ shift is {2} to the right", _moduleId, tileName(_countingRow[offset]), offset);
        _matchRow2 = _matchRow2.Skip(14 - offset).Concat(_matchRow2.Take(14 - offset)).ToArray();

        Debug.LogFormat(@"[Mahjong #{0}] After shift, match rows are:", _moduleId);
        Debug.LogFormat(@"[Mahjong #{0}] Row 1: {1}", _moduleId, _matchRow1.Select(tileShortName).JoinString(" "));
        Debug.LogFormat(@"[Mahjong #{0}] Row 2: {1}", _moduleId, _matchRow2.Select(tileShortName).JoinString(" "));

        // Decide on a layout
        var layouts = new[]
        {
            @"/.1.1/..2/.141..1.1.1/..2....242/.1.1..1.1.1",
            @"/..1.1/...2.2/..1.5.4/...2...2/....4.5.1/.....2.2/......1.1",
            @"//.....3/.1.1...1.1/..2.252.2/.1.1...1.1/.....3",
            @"/.1212121//.3.....3//.1212121",
            @"....1.1//....1.1/..2.2.2.2/....1.1/..2.2.2.2/....1.1//....1.1",
            @"/..1.1/1..2.2/..1.1/...2..1/....12.2/......1.1/......2.2/........1.1",
            @"/.....121/...12...21//..3.3//...12...21/.....121",
            @"//.121212121//....121//....121//....121",
            @"....3//...3.3//..3...3...3//.......3.3//........3",
            @"/...1.1.1/.1...2...1/.....1/.1.1...1.1/.....1/.1...2...1/...1.1.1",
            @"///1.1.1.1.1.1/.2.2.6.2.2/1.1.1.1.1.1",
            @"///.7.4.4.4.7//.7.4.4.4.7",
            @"//...7.5.7//...4...4//...7.5.7",
            @"/1/2.1/1.2.1/..1.2.1/....1.2.1/......1.2.1/........1.2/..........1",
            @"/.3.3//.3.3/....4.4/.......3.3//.......3.3",
            @"..1/....1/1.1/...2/.1..4R4..1/.......2/........1.1/......1/........1",
            @".1.1/.2.2/1.5.1/.2.2/.1.1.3/.......2/......1.1/..........1",
            @"///...7.7.7//...7.7.7",
            @"/1/..1.1.1.1/1.........1/..1.1.1.1/1.........1/..1.1.1.1/00........1",
            @"/.1.1.1.1/..2.2.2/..14141/...2.2/...1.1/....2/....1",
            @"....1//......12/........1/3.3.3.3.2.1/........1/......12//00..1",
            @"...1.1.1//...1.1.1/.1/...1.3.1/.........1/...1.1.1//00.1.1.1",
            @".....1/.....2/....141/....2.2/...14.41/....2.2/....141/00...2/00...1",
            @"//.....1/...12421/ 124...421/...12421/.....1",
            @"//...1.1.1/..2.2.2.2/...5.4.1/..2.2.2.2/...1.1.1"
        };
        var layoutRaw = layouts[Rnd.Range(0, layouts.Length)];

        // Find out which locations have a tile. Also place the tiles’ actual game objects in the right places (we’ll assign their textures later).
        _layout = LayoutInfo.Create(layoutRaw, _moduleId, Tiles);
        Debug.LogFormat(@"<Mahjong #{0}> Chosen layout has {1} tiles.", _moduleId, _layout.Tiles.Length);
        for (int i = _layout.Tiles.Length; i < Tiles.Length; i++)
            Tiles[i].gameObject.SetActive(false);

        var solution = _layout.FindSolutionPath(_moduleId);
        _taken = new bool[_layout.Tiles.Length];

        Debug.LogFormat(@"[Mahjong #{0}] Possible solution:", _moduleId);
        var pairIxs = Enumerable.Range(0, solution.Count).ToArray().Shuffle();
        for (int i = 0; i < solution.Count; i++)
        {
            _layout.Tiles[solution[i].Ix1].SetTextures(TileTextures[_matchRow1[pairIxs[i]]], TileTexturesHighlighted[_matchRow1[pairIxs[i]]]);
            _layout.Tiles[solution[i].Ix1].PairedWith = solution[i].Ix2;

            _layout.Tiles[solution[i].Ix2].SetTextures(TileTextures[_matchRow2[pairIxs[i]]], TileTexturesHighlighted[_matchRow2[pairIxs[i]]]);
            _layout.Tiles[solution[i].Ix2].PairedWith = solution[i].Ix1;

            Debug.LogFormat(@"[Mahjong #{0}] — {1} and {2}", _moduleId, tileName(_matchRow1[pairIxs[i]]), tileName(_matchRow2[pairIxs[i]]));
        }

        for (int i = 0; i < Tiles.Length; i++)
            Tiles[i].OnInteract = clickTile(i);
    }

    private KMSelectable.OnInteractHandler clickTile(int i)
    {
        return delegate
        {
            if (_selectedTile == i)
            {
                Audio.PlaySoundAtTransform("Selection", _layout.Tiles[i].Transform);
                _layout.Tiles[i].SetNormal();
                _selectedTile = null;
            }
            else if (!_layout.IsTileAvailable(i, _taken))
            {
                Debug.LogFormat(@"[Mahjong #{0}] You received a strike because you selected a tile ({1}) that was not available.", _moduleId, _layout.Tiles[i].Name);
                Module.HandleStrike();
            }
            else if (_selectedTile == null)
            {
                Audio.PlaySoundAtTransform("Selection", _layout.Tiles[i].Transform);
                _layout.Tiles[i].SetHighlighted();
                _selectedTile = i;
            }
            else if (i == _layout.Tiles[_selectedTile.Value].PairedWith)
            {
                // Valid pair! Eliminate
                Audio.PlaySoundAtTransform("Elimination", _layout.Tiles[i].Transform);
                Smoke1.transform.localPosition = _layout.Tiles[i].Transform.localPosition;
                Smoke1.Play();
                Smoke2.transform.localPosition = _layout.Tiles[_selectedTile.Value].Transform.localPosition;
                Smoke2.Play();
                Debug.LogFormat(@"[Mahjong #{0}] {1} and {2} correctly eliminated.", _moduleId, _layout.Tiles[_selectedTile.Value].Name, _layout.Tiles[i].Name);
                _layout.Tiles[i].GameObject.SetActive(false);
                _layout.Tiles[_selectedTile.Value].GameObject.SetActive(false);
                _taken[i] = true;
                _taken[_selectedTile.Value] = true;
                _selectedTile = null;

                if (_taken.All(t => t))
                {
                    Debug.LogFormat(@"[Mahjong #{0}] Module passed.", _moduleId);
                    Module.HandlePass();
                }
            }
            else
            {
                // Invalid pair. Strike.
                Debug.LogFormat(@"[Mahjong #{0}] {1} and {2} are not a valid pair. Strike.", _moduleId, _layout.Tiles[_selectedTile.Value].Name, _layout.Tiles[i].Name);
                Module.HandleStrike();
                _layout.Tiles[_selectedTile.Value].SetNormal();
                _selectedTile = null;
            }
            return false;
        };
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} W1,B7,C4,RD,O,N [Wheel 1, Bamboo 7, Character 4, Red Dragon, Orchid, North] | Gotta write out “South”/“Summer”/“Spring” and “West”/“Winter” in full if more than one of those tiles is present on the module.";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        var pieces = command.ToLowerInvariant().Split(new[] { ',', ';', '+' });
        var list = new List<KMSelectable>();
        foreach (var piece in pieces)
        {
            var pieceName = piece.Trim();
            var matches = Enumerable.Range(0, _layout.Tiles.Length)
                .Where(i => !_taken[i] && (
                    _layout.Tiles[i].Name.Equals(pieceName, StringComparison.InvariantCultureIgnoreCase) ||
                    _layout.Tiles[i].ShortName.Equals(pieceName, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();
            if (matches.Length > 1)
            {
                yield return string.Format("sendtochaterror The name “{0}” matches multiple tiles: {1}", pieceName, matches.Select(ix => _layout.Tiles[ix].Name).JoinString(", "));
                yield break;
            }
            if (matches.Length == 0)
            {
                yield return string.Format("sendtochaterror The name “{0}” does not match any tile.", pieceName);
                yield break;
            }
            list.Add(_layout.Tiles[matches[0]].KMSelectable);
        }
        yield return null;
        var even = false;
        foreach (var elem in list)
        {
            yield return new[] { elem };
            yield return new WaitForSeconds(even ? .5f : .2f);
            even = !even;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (_selectedTile != null)
        {
            Tiles[_selectedTile.Value].OnInteract();
            yield return new WaitForSeconds(.1f);
        }

        while (!_taken.All(t => t))
        {
            // Find a valid pair to eliminate
            for (var i = 0; i < _layout.Tiles.Length; i++)
            {
                if (!_taken[i] && _layout.IsTileAvailable(i, _taken) && _layout.IsTileAvailable(_layout.Tiles[i].PairedWith, _taken))
                {
                    Tiles[i].OnInteract();
                    yield return new WaitForSeconds(.1f);
                    Tiles[_layout.Tiles[i].PairedWith].OnInteract();
                    yield return new WaitForSeconds(.1f);
                    while (Smoke1.isPlaying || Smoke2.isPlaying)
                        yield return true;
                    break;
                }
            }
        }
    }
}
