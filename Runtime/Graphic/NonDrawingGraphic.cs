namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Non Drawing Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class NonDrawingGraphic : MaskableGraphic
    {
        public override void SetMaterialDirty() { return; }
        public override void SetVerticesDirty() { return; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}