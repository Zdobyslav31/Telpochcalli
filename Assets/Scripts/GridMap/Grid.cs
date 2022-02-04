using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using System;

public class Grid<TGridObject> {
    public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
    public class OnGridObjectChangedEventArgs : EventArgs {
        public int x;
        public int y;
    }
    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private TGridObject[,] gridArray;
    private bool showDebug;


    // Constructor
    public Grid(
        int width, int height, float cellSize, Vector3 originPosition,
        Func<Grid<TGridObject>, int, int, TGridObject> createGridObject, bool showDebug = false
        ) {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        this.showDebug = showDebug;

        gridArray = new TGridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int y = 0; y < gridArray.GetLength(1); y++) {
                gridArray[x, y] = createGridObject(this, x, y);
            }
        }

        if (showDebug) {

            TextMesh[,] debugTextArray = new TextMesh[width, height];

            for (int x = 0; x < gridArray.GetLength(0); x++) {
                for (int y = 0; y < gridArray.GetLength(1); y++) {
                    debugTextArray[x, y] = UtilsClass.CreateWorldText(
                        gridArray[x, y]?.ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * .5f,
                        12, Color.white, TextAnchor.MiddleCenter
                    );
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 100f);
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 100f);
                }
            }
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

            OnGridObjectChanged += (object sender, OnGridObjectChangedEventArgs eventArgs) => {
                debugTextArray[eventArgs.x, eventArgs.y].text = gridArray[eventArgs.x, eventArgs.y]?.ToString();
            };
        }
    }

    public int GetWidth() {
        return width;
    }

    public int GetHeight() {
        return height;
    }

    public float GetCellSize() {
        return cellSize;
    }

    public Vector3 GetWorldPosition(int x, int y) {
        return new Vector3(x, y) * cellSize + originPosition;
    }

    public Vector3 GetCenterWorldPosition(int x, int y) {
        return GetWorldPosition(x, y) + new Vector3(cellSize * .5f, cellSize * .5f);
    }


    public List<TGridObject> GetNeighboursList(int x, int y) {
        List<TGridObject> neighboursList = new List<TGridObject>();
        if (x - 1 >= 0) {
            // Left
            neighboursList.Add(GetGridObject(x - 1, y));
            // Left down
            if (y - 1 >= 0)
                neighboursList.Add(GetGridObject(x - 1, y - 1));
            //Left up
            if (y + 1 < height)
                neighboursList.Add(GetGridObject(x - 1, y + 1));

        }
        if (x + 1 < width) {
            // Right
            neighboursList.Add(GetGridObject(x + 1, y));
            // Right down
            if (y - 1 >= 0)
                neighboursList.Add(GetGridObject(x + 1, y - 1));
            //Right up
            if (y + 1 < height)
                neighboursList.Add(GetGridObject(x + 1, y + 1));

        }
        // Down
        if (y - 1 >= 0)
            neighboursList.Add(GetGridObject(x, y - 1));
        // Up
        if (y + 1 < height)
            neighboursList.Add(GetGridObject(x, y + 1));
        return neighboursList;
    }

    public List<TGridObject> GetBorderNodes() {
        List<TGridObject> borderNodes = new List<TGridObject>();
        for (int x = 0; x < width; x++) {
            borderNodes.Add(GetGridObject(x, 0));
            borderNodes.Add(GetGridObject(x, GetHeight() - 1));
        }
        for (int y = 1; y < height - 1; y++) {
            borderNodes.Add(GetGridObject(0, y));
            borderNodes.Add(GetGridObject(GetWidth() - 1, y));
        }
        return borderNodes;
    }

    public void GetGridPosition(Vector3 worldPosition, out int x, out int y) {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }

    public void SetGridObject(int x, int y, TGridObject value) {
        if (x >= 0 && y >= 0 && x < width && y < height) {
            gridArray[x, y] = value;
            if (OnGridObjectChanged != null) {
                OnGridObjectChanged(this, new OnGridObjectChangedEventArgs { x = x, y = y });
            }
        } else {
            Debug.Log("Grid: failed attempt to SetGridObject: illegal coordinates:" + x + "," + y);
        }
    }

    public void TriggerGridObjectChanged(int x, int y) {
        if (OnGridObjectChanged != null)
            OnGridObjectChanged(this, new OnGridObjectChangedEventArgs { x = x, y = y });
    }

    public void SetGridObject(Vector3 worldPosition, TGridObject value) {
        int x, y;
        GetGridPosition(worldPosition, out x, out y);
        SetGridObject(x, y, value);
    }

    public TGridObject GetGridObject(int x, int y) {
        if (x >= 0 && y >= 0 && x < width && y < height) {
            return gridArray[x, y];
        } else {
            return default(TGridObject);
        }
    }

    public TGridObject GetGridObject(Vector3 worldPosition) {
        int x, y;
        GetGridPosition(worldPosition, out x, out y);
        return GetGridObject(x, y);
    }

}