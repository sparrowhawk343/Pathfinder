using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Pathfinder : MonoBehaviour
{
    List<Tile> open = new List<Tile>();
    List<Tile> closed = new List<Tile>();
    //Queue<Tile> open = new Queue<Tile>();

    Tile currentTile;
    //Tile lowestTile;

    const int baseDiagonalCost = 14;
    const int baseStraightCost = 10;

    public TileGrid grid;

    public List<Tile> FindPath(Tile start, Tile target) {

        currentTile = null;
        open.Add(start);

        // set currentTile to the tile in the open list with the lowest fCost
        while (currentTile != target) {
            open.Sort((tileA, tileB) => {
                if (tileA.fCost <= tileB.fCost) {
                    return 1;
                }
                else {
                    return -1;
                }
            });
            currentTile = open.LastOrDefault();

            //    lowestTile = null;
            //foreach (Tile tile in open) {
            //    if (lowestTile != null && tile.fCost <= lowestTile.fCost) {
            //         currentTile = lowestTile;
            //    }
            //    else {
            //        lowestTile = tile;
            //    }

            //}

            // this null check is to prevent running out of tiles when trying to set an inaccesible
            // tile as target (should probably prevent this from happening in some other way later)
            if (currentTile == null) {
                Debug.Log("Tried to access inaccessible tile");
                return null;
            }
            // remove currentTile from open and add it to closed
            open.Remove(currentTile);
            closed.Add(currentTile);

            // if currentTile is same as target, we have a path
            if (currentTile == target) {
                break;
            }

            // go through all neighbor tiles that the current tile has, and check if they are traversible
            foreach (Tile neighborTile in grid.GetNeighbors(currentTile)) {
                if (!neighborTile.isTraversible || closed.Contains(neighborTile)) {
                    open.Remove(neighborTile);
                    continue;
                }

                int newMovementCostToNeighbor = currentTile.gCost + GetDistance(currentTile, neighborTile);
                if (newMovementCostToNeighbor < neighborTile.gCost || !open.Contains(neighborTile)) {
                    neighborTile.gCost = newMovementCostToNeighbor;
                    neighborTile.hCost = GetDistance(neighborTile, target);
                    neighborTile.parentTile = currentTile;
                    if (!open.Contains(neighborTile)) {
                        open.Add(neighborTile);
                    }
                }
            }
        }
        // here is where we return the path (outside the while loop, which has the same check as the path found check, basically)
        return RetracePath(start, target);
    }

    List<Tile> RetracePath(Tile startTile, Tile endTile) {
        List<Tile> path = new List<Tile>();
        Tile currentTile = endTile;

        while (currentTile != startTile) {
            path.Add(currentTile);
            currentTile = currentTile.parentTile;
        }
        path.Reverse();
        foreach (Tile tile in path) {
            MeshRenderer meshRenderer = tile.GetComponent<MeshRenderer>(); 
            meshRenderer.material.color = Color.blue;
            //if (currentTile.parentTile != null) {
            //    Debug.DrawLine(currentTile.parentTile.transform.position, currentTile.transform.position, Color.black);
            //}
        }
        return path;
    }

    int GetDistance(Tile tileA, Tile tileB) {
        int distanceX = Mathf.Abs(tileA.gridX - tileB.gridX);
        int distanceY = Mathf.Abs(tileA.gridY - tileB.gridY);

        if (distanceX > distanceY) {
            return baseDiagonalCost * distanceY + baseStraightCost * (distanceX - distanceY);
        }
        else {
            return baseDiagonalCost * distanceX + baseStraightCost * (distanceY - distanceX);
        }

    }

    public void TestPathFinder() {
        FindPath(grid.selectedArray[0], grid.selectedArray[1]);
    }

    public void ResetPathFinder() {
        open.Clear();
        closed.Clear();
        grid.ResetGrid();
    }


    // TODO:
    // fix pathfinding algorithm (it is currently doing what looks like a longest path solution)
    // mouse hovering interaction
    // implement path line
    // implement pathfinding range

    // implement player
    // implement action points
    // implement movement (walk/dash)
    // optimize algorithm with heap

    // implement vfx/highlights/gizmos for tile
}
