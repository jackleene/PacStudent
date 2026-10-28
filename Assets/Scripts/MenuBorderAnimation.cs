using UnityEngine;
using UnityEngine.UI;

public class MenuBorderAnimation : MaskableGraphic
{
    private const float DashLength = 28f;
    private const float Gap = 16f;
    private const float Thickness = 6f;
    private const float BlinkInterval = 1f;

    private float elapsed;
    private bool bright = true;

    protected override void OnEnable()
    {
        base.OnEnable();
        elapsed = 0f;
        bright = true;
        canvasRenderer.SetAlpha(1f);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        if (elapsed < BlinkInterval)
        {
            return;
        }

        elapsed %= BlinkInterval;
        bright = !bright;
        canvasRenderer.SetAlpha(bright ? 1f : 0.2f);
    }

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect bounds = GetPixelAdjustedRect();
        AddEdge(vertices, new Vector2(bounds.xMin, bounds.yMin), bounds.width, true);
        AddEdge(vertices, new Vector2(bounds.xMin, bounds.yMax - Thickness), bounds.width, true);
        AddEdge(vertices, new Vector2(bounds.xMin, bounds.yMin + Thickness), bounds.height - 2f * Thickness, false);
        AddEdge(vertices, new Vector2(bounds.xMax - Thickness, bounds.yMin + Thickness), bounds.height - 2f * Thickness, false);
    }

    private void AddEdge(VertexHelper vertices, Vector2 origin, float length, bool horizontal)
    {
        int count = Mathf.FloorToInt((length + Gap) / (DashLength + Gap));
        float offset = (length - count * DashLength - (count - 1) * Gap) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float position = Mathf.Round(offset + i * (DashLength + Gap));
            Vector2 min = origin + (horizontal ? new Vector2(position, 0f) : new Vector2(0f, position));
            Vector2 max = min + (horizontal ? new Vector2(DashLength, Thickness) : new Vector2(Thickness, DashLength));
            int first = vertices.currentVertCount;
            vertices.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
            vertices.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
            vertices.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
            vertices.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
            vertices.AddTriangle(first, first + 1, first + 2);
            vertices.AddTriangle(first + 2, first + 3, first);
        }
    }
}
