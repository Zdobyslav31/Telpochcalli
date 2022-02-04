using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvailablePositionsTilemapVisual : MonoBehaviour {
    [System.Serializable]
    public struct TilemapSpriteUV {
        public AvailablePositionsTilemapObject.TilemapSprite tilemapSprite;
        public Vector2Int uv00Pixels;
        public Vector2Int uv11Pixels;
    }
    private struct UVCoords {
        public Vector2 uv00;
        public Vector2 uv11;
    }

    [SerializeField] private TilemapSpriteUV[] tilemapSpriteUVArray;
    private Grid<AvailablePositionsTilemapObject> grid;
    private Mesh mesh;
    private bool updateMesh;
    private Dictionary<AvailablePositionsTilemapObject.TilemapSprite, UVCoords> uvCoordsDict;

    private void Awake() {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Convert texture coords from pixels to normalized value
        Texture texture = GetComponent<MeshRenderer>().material.mainTexture;
        float textureWidth = texture.width;
        float textureHeight = texture.height;

        uvCoordsDict = new Dictionary<AvailablePositionsTilemapObject.TilemapSprite, UVCoords>();

        foreach (TilemapSpriteUV tilemapSpriteUV in tilemapSpriteUVArray) {
            uvCoordsDict[tilemapSpriteUV.tilemapSprite] = new UVCoords {
                uv00 = new Vector2(
                    tilemapSpriteUV.uv00Pixels.x / textureWidth,
                    tilemapSpriteUV.uv00Pixels.y / textureHeight
                ),
                uv11 = new Vector2(
                    tilemapSpriteUV.uv11Pixels.x / textureWidth,
                    tilemapSpriteUV.uv11Pixels.y / textureHeight
                ),
            };
        }
    }

    public void SetGrid(Grid<AvailablePositionsTilemapObject> grid) {
        this.grid = grid;
        UpdateTilemapVisual();

        grid.OnGridObjectChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<AvailablePositionsTilemapObject>.OnGridObjectChangedEventArgs e) {
        updateMesh = true;
    }

    private void LateUpdate() {
        if (updateMesh) {
            updateMesh = false;
            UpdateTilemapVisual();
        }
    }

    private void UpdateTilemapVisual() {
        MeshUtils.CreateEmptyMeshArrays(
            grid.GetWidth() * grid.GetHeight(),
            out Vector3[] vertices, out Vector2[] uv, out int[] triangles
        );

        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                int index = x * grid.GetHeight() + y;
                Vector3 quadSize = new Vector3(1, 1) * grid.GetCellSize();

                AvailablePositionsTilemapObject gridObject = grid.GetGridObject(x, y);
                AvailablePositionsTilemapObject.TilemapSprite tilemapSprite = gridObject.GetTilemapSprite();
                Vector2 gridUV00, gridUV11;
                if (tilemapSprite == AvailablePositionsTilemapObject.TilemapSprite.None) {
                    gridUV00 = Vector2.zero;
                    gridUV11 = Vector2.zero;
                    quadSize = Vector3.zero;
                } else {
                    UVCoords uvCoords = uvCoordsDict[tilemapSprite];
                    gridUV00 = uvCoords.uv00;
                    gridUV11 = uvCoords.uv11;
                }

                MeshUtils.AddToMeshArrays(
                    vertices, uv, triangles, index, grid.GetWorldPosition(x, y) + quadSize * .5f,
                    0f, quadSize, gridUV00, gridUV11
                );
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }
}
