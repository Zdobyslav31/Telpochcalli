using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class TestingTerrain : MonoBehaviour {
    [SerializeField] private TerrainVisual terrainVisual;
    [SerializeField] private bool showDebug;
    private TerrainMap terrain;
    private TerrainNode.TerrainType terrainType;
    private Vector3 origin;

    void Start() {
        origin = new Vector3(-20, -20, 0);
        terrain = new TerrainMap(10, 10, 4f, origin, showDebug);

        terrain.SetTerrainVisual(terrainVisual);
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            terrain.SetTerrainType(mouseWorldPosition, terrainType);
        }

        if (Input.GetKeyDown(KeyCode.N)) {
            terrainType = TerrainNode.TerrainType.Normal;

        }

        if (Input.GetKeyDown(KeyCode.D)) {
            terrainType = TerrainNode.TerrainType.Difficult;
        }

        if (Input.GetKeyDown(KeyCode.U)) {
            terrainType = TerrainNode.TerrainType.Unwalkable;
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            terrainType = TerrainNode.TerrainType.Sand;
        }

        if (Input.GetMouseButtonDown(1)) {
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            terrain.GetGrid().GetGridPosition(mouseWorldPosition, out int x, out int y);
            List<TerrainNode> path = terrain.FindPath(0, 0, x, y, out _);
            if (path != null) {
                for (int i = 0; i < path.Count - 1; i++) {
                    Debug.DrawLine(
                        origin + new Vector3(path[i].x, path[i].y) * 4f + Vector3.one * 2f,
                        origin + new Vector3(path[i + 1].x, path[i + 1].y) * 4f + Vector3.one * 2f,
                        Color.green,
                        3f
                    );
                }
            }
        }
    }
}
