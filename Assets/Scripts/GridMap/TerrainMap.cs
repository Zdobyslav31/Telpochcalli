using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainMap {
    private int MOVE_STRAIGHT_COST = 10;
    private int MOVE_DIAGONAL_COST = 14;
    private Dictionary<TerrainNode.TerrainType, int> terrainCostMultipliers;

    private Grid<TerrainNode> grid;
    private List<TerrainNode> openList;
    private List<TerrainNode> closedList;

    public TerrainMap(int width, int height, float cellSize, Vector3 originPosition, bool showDebug = false) {
        grid = new Grid<TerrainNode>(
                width, height, cellSize, originPosition,
                (Grid<TerrainNode> g, int x, int y) => new TerrainNode(g, x, y), showDebug
            );
        terrainCostMultipliers = new Dictionary<TerrainNode.TerrainType, int>{
            { TerrainNode.TerrainType.Difficult, 2 },
            { TerrainNode.TerrainType.Sand, 3 }
        };
    }

    /* 
     * Tilemap logic
     */
    public Grid<TerrainNode> GetGrid() {
        return grid;
    }

    public void SetTerrainType(Vector3 worldPosition, TerrainNode.TerrainType terrainType) {
        TerrainNode terrainNode = grid.GetGridObject(worldPosition);
        try {
            terrainNode.SetTerrainType(terrainType);
        } catch (NullReferenceException) {
            Debug.Log("Tried to set terrain type outside board");
        }

    }

    public void SetTerrainVisual(TerrainVisual terrainVisual) {
        terrainVisual.SetGrid(grid);
    }

    /*
     * Line of sight logic
     */
    public bool IsFieldVisible(int x, int y) {
        return grid.GetGridObject(x, y).isVisible;
    }

    public void UpdateVisibleFields(int observerX, int observerY) {
        SetEntireGridVisibility(false);
        foreach (TerrainNode field in GetGrid().GetBorderNodes()) {
            CastRay((float)observerX + .5f, (float)observerY + .5f, (float)field.x + .5f, (float)field.y + .5f);
        }
    }

    private void SetEntireGridVisibility(bool value) {
        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                SetVisibility(x, y, value);
            }
        }
    }

    private void SetVisibility(int x, int y, bool value) {
        GetGrid().GetGridObject(x, y).isVisible = value;
        grid.TriggerGridObjectChanged(x, y);
    }

    private void CastRay(float startX, float startY, float endX, float endY) {
        List<float[]> intersections = GetIntersectionPointsOnSegment(startX, startY, endX, endY);
        foreach (float[] intersection in intersections) {
            TerrainNode field = GetGrid().GetGridObject(
                startX < endX ? (int)Math.Floor(intersection[0]) : (int)Math.Ceiling(intersection[0]) - 1,
                startY < endY ? (int)Math.Floor(intersection[1]) : (int)Math.Ceiling(intersection[1]) - 1
                );
            if (field.blocksSight) {
                break;
            } else {
                SetVisibility(field.x, field.y, true);
            }
        }
    }

    private List<float[]> GetIntersectionPointsOnSegment(float startX, float startY, float endX, float endY) {
        // coefficients of the linear equation of the ray
        float a = endX - startX;
        float b = startY - endY;
        float c = endY * startX - startY * startX - startY * endX + startY * startX;
        List<float[]> intersectionsList = new List<float[]>();
        // Intersections with vertical lines
        foreach (int x in (Enumerable.Range(
                (int)Math.Ceiling(Math.Min(startX, endX)),
                (int)Math.Floor(Math.Max(startX, endX)) - (int)Math.Ceiling(Math.Min(startX, endX)) + 1
            ))) {
            intersectionsList.Add(new float[] { (float)x, GetLinearYParameter(a, b, c, (float)x) });
        }
        //Intersections with horizontal lines
        foreach (int y in (Enumerable.Range(
                (int)Math.Ceiling(Math.Min(startY, endY)),
                (int)Math.Floor(Math.Max(startY, endY)) - (int)Math.Ceiling(Math.Min(startY, endY)) + 1
            ))) {
            float[] intersection = new float[] { GetLinearXParameter(a, b, c, (float)y), (float)y };
            // only if this intersection wasn't with both vertical & horizontal line simultaneously
            if (!intersectionsList.Contains(intersection)) intersectionsList.Add(intersection);
        }
        return OrderIntersectionPoints(intersectionsList,startX, startY, endX, endY);
    }

    private List<float[]> OrderIntersectionPoints(
        List<float[]> intersectionsList, float startX, float startY, float endX, float endY
        ) {
        List<float[]> sortedList = intersectionsList
            .OrderBy(a => startX < endX ? a[0] : -a[0] )
            .ThenBy(a => startY < endY ? a[1] : -a[1])
            .ToList();
        return sortedList;
    }

    private float GetLinearXParameter(float a, float b, float c, float y) {
        return (-a * y - c) / b;
    }

    private float GetLinearYParameter(float a, float b, float c, float x) {
        return (-b * x - c) / a;
    }

    /*
     * Pathfinding logic
     */

    public List<TerrainNode> FindPath(
            int startX, int startY, int endX, int endY, out int pathDistance,
            List<TerrainNode> additionalUnwalkableNodes = null,
            List<TerrainNode> endangeredNodes = null
        ) {
        ResetEndangeredNodes(endangeredNodes);
        TerrainNode startNode = grid.GetGridObject(startX, startY);
        TerrainNode endNode = grid.GetGridObject(endX, endY);

        openList = new List<TerrainNode> { startNode };
        closedList = new List<TerrainNode>();

        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                TerrainNode pathNode = grid.GetGridObject(x, y);
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode.cameFromNode = null;
            }
        }
        startNode.gCost = 0;
        startNode.hCost = CalculateTentativeDistanceCost(startNode, endNode);
        startNode.CalculateFCost();

        while (openList.Count > 0) {
            TerrainNode currentNode = GetLowestFCostNode(openList);
            if (currentNode == endNode) {
                // Reached final node
                pathDistance = currentNode.gCost;
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);
            foreach (TerrainNode neighbourNode in grid.GetNeighboursList(currentNode.x, currentNode.y)) {
                if (closedList.Contains(neighbourNode))
                    continue;
                if (!neighbourNode.isWalkable || additionalUnwalkableNodes.Contains(neighbourNode)) {
                    closedList.Add(neighbourNode);
                    continue;
                }

                int tentativeGCost = currentNode.gCost + CalculateAccurateNeighbourDistanceCost(currentNode, neighbourNode);
                if (tentativeGCost < neighbourNode.gCost) {
                    neighbourNode.cameFromNode = currentNode;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = CalculateTentativeDistanceCost(neighbourNode, endNode);
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode)) {
                        openList.Add(neighbourNode);
                    }
                }

            }
        }
        // Out of nodes on the openList, no valid path
        Debug.Log("Pathfinding: No valid path");
        pathDistance = int.MaxValue;
        return null;
    }


    private List<TerrainNode> CalculatePath(TerrainNode endNode) {
        List<TerrainNode> path = new List<TerrainNode>();
        path.Add(endNode);
        TerrainNode currentNode = endNode;
        while (currentNode.cameFromNode != null) {
            path.Add(currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }
        path.Reverse();
        return path;
    }

    private int CalculateTentativeDistanceCost(TerrainNode a, TerrainNode b) {
        int xDistance = Mathf.Abs(a.x - b.x);
        int yDistance = Mathf.Abs(a.y - b.y);
        int remaining = Mathf.Abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    private int CalculateAccurateNeighbourDistanceCost(TerrainNode a, TerrainNode b) {
        if (Mathf.Abs(a.x - b.x) > 1 || Mathf.Abs(a.y - b.y) > 1) {
            Debug.LogWarning("Tried to calculate neigbour distance cost for nodes that are not neighbours");
            return CalculateTentativeDistanceCost(a, b);
        }
        int terrainCostMultiplier = (
                GetTerrainCostMultiplier(b.GetTerrainType()) + GetTerrainCostMultiplier(a.GetTerrainType())
            ) / 2;
        if (a.x == b.x || a.y == b.y) {
            return MOVE_STRAIGHT_COST * terrainCostMultiplier;
        } else {
            return MOVE_DIAGONAL_COST * terrainCostMultiplier;
        }
    }

    private TerrainNode GetLowestFCostNode(List<TerrainNode> pathNodeList) {
        TerrainNode lowestFCostNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++) {
            if (pathNodeList[i].fCost < lowestFCostNode.fCost) {
                lowestFCostNode = pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }

    private void ResetEndangeredNodes(List<TerrainNode> endangeredNodes) {
        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                GetNode(x, y).isEndangered = false;
                grid.TriggerGridObjectChanged(x, y);
            }
        }
        foreach (TerrainNode terrainNode in endangeredNodes) {
            terrainNode.isEndangered = true;
            grid.TriggerGridObjectChanged(terrainNode.x, terrainNode.y);
        }
    }

    public TerrainNode GetNode(int x, int y) {
        return grid.GetGridObject(x, y);
    }

    public int GetTerrainCostMultiplier(TerrainNode.TerrainType terrainType) {
        if (terrainCostMultipliers.ContainsKey(terrainType)) {
            return terrainCostMultipliers[terrainType];
        } else {
            return 1;
        }
    }

    public bool IsWalkable(int x, int y) {
        return grid.GetGridObject(x, y).isWalkable;
    }

    public bool IsWalkable(Vector3 position) {
        return grid.GetGridObject(position).isWalkable;
    }
}