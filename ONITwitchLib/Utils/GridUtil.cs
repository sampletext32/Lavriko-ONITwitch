using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using ONITwitchLib.Logger;
using UnityEngine;

namespace ONITwitchLib.Utils;

public static class GridUtil
{
	public static readonly List<Direction> CellNeighbors = new()
	{
		Direction.Down,
		Direction.Right,
		Direction.Left,
		Direction.Up,
	};

	public static IEnumerable<int> GetNeighborsInBounds(int cell)
	{
		return CellNeighbors.Select(dir => Grid.GetCellInDirection(cell, dir))
			.Where(n => Grid.IsValidCell(n) && Grid.AreCellsInSameWorld(cell, n));
	}

	public static bool IsCellFoundationEmpty(int cell)
	{
		var isFoundation = Grid.Foundation[cell];
		var validCell = Grid.IsValidBuildingCell(cell);
		var isDiggable = Grid.Element[cell].hardness < byte.MaxValue;
		return !isFoundation && validCell && isDiggable;
	}

	public static IEnumerable<int> GetNeighborsWithBuildingClearance(int cell)
	{
		return GetNeighborsInBounds(cell).Where(IsCellFoundationEmpty);
	}

	public static bool IsCellEmpty(int cell)
	{
		var isFoundation = Grid.Foundation[cell];
		var validCell = Grid.IsValidBuildingCell(cell);
		var isOpen = !Grid.IsSolidCell(cell);
		return !isFoundation && validCell && isOpen;
	}

	public const int NearestEmptyCellDepth = 40;

	public static int NearestEmptyCell(int baseCell)
	{
		var emptyCell = GameUtil.FloodFillFind<object>(
			(cell, _) => IsCellEmpty(cell) && Grid.AreCellsInSameWorld(cell, baseCell),
			null,
			baseCell,
			NearestEmptyCellDepth,
			false,
			false
		);

		if (!Grid.IsValidBuildingCell(emptyCell))
		{
			Log.Warn($"Unable to find empty cell close to {baseCell} output {emptyCell}");
		}

		return emptyCell;
	}

	public static int FindCellWithCavityClearance(int baseCell)
	{
		var emptyCell = GameUtil.FloodFillFind<object>(
			(cell, _) =>
			{
				var cellEmpty = IsCellEmpty(cell);
				var neighborsValid = GetNeighborsInBounds(cell).All(IsCellEmpty);
				return cellEmpty && neighborsValid;
			},
			null,
			baseCell,
			NearestEmptyCellDepth,
			false,
			false
		);

		if (!Grid.IsValidCell(emptyCell))
		{
			Log.Warn($"Unable to find valid cell with clearance at start cell {baseCell}, output {emptyCell}");
		}

		return emptyCell;
	}

	public static int FindCellWithFoundationClearance(int baseCell)
	{
		var clusterIdx = ClusterManager.Instance.activeWorldId;
		var emptyCell = GameUtil.FloodFillFind<object>(
			(cell, _) =>
			{
				var cellEmpty = IsCellFoundationEmpty(cell);
				var neighborsValid = GetNeighborsInBounds(cell).All(IsCellFoundationEmpty);
				return cellEmpty && neighborsValid;
			},
			null,
			baseCell,
			NearestEmptyCellDepth,
			false,
			false
		);

		if (!Grid.IsValidCell(emptyCell))
		{
			Log.Warn($"Unable to find valid cell with building clearance at start cell {baseCell}, output {emptyCell}");
		}

		return emptyCell;
	}

	/// <summary>
	/// Collects cells by flood fill but does not favor one direction first.
	/// Does not clear the set of found cells if too many cells match, it just stops early.
	/// </summary>
	/// <param name="startCell">The initial cell to start searching from.</param>
	/// <param name="cellValid">The function called to determine whether a visited cell is valid.</param>
	/// <param name="maxSize">The maximum number of cells to collect.</param>
	/// <param name="invalidVisitedCells">All of the cells that were visited that were not considered valid.</param>
	/// <returns>A HashSet containing the cells that were valid.</returns>
	public static HashSet<int> FloodCollectCells(
		int startCell,
		Func<int, bool> cellValid,
		int maxSize = 1000,
		HashSet<int> invalidVisitedCells = null
	)
	{
		var result = new HashSet<int>();

		const int cellsDefaultCapacity = 64;
		var visitedCells = new HashSet<int>();
		var toVisit = new Queue<int>(cellsDefaultCapacity);
		toVisit.Enqueue(startCell);

		// Once there are no more cells that can be visited, or the maximum number of cells has been reached, stop
		while ((toVisit.Count > 0) && (result.Count < maxSize))
		{
			var visitingCell = toVisit.Dequeue();
			visitedCells.Add(visitingCell);

			var isValidCell = cellValid(visitingCell);
			if (isValidCell)
			{
				result.Add(visitingCell);

				// Only ever queue the cells that have not yet been visited
				var neighbors = GetNeighborsInBounds(visitingCell).Where(cell => !visitedCells.Contains(cell));
				foreach (var unvisitedNeighbor in neighbors)
				{
					toVisit.Enqueue(unvisitedNeighbor);
				}
			}
			else
			{
				invalidVisitedCells?.Add(visitingCell);
			}
		}

		return result;
	}

	public static IEnumerable<int> IterateCellRegion(Game.SimActiveRegion region)
	{
		return IterateCellRegion(region.region.first, region.region.second);
	}

	public static IEnumerable<int> IterateCellRegion(Vector2I min, Vector2I max)
	{
		for (var y = min.y; y < max.y; y++)
		{
			for (var x = min.x; x < max.x; x++)
			{
				yield return Grid.XYToCell(x, y);
			}
		}
	}

	private static readonly AccessTools.FieldRef<Game, List<Game.SimActiveRegion>> SimRegionsDelegate =
		AccessTools.FieldRefAccess<Game, List<Game.SimActiveRegion>>(
			AccessTools.DeclaredField(typeof(Game), "simActiveRegions")
		);

	public static IEnumerable<int> ActiveSimCells()
	{
		var activeSimRegions = SimRegionsDelegate(Game.Instance);
		return activeSimRegions.SelectMany(IterateCellRegion);
	}

	[CanBeNull]
	public static Vector2? LineSegmentIntersectsBox(Vector2 start, Vector2 end, Vector2I boxStart)
	{
		var boxEnd = boxStart + Vector2I.one;

		var x1t = (boxStart.x - start.x) / (end.x - start.x);
		var x2t = (boxEnd.x - start.x) / (end.x - start.x);
		// swap such that 2 is always greater than or equal to 1
		if (x1t > x2t)
		{
			(x2t, x1t) = (x1t, x2t);
		}

		var y1t = (boxStart.y - start.y) / (end.y - start.y);
		var y2t = (boxEnd.y - start.y) / (end.y - start.y);
		// swap such that 2 is always greater than or equal to 1
		if (y1t > y2t)
		{
			(y2t, y1t) = (y1t, y2t);
		}

		// if the second x axis is crossed before the first y, or the second y before the first x, then the line is outside the cell
		if ((x2t < y1t) || (y2t < x1t))
		{
			return null;
		}

		var tmin = Mathf.Max(x1t, y1t);
		var tmax = Mathf.Min(x2t, y2t);
		if (tmin > tmax)
		{
			(tmax, tmin) = (tmin, tmax);
		}

		var dir = end - start;
		// return the first collision that is in bounds of the segment
		if (tmin is >= 0 and <= 1)
		{
			return start + tmin * dir;
		}

		if (tmax is >= 0 and <= 1)
		{
			return start + tmax * dir;
		}

		return null;
	}

	// Finds a cell near the input cell that can have the passed building built	
	// returns -1 if such a cell cannot be found
	public static int FindCellOpenToBuilding(
		int cell,
		[NotNull] BuildingDef building,
		Orientation orientation = Orientation.Neutral
	)
	{
		var foundCell = GameUtil.FloodFillFind<object>(
			(cell, _) => building.IsValidPlaceLocation(null, cell, orientation, false, out var _) &&
						 building.IsValidBuildLocation(null, cell, orientation, false, out var _),
			null,
			cell,
			1_000,
			false,
			false
		);

		return foundCell;
	}
}
