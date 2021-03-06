using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class TileGrid : MonoBehaviour {

    private Vector2Int gridSize = new Vector2Int(50, 50);
    private float outlinePercent = 0.05f;
    private int movementRange = 20;

    public Tile[,] grid;
    public List<Tile> selectedTiles = new List<Tile>(2);
    public Tile[] selectedArray = new Tile[2];

    public Tile selectedTile;

    public Transform tilePrefab;

    public LayerMask nonTraversibleMask;
    public float tileRadius = 1f;
    public float tileDiameter;

    
    private void Awake() {
        tileDiameter = tileRadius * 2;
        PopulateGrid();
    }


    void PopulateGrid() {
        grid = new Tile[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++) {
            for (int y = 0; y < gridSize.y; y++) {

                Vector3 tilePosition = new Vector3(-gridSize.x / 2 + 0.5f + x, 0, -gridSize.y / 2 + 0.5f + y);
                Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90));
                Tile currentTile = newTile.GetComponent<Tile>();
                currentTile.OnSelectedEvent.AddListener(StoreSelection);
                currentTile.OnDeselectedEvent.AddListener(RemoveSelection);
                newTile.parent = gameObject.GetComponent<TileGrid>().transform;
                newTile.localScale = Vector3.one * (1 - outlinePercent);

                bool traversible = (!Physics.CheckSphere(newTile.position, tileRadius - 0.04f, nonTraversibleMask));

                grid[x, y] = newTile.GetComponent<Tile>();
                grid[x, y].gridX = x;
                grid[x, y].gridY = y;
                grid[x, y].isTraversible = traversible;
            }
        }
    }


    // created this GetTraversibleNeighbors method to get around the diagonal-move-through-wall-bug
    // checks the cardinal directions for obstacles, then removes both those and diagonal tiles from viable moves if obstacles found
    // nonTraversibleNeighbors is a Set so that I don't have to worry about duplicates, I love sets
    public List<Tile> GetTraversibleNeighbors(Tile tile) {
        List<Tile> neighbors = GetNeighbors(tile);
        HashSet<Tile> nonTraversibleNeighbors = new HashSet<Tile>();

        foreach (Tile neighbor in neighbors) {
            if (!neighbor.isTraversible) {
                if (neighbor.gridX == tile.gridX && neighbor.gridY != tile.gridY) {
                    List<Tile> sameYNeighbors = neighbors.Where(neighborTile => neighborTile.gridY == neighbor.gridY).ToList();
                    nonTraversibleNeighbors.UnionWith(sameYNeighbors);
                }
                else if (neighbor.gridY == tile.gridY && neighbor.gridX != tile.gridX) {
                    List<Tile> sameXNeighbors = neighbors.Where(neighborTile => neighborTile.gridX == neighbor.gridX).ToList();
                    nonTraversibleNeighbors.UnionWith(sameXNeighbors);
                }
                else {
                    nonTraversibleNeighbors.Add(neighbor);
                }
            }
        }
        return neighbors.Except(nonTraversibleNeighbors).ToList();

    }

    public List<Tile> GetNeighbors(Tile tile) {
        List<Tile> neighbors = new List<Tile>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) {
                    continue;
                }
                int checkX = tile.gridX + x;
                int checkY = tile.gridY + y;

                if (checkX >= 0 && checkX < gridSize.x && checkY >= 0 && checkY < gridSize.y) {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    // these gizmos are used in Scene view to test obstacle collision detection and to show the size of the grid
    private void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize.x, 0.5f, gridSize.y));

        if (grid != null) {

            foreach (Tile tile in grid) {
                Gizmos.color = (tile.isTraversible) ? Color.white : Color.red;

                Gizmos.DrawCube(tile.transform.position, Vector3.one * (tileDiameter - 0.3f));
            }

        }
    }

    // StoreSelection and RemoveSelection handle the 2 possible selected tiles for feeding into the pathfinding later
    private void StoreSelection(ISelectable selectable) {
        Tile selectedTile = ((Tile)selectable);
        if (selectedTiles.Count >= 2) {
            Tile poppedTile = selectedTiles.Last();
            selectedTiles.RemoveAt(selectedTiles.Count - 1);
            poppedTile.DeselectTile();
        }

        if (selectedTiles.Count == 1 && (selectedTile.isInRange)) {
            selectedTile.SelectTile();
            selectedTiles.Add(selectedTile);
        }

        if (selectedTiles.Count == 0) {
            selectedTile.SelectTile();
            selectedTiles.Add(selectedTile);
            DrawMovementRange(selectedTile);
        }
    }

    private void RemoveSelection(ISelectable selectable) {
        Tile selectedTile = (Tile)selectable;
        selectedTile.DeselectTile();
        selectedTiles.Remove(selectedTile);
        if (selectedTiles.Count == 0) {
            ResetGrid();
        }
    }

    // method for showing how far the player can move - it's a breadth first search, which I think gives a nice visualization
    private void DrawMovementRange(Tile tile) {
        List<Tile> visited = new List<Tile>();
        HashSet<Tile> notVisited = new HashSet<Tile>(GetTraversibleNeighbors(tile));
        List<Tile> via = new List<Tile>();

        visited.Add(tile);

        for (int i = 0; i < movementRange; i++) {
            via.AddRange(notVisited.Except(visited));
            foreach (Tile viaTile in via) {
                visited.Add(viaTile);
                notVisited.UnionWith(GetTraversibleNeighbors(viaTile));
            }
            via = new List<Tile>();
        }
        foreach (Tile visitedTile in visited) {
            if (visitedTile != tile) {
                MeshRenderer meshRenderer = visitedTile.GetComponent<MeshRenderer>();
                visitedTile.SetInRange();
            }
        }
    }

    // cleaning the grid up
    public void ResetGrid() {
        selectedTiles.Clear();
        foreach (Tile tile in grid) {
            tile.ResetTile();
        }
    }
}
