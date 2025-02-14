﻿using System;
using System.Collections.Generic;
using Aicup2020.Model;

namespace Aicup2020.MyModel
{
    public static class Vec2IntExtensions
    {
        public static List<Vec2Int> Neighbors(this Vec2Int p)
        {
            var neighbors = new List<Vec2Int>();

            // up
            if (p.Y - 1 >= 0)
            {
                neighbors.Add(new Vec2Int(p.X, p.Y - 1));
            }

            // right
            if (p.X - 1 >= 0)
            {
                neighbors.Add(new Vec2Int(p.X - 1, p.Y));
            }

            // down
            if (p.Y + 1 < 80)
            {
                neighbors.Add(new Vec2Int(p.X, p.Y + 1));
            }

            // left
            if (p.X + 1 < 80)
            {
                neighbors.Add(new Vec2Int(p.X + 1, p.Y));
            }

            return neighbors;
        }

        public static List<Vec2Int> Neighbors(this Vec2Int p, int size)
        {
            var neighbors = new List<Vec2Int>();

            // up
            if (p.Y - 1 >= 0)
            {
                for (int i = 0; i < size; i++)
                {
                    if (p.X + i < 80)
                    {
                        neighbors.Add(new Vec2Int(p.X + i, p.Y - 1));
                    }
                }
            }

            // right
            if (p.X - 1 >= 0)
            {
                for (int i = 0; i < size; i++)
                {
                    if (p.Y + i < 80)
                    {
                        neighbors.Add(new Vec2Int(p.X - 1, p.Y + i));
                    }
                }
            }

            // down
            if (p.Y + size < 80)
            {
                for (int i = 0; i < size; i++)
                {
                    if (p.X + i < 80)
                    {
                        neighbors.Add(new Vec2Int(p.X + i, p.Y + size));
                    }
                }
            }

            // left
            if (p.X + size < 80)
            {
                for (int i = 0; i < size; i++)
                {
                    if (p.Y + i < 80)
                    {
                        neighbors.Add(new Vec2Int(p.X + size, p.Y + i));
                    }
                }
            }

            return neighbors;
        }

        public static List<Vec2Int> Diagonals(this Vec2Int p, int size)
        {
            var diagonals = new List<Vec2Int>();

            if (p.X - 1 >= 0 && p.Y - 1 >= 0)
            {
                diagonals.Add(new Vec2Int(p.X - 1, p.Y - 1));
            }

            if (p.X + size < 80 && p.Y - 1 >= 0)
            {
                diagonals.Add(new Vec2Int(p.X + size, p.Y - 1));
            }

            if (p.X + size < 80 && p.Y + size < 80)
            {
                diagonals.Add(new Vec2Int(p.X + size, p.Y + size));
            }

            if (p.X - 1 >= 0 && p.Y + size < 80)
            {
                diagonals.Add(new Vec2Int(p.X - 1, p.Y + size));
            }

            return diagonals;
        }

        public static List<Vec2Int> Range(this Vec2Int p, int size)
        {
            var radius = new List<Vec2Int>();

            for (int x = p.X - size; x <= p.X + size; x++)
            {
                for (int y = p.Y - size; y <= p.Y + size; y++)
                {
                    if (!InBounds(x, y))
                    {
                        continue;
                    }

                    int distance = p.Distance(x, y);
                    if (distance > 0 && distance <= size)
                    {
                        radius.Add(new Vec2Int(x, y));
                    }
                }
            }

            return radius;
        }

        public static List<Vec2Int> Range(this Vec2Int p, int size, int minSize)
        {
            var radius = new List<Vec2Int>();

            for (int x = p.X - size; x <= p.X + size; x++)
            {
                for (int y = p.Y - size; y <= p.Y + size; y++)
                {
                    if (!InBounds(x, y))
                    {
                        continue;
                    }

                    int distance = p.Distance(x, y);
                    if (distance > minSize && distance <= size)
                    {
                        radius.Add(new Vec2Int(x, y));
                    }
                }
            }

            return radius;
        }

        private static bool InBounds(Vec2Int p) => p.X >= 0 && p.Y >= 0 && p.X < 80 && p.Y < 80;

        private static bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < 80 && y < 80;

        // Neighbors with left down corner
        public static List<Vec2Int> BuildPositions(this Vec2Int p, int size)
        {
            var buildPositions = new List<Vec2Int>();

            // up
            if (p.Y - size >= 0)
            {
                for (int i = 0; i < size; i++)
                {
                    if (p.X - i >= 0)
                    {
                        buildPositions.Add(new Vec2Int(p.X - i, p.Y - size));
                    }
                }
            }

            // right
            if (p.X - size >= 0)
            {
                for (int i = 0; i < size; i++)
                {
                    if (p.Y - i >= 0)
                    {
                        buildPositions.Add(new Vec2Int(p.X - size, p.Y - i));
                    }
                }
            }

            // down
            if (p.Y + 1 < 80)
            {
                for (int i = 0; i < size; i++)
                {
                    if (p.X - i >= 0)
                    {
                        buildPositions.Add(new Vec2Int(p.X - i, p.Y + 1));
                    }
                }
            }

            // left
            if (p.X + 1 < 80)
            {
                for (int i = 0; i < size; i++)
                {
                    if (p.Y - i >= 0)
                    {
                        buildPositions.Add(new Vec2Int(p.X + 1, p.Y - i));
                    }
                }
            }

            return buildPositions;
        }

        public static int Distance(this Vec2Int p1, Vec2Int p2) => Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);

        public static int Distance(this Vec2Int p1, int x, int y) => Math.Abs(p1.X - x) + Math.Abs(p1.Y - y);
    }
}