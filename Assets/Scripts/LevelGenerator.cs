using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private GameObject manualLevel;
    [SerializeField] private Transform generatedLevel;

    [SerializeField] private GameObject outsideCornerPrefab;
    [SerializeField] private GameObject outsideWallPrefab;
    [SerializeField] private GameObject insideCornerPrefab;
    [SerializeField] private GameObject insideWallPrefab;
    [SerializeField] private GameObject normalPelletPrefab;
    [SerializeField] private GameObject powerPelletPrefab;
    [SerializeField] private GameObject tJunctionPrefab;
    [SerializeField] private GameObject ghostExitWallPrefab;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraPadding = 1f;

    [SerializeField] private float outsideCornerOffset;
    [SerializeField] private float outsideWallOffset;
    [SerializeField] private float insideCornerOffset;
    [SerializeField] private float insideWallOffset;
    [SerializeField] private float tJunctionOffset;
    [SerializeField] private float ghostExitWallOffset;

    private readonly int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
    };

    private void Start()
    {
        if (manualLevel != null)
        {
            Destroy(manualLevel);
        }

        ClearGeneratedLevel();
        GenerateLevel();
        AdjustCamera();
    }

    private void GenerateLevel()
    {
        int height = levelMap.GetLength(0);
        int width = levelMap.GetLength(1);

        Transform topLeft = CreateQuadrant(
            "TopLeft",
            Vector3.zero,
            new Vector3(1f, 1f, 1f)
        );

        Transform topRight = CreateQuadrant(
            "TopRight",
            new Vector3(width * 2 - 1, 0f, 0f),
            new Vector3(-1f, 1f, 1f)
        );

        Transform bottomLeft = CreateQuadrant(
            "BottomLeft",
            new Vector3(0f, -(height - 1) * 2f, 0f),
            new Vector3(1f, -1f, 1f)
        );

        Transform bottomRight = CreateQuadrant(
            "BottomRight",
            new Vector3(width * 2 - 1, -(height - 1) * 2f, 0f),
            new Vector3(-1f, -1f, 1f)
        );

        GenerateQuadrant(topLeft, height);
        GenerateQuadrant(topRight, height);
        GenerateQuadrant(bottomLeft, height - 1);
        GenerateQuadrant(bottomRight, height - 1);
    }

    private Transform CreateQuadrant(string quadrantName, Vector3 position, Vector3 scale)
    {
        GameObject quadrant = new GameObject(quadrantName);
        quadrant.transform.SetParent(generatedLevel);
        quadrant.transform.localPosition = position;
        quadrant.transform.localRotation = Quaternion.identity;
        quadrant.transform.localScale = scale;
        return quadrant.transform;
    }

    private void GenerateQuadrant(Transform parent, int rowCount)
    {
        int width = levelMap.GetLength(1);

        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int tile = levelMap[row, column];

                if (tile == 0)
                {
                    continue;
                }

                GameObject prefab = GetPrefab(tile);

                if (prefab == null)
                {
                    continue;
                }

                Vector3 position = new Vector3(column, -row, 0f);
                float rotation = GetRotation(row, column, tile);

                GameObject instance = Instantiate(
                    prefab,
                    parent
                );

                instance.name = $"{tile}_{row}_{column}";
                instance.transform.localPosition = position;
                instance.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
                instance.transform.localScale = Vector3.one;
            }
        }
    }

    private GameObject GetPrefab(int tile)
    {
        switch (tile)
        {
            case 1:
                return outsideCornerPrefab;
            case 2:
                return outsideWallPrefab;
            case 3:
                return insideCornerPrefab;
            case 4:
                return insideWallPrefab;
            case 5:
                return normalPelletPrefab;
            case 6:
                return powerPelletPrefab;
            case 7:
                return tJunctionPrefab;
            case 8:
                return ghostExitWallPrefab;
            default:
                return null;
        }
    }

    private float GetRotation(int row, int column, int tile)
    {
        switch (tile)
        {
            case 1:
                return GetOutsideCornerRotation(row, column) + outsideCornerOffset;
            case 2:
                return GetStraightRotation(row, column, true) + outsideWallOffset;
            case 3:
                return GetInsideCornerRotation(row, column) + insideCornerOffset;
            case 4:
                return GetStraightRotation(row, column, false) + insideWallOffset;
            case 7:
                return GetTJunctionRotation(row, column) + tJunctionOffset;
            case 8:
                return GetGhostExitRotation(row, column) + ghostExitWallOffset;
            default:
                return 0f;
        }
    }

    private float GetOutsideCornerRotation(int row, int column)
    {
        bool up = IsOuterStructure(GetValue(row - 1, column));
        bool down = IsOuterStructure(GetValue(row + 1, column));
        bool left = IsOuterStructure(GetValue(row, column - 1));
        bool right = IsOuterStructure(GetValue(row, column + 1));

        if (right && down)
        {
            return 0f;
        }

        if (up && right)
        {
            return 90f;
        }

        if (up && left)
        {
            return 180f;
        }

        if (down && left)
        {
            return 270f;
        }

        return 0f;
    }

    private float GetStraightRotation(int row, int column, bool outer)
    {
        int horizontal = 0;
        int vertical = 0;

        if (IsStructure(GetValue(row, column - 1), outer))
        {
            horizontal++;
        }

        if (IsStructure(GetValue(row, column + 1), outer))
        {
            horizontal++;
        }

        if (IsStructure(GetValue(row - 1, column), outer))
        {
            vertical++;
        }

        if (IsStructure(GetValue(row + 1, column), outer))
        {
            vertical++;
        }

        return vertical > horizontal ? 90f : 0f;
    }

    private float GetInsideCornerRotation(int row, int column)
    {
        bool up = IsInnerStructure(GetValue(row - 1, column));
        bool right = IsInnerStructure(GetValue(row, column + 1));
        bool down = IsInnerStructure(GetValue(row + 1, column));
        bool left = IsInnerStructure(GetValue(row, column - 1));

        int connections = 0;

        if (up) connections++;
        if (right) connections++;
        if (down) connections++;
        if (left) connections++;

        if (connections == 2)
        {
            if (right && down)
            {
                return 0f;
            }

            if (up && right)
            {
                return 90f;
            }

            if (up && left)
            {
                return 180f;
            }

            if (down && left)
            {
                return 270f;
            }
        }

        bool upLeftOpen = IsOpen(GetValue(row - 1, column - 1));
        bool upRightOpen = IsOpen(GetValue(row - 1, column + 1));
        bool downRightOpen = IsOpen(GetValue(row + 1, column + 1));
        bool downLeftOpen = IsOpen(GetValue(row + 1, column - 1));

        if (downRightOpen)
        {
            return 0f;
        }

        if (upRightOpen)
        {
            return 90f;
        }

        if (upLeftOpen)
        {
            return 180f;
        }

        if (downLeftOpen)
        {
            return 270f;
        }

        if (right && down)
        {
            return 0f;
        }

        if (up && right)
        {
            return 90f;
        }

        if (up && left)
        {
            return 180f;
        }

        if (down && left)
        {
            return 270f;
        }

        return 0f;
    }

    private float GetTJunctionRotation(int row, int column)
    {
        bool up = IsAnyStructure(GetValue(row - 1, column));
        bool down = IsAnyStructure(GetValue(row + 1, column));
        bool left = IsAnyStructure(GetValue(row, column - 1));
        bool right = IsAnyStructure(GetValue(row, column + 1));

        if (!up && down && left && right)
        {
            return 0f;
        }

        if (up && down && !left && right)
        {
            return 90f;
        }

        if (up && !down && left && right)
        {
            return 180f;
        }

        if (up && down && left && !right)
        {
            return 270f;
        }

        return 0f;
    }

    private float GetGhostExitRotation(int row, int column)
    {
        bool left = IsInnerStructure(GetValue(row, column - 1));
        bool right = IsInnerStructure(GetValue(row, column + 1));
        bool up = IsInnerStructure(GetValue(row - 1, column));
        bool down = IsInnerStructure(GetValue(row + 1, column));

        if (up || down)
        {
            if (!left && !right)
            {
                return 90f;
            }
        }

        return 0f;
    }

    private int GetValue(int row, int column)
    {
        int height = levelMap.GetLength(0);
        int width = levelMap.GetLength(1);

        if (column == width)
        {
            column = width - 1;
        }

        if (row == height)
        {
            row = height - 2;
        }

        if (row < 0 || row >= height || column < 0 || column >= width)
        {
            return -1;
        }

        return levelMap[row, column];
    }

    private bool IsOuterStructure(int tile)
    {
        return tile == 1 || tile == 2 || tile == 7;
    }

    private bool IsInnerStructure(int tile)
    {
        return tile == 3 || tile == 4 || tile == 7 || tile == 8;
    }

    private bool IsAnyStructure(int tile)
    {
        return tile == 1 ||
               tile == 2 ||
               tile == 3 ||
               tile == 4 ||
               tile == 7 ||
               tile == 8;
    }

    private bool IsStructure(int tile, bool outer)
    {
        return outer ? IsOuterStructure(tile) : IsInnerStructure(tile);
    }

    private bool IsOpen(int tile)
    {
        return tile == 0 || tile == 5 || tile == 6;
    }

    private void ClearGeneratedLevel()
    {
        for (int i = generatedLevel.childCount - 1; i >= 0; i--)
        {
            Destroy(generatedLevel.GetChild(i).gameObject);
        }
    }

    private void AdjustCamera()
    {
        if (mainCamera == null)
        {
            return;
        }

        int height = levelMap.GetLength(0);
        int width = levelMap.GetLength(1);

        float fullWidth = width * 2f;
        float fullHeight = height * 2f - 1f;

        float centerX = (fullWidth - 1f) * 0.5f;
        float centerY = -(fullHeight - 1f) * 0.5f;

        Vector3 cameraPosition = mainCamera.transform.position;
        cameraPosition.x = centerX;
        cameraPosition.y = centerY;
        mainCamera.transform.position = cameraPosition;

        float verticalSize = fullHeight * 0.5f + cameraPadding;
        float horizontalSize = fullWidth / (2f * mainCamera.aspect) + cameraPadding;

        mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }
}