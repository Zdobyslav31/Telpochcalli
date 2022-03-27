using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainNode {
    public enum TerrainType {
        Normal,
        Difficult,
        Unwalkable,
        Sand
    }
    //public enum Visibility {
    //    Invisible,
    //    Covered,
    //    Visible
    //}

    private Grid<TerrainNode> grid;
    public int x;
    public int y;
    private TerrainType terrainType;

    // Pathfinding
    public int gCost; // walking cost from start node
    public int hCost; // heuristic cost to reach end node
    public int fCost; // g + h

    public bool isWalkable;
    public bool isEndangered;

    // Line of sight
    public bool isVisible; // TODO: visibility levels
    public bool blocksSight;

    public TerrainNode cameFromNode;

    public TerrainNode(Grid<TerrainNode> grid, int x, int y) {
        this.grid = grid;
        this.x = x;
        this.y = y;
        this.isWalkable = true;
        this.isEndangered = false;
        this.blocksSight = false;
        this.terrainType = TerrainType.Normal;
    }

    public void CalculateFCost() {
        fCost = gCost + hCost;
    }

    public void SetTerrainType(TerrainType terrainType) {
        this.terrainType = terrainType;
        if (terrainType == TerrainType.Unwalkable) {
            this.isWalkable = false;
            this.blocksSight = true;
        } else {
            this.isWalkable = true;
            this.blocksSight = false;
        }
        grid.TriggerGridObjectChanged(x, y);
    }

    public TerrainType GetTerrainType() {
        return terrainType;
    }

    public bool AllowsCharge() {
        return (GetTerrainType() == TerrainNode.TerrainType.Normal);
    }

    public override string ToString() {
        if (blocksSight) {
            return "X";
        } else {
            if (!isEndangered) {
                return x + "," + y;
            }
            return "[" + x + "," + y + "]";
        }
    }
}
