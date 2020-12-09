using System;
using System.Collections.Generic;
using Aicup2020.Model;

namespace Aicup2020
{
    public static class Vec2IntExtensions
    {
        public static List<Vec2Int> Neighbors(this Vec2Int p)
        {
            var neighbors = new List<Vec2Int>();

            // left
            if (p.X + 1 < 80)
            {
                neighbors.Add(new Vec2Int(p.X + 1, p.Y));
            }

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

            return neighbors;
        }

        public static List<Vec2Int> Neighbors(this Vec2Int p, int size)
        {
            var neighbors = new List<Vec2Int>();

            // left
            if (p.X + size < 80)
            {
                for (int i = 0; i < size; i++)
                {
                    neighbors.Add(new Vec2Int(p.X + size, p.Y + i));
                }
            }

            // up
            if (p.Y - 1 >= 0)
            {
                for (int i = 0; i < size; i++)
                {
                    neighbors.Add(new Vec2Int(p.X + i, p.Y - 1));
                }
            }

            // right
            if (p.X - 1 >= 0)
            {
                for (int i = 0; i < size; i++)
                {
                    neighbors.Add(new Vec2Int(p.X - 1, p.Y + i));
                }
            }

            // down
            if (p.Y + size < 80)
            {
                for (int i = 0; i < size; i++)
                {
                    neighbors.Add(new Vec2Int(p.X + i, p.Y + size));
                }
            }

            return neighbors;
        }

        public static int Distance(this Vec2Int p1, Vec2Int p2) => Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
    }
}