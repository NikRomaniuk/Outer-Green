using UnityEngine;

[ExecuteInEditMode]
public class SpriteAtlasHelper : MonoBehaviour
{
    private SpriteRenderer sr;
    private MaterialPropertyBlock block;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        block = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (sr == null || sr.sprite == null) return;

        Vector4 uv = UnityEngine.Sprites.DataUtility.GetInnerUV(sr.sprite);
        float scaleX = uv.z - uv.x;
        float scaleY = uv.w - uv.y;

        Vector3 worldScale = transform.lossyScale;

        float pixelWidth = sr.sprite.rect.width;
        float pixelHeight = sr.sprite.rect.height;

        sr.GetPropertyBlock(block);
        block.SetVector("_CustomUVScale", new Vector4(scaleX, scaleY, 0, 0));
        block.SetVector("_CustomWorldScale", new Vector4(worldScale.x, worldScale.y, 1, 1));
        block.SetVector("_SpritePixelSize", new Vector2(pixelWidth, pixelHeight));
        sr.SetPropertyBlock(block);
    }
}
