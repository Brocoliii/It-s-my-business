using UnityEngine;
using UnityEngine.UI;

public class CustomGridLayoutGroup : LayoutGroup
{
    [Header("Grid")]
    public int columnCount = 2;
    public Vector2 cellSize = new Vector2(150, 150);
    public Vector2 spacing = new Vector2(20, 20);

    [Header("Individual Offset")]
    public Vector2[] offsets;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        int rows = Mathf.CeilToInt((float)rectChildren.Count / columnCount);

        float width =
            padding.left +
            padding.right +
            (columnCount * cellSize.x) +
            ((columnCount - 1) * spacing.x);

        SetLayoutInputForAxis(width, width, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        int rows = Mathf.CeilToInt((float)rectChildren.Count / columnCount);

        float height =
            padding.top +
            padding.bottom +
            (rows * cellSize.y) +
            ((rows - 1) * spacing.y);

        SetLayoutInputForAxis(height, height, -1, 1);
    }

    public override void SetLayoutHorizontal()
    {
        SetChildren();
    }

    public override void SetLayoutVertical()
    {
        SetChildren();
    }

    private void SetChildren()
    {
        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];

            int column = i % columnCount;
            int row = i / columnCount;

            float x = padding.left + column * (cellSize.x + spacing.x);
            float y = padding.top + row * (cellSize.y + spacing.y);

            // Individual offset
            if (offsets != null && i < offsets.Length)
            {
                x += offsets[i].x;
                y += offsets[i].y;
            }

            SetChildAlongAxis(child, 0, x, cellSize.x);
            SetChildAlongAxis(child, 1, y, cellSize.y);
        }
    }
}