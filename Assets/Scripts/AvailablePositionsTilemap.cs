using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvailablePositionsTilemap {
    private Grid<AvailablePositionsTilemapObject> grid;
    public AvailablePositionsTilemap(int width, int height, float cellSize, Vector3 originPosition,
            bool showDebug = false
        ) {
        grid = new Grid<AvailablePositionsTilemapObject>(
                width, height, cellSize, originPosition,
                (Grid<AvailablePositionsTilemapObject> g, int x, int y) => new AvailablePositionsTilemapObject(g, x, y),
                showDebug
            );
    }

    public void SetTilemapSprite(Vector3 worldPosition, AvailablePositionsTilemapObject.TilemapSprite tilemapSprite) {
        AvailablePositionsTilemapObject tilemapObject = grid.GetGridObject(worldPosition);
        if (tilemapObject != null) {
            tilemapObject.SetTilemapSprite(tilemapSprite);
        }
    }

    public void SetTilemapSprite(int x, int y, AvailablePositionsTilemapObject.TilemapSprite tilemapSprite) {
        AvailablePositionsTilemapObject tilemapObject = grid.GetGridObject(x, y);
        if (tilemapObject != null) {
            tilemapObject.SetTilemapSprite(tilemapSprite);
        }
    }

    public void SetAvailablePositionsTilemapVisual(AvailablePositionsTilemapVisual tilemapVisual) {
        tilemapVisual.SetGrid(grid);
    }


    public void SetAllTilemapSprite(AvailablePositionsTilemapObject.TilemapSprite tilemapSprite) {
        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                SetTilemapSprite(x, y, tilemapSprite);
            }
        }
    }

    public void ClearTilemap() {
        SetAllTilemapSprite(
            AvailablePositionsTilemapObject.TilemapSprite.None
        );
    }
}

public class AvailablePositionsTilemapObject {
    public enum TilemapSprite {
        None,
        Move,
        RotateN,
        RotateS,
        RotateW,
        RotateE,
        Attack
    }

    private Grid<AvailablePositionsTilemapObject> grid;
    private int x;
    private int y;
    private TilemapSprite tilemapSprite;

    public AvailablePositionsTilemapObject(Grid<AvailablePositionsTilemapObject> grid, int x, int y) {
        this.grid = grid;
        this.x = x;
        this.y = y;
    }

    public void SetTilemapSprite(TilemapSprite tilemapSprite) {
        this.tilemapSprite = tilemapSprite;
        grid.TriggerGridObjectChanged(x, y);
    }

    public TilemapSprite GetTilemapSprite() {
        return tilemapSprite;
    }

    public override string ToString() {
        return tilemapSprite.ToString();
    }
}