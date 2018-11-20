using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Mahjong
{
    sealed class LayoutInfo
    {
        public string Syntax { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public TileInfo[] Tiles { get; private set; }

        public LayoutInfo(string syntax, int w, int h, TileInfo[] tiles)
        {
            Syntax = syntax;
            Width = w;
            Height = h;
            Tiles = tiles;
        }

        const float xMult = .0165f;
        const float yMult = .0205f;
        const float vertMult = .0145f;
        const float xOffset = 0f;
        const float yOffset = 0f;
        const float vertOffset = 0f;

        public static LayoutInfo Create(string syntax, int moduleId, KMSelectable[] tileSelectables)
        {
            var tileInfos = new List<TileInfo>();

            // Determine the size of the layout
            var layoutRows = syntax.Split('/');
            var h = layoutRows.Length;
            var w = layoutRows.Max(r => r.Length);
            var tx = 0;
            for (int y = 0; y < layoutRows.Length; y++)
                for (int x = 0; x < layoutRows[y].Length; x++)
                {
                    var bits = layoutRows[y][x] == '.' ? 0 : int.Parse(layoutRows[y][x].ToString(), NumberStyles.HexNumber);
                    for (int b = 0; bits > 0; b++)
                        if ((bits & (1 << b)) != 0)
                        {
                            var tl = new TileInfo(x + w * (y + h * b), tileSelectables[tx]);
                            tl.Transform.localPosition = new Vector3(xMult * x + xOffset, vertMult * b + vertOffset, -yMult * y - yOffset);
                            tileInfos.Add(tl);
                            bits -= 1 << b;
                            tx++;
                        }
                }

            if (tx % 2 == 1)
                Debug.LogFormat(@"[Mahjong #{0}] Please report this bug to Timwi: Layout has an odd number of tiles.", moduleId);

            return new LayoutInfo(syntax, w, h, tileInfos.ToArray());
        }

        public List<TilePair> FindSolutionPath(int moduleId)
        {
            var solution = findSolutionPath(new bool[Tiles.Length]);
            if (solution == null)
                Debug.LogFormat(@"[Mahjong #{0}] Please report this bug to Timwi: Layout has no possible solution.", moduleId);
            solution.Reverse();
            return solution;
        }

        private List<TilePair> findSolutionPath(bool[] taken)
        {
            if (taken.All(t => t))
                return new List<TilePair>();

            var permissibleTileIxs = Enumerable.Range(0, Tiles.Length).Where(tix => !taken[tix] && IsTileAvailable(tix, taken)).ToArray().Shuffle();

            // Yield all pairs of permissible tiles
            for (int i = 0; i < permissibleTileIxs.Length; i++)
                for (int j = i + 1; j < permissibleTileIxs.Length; j++)
                {
                    var newTaken = taken.ToArray();
                    newTaken[permissibleTileIxs[i]] = true;
                    newTaken[permissibleTileIxs[j]] = true;
                    var sol = findSolutionPath(newTaken);
                    if (sol != null)
                    {
                        sol.Add(new TilePair(permissibleTileIxs[i], permissibleTileIxs[j]));
                        return sol;
                    }
                }

            return null;
        }

        public bool IsTileAvailable(int tileIndex, bool[] taken)
        {
            var tx = Tiles[tileIndex].Location % Width;
            var ty = (Tiles[tileIndex].Location / Width) % Height;
            var tz = Tiles[tileIndex].Location / Width / Height;

            // Make sure that no tile is on top of it
            if (Enumerable.Range(tx - 1, 3).Any(x => x >= 0 && x < Width && Enumerable.Range(ty - 1, 3).Any(y =>
            {
                if (y < 0 || y >= Height)
                    return false;
                var loc = x + Width * (y + Height * (tz + 1));
                var pos = Tiles.IndexOf(tile => tile.Location == loc);
                return pos != -1 && !taken[pos];
            })))
                return false;

            // Permissible if there’s no tile to its immediate left or right
            return tx - 2 < 0 || tx + 2 >= Width || Enumerable.Range(ty - 1, 3).All(y =>
            {
                if (y < 0 || y >= Height)
                    return true;
                var loc = tx - 2 + Width * (y + Height * tz);
                var pos = Tiles.IndexOf(tile => tile.Location == loc);
                return pos == -1 || taken[pos];
            }) || Enumerable.Range(ty - 1, 3).All(y =>
            {
                if (y < 0 || y >= Height)
                    return true;
                var loc = tx + 2 + Width * (y + Height * tz);
                var pos = Tiles.IndexOf(tile => tile.Location == loc);
                return pos == -1 || taken[pos];
            });
        }
    }
}
