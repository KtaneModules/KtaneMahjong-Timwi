namespace Mahjong
{
    struct TilePair
    {
        public int Ix1 { get; private set; }
        public int Ix2 { get; private set; }
        public TilePair(int ix1, int ix2) { Ix1 = ix1; Ix2 = ix2; }
    }
}
