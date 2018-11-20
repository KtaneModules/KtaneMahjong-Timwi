using UnityEngine;

namespace Mahjong
{
    sealed class TileInfo
    {
        public int Location { get; private set; }
        public GameObject GameObject { get; private set; }
        public Transform Transform { get; private set; }
        private MeshRenderer _meshRenderer;
        public KMSelectable KMSelectable { get; private set; }

        private Texture _normal, _highlighted;
        public int PairedWith;

        public TileInfo(int location, KMSelectable selectable)
        {
            Location = location;
            KMSelectable = selectable;
            GameObject = selectable.gameObject;
            Transform = selectable.transform;
            _meshRenderer = selectable.GetComponent<MeshRenderer>();
        }

        public void SetTextures(Texture normal, Texture highlighted)
        {
            _normal = normal;
            _highlighted = highlighted;
            SetNormal();
        }
        public string Name { get { return _normal.name.Replace(" normal", ""); } }
        public void SetNormal() { _meshRenderer.material.mainTexture = _normal; }
        public void SetHighlighted() { _meshRenderer.material.mainTexture = _highlighted; }
    }
}
