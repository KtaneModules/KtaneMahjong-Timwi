using UnityEngine;

namespace Mahjong
{
    sealed class TileInfo
    {
        public int Location { get; private set; }
        public GameObject GameObject { get; private set; }
        public Transform Transform { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }
        public KMSelectable KMSelectable { get; private set; }

        public TileInfo(int location, KMSelectable selectable)
        {
            Location = location;
            KMSelectable = selectable;
            GameObject = selectable.gameObject;
            Transform = selectable.transform;
            MeshRenderer = selectable.GetComponent<MeshRenderer>();
        }
    }
}
