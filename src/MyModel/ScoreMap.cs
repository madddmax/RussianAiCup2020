using Aicup2020.Model;

namespace Aicup2020.MyModel
{
    public static class ScoreMap
    {
        private static readonly ScoreCell[,] Map = new ScoreCell[80, 80];

        public static ScoreCell Get(Vec2Int p) => Map[p.X, p.Y];

        public static void InitMap(PlayerView playerView)
        {
            for (int y = 0; y < 80; y++)
            {
                for (int x = 0; x < 80; x++)
                {
                    Map[x, y] = new ScoreCell
                    {
                        Entity = null,
                        ResourceScore = 0
                    };
                }
            }

            foreach (Entity entity in playerView.Entities)
            {
                var properties = playerView.EntityProperties[entity.EntityType];
                if (properties.Size > 1)
                {
                    for (int y = entity.Position.Y; y < entity.Position.Y + properties.Size; y++)
                    {
                        for (int x = entity.Position.X; x < entity.Position.X + properties.Size; x++)
                        {
                            Map[x, y].Entity = entity;
                        }
                    }
                }
                else
                {
                    Map[entity.Position.X, entity.Position.Y].Entity = entity;
                }
            }

            for (int y = 0; y < 80; y++)
            {
                for (int x = 0; x < 80; x++)
                {
                    if (Map[x, y].Entity == null)
                    {
                        continue;
                    }

                    Entity entity = Map[x, y].Entity.Value;
                    if (entity.EntityType == EntityType.Resource)
                    {
                        int leftX = entity.Position.X + 1;
                        int leftY = entity.Position.Y;
                        if (leftX < 80 && PassableInFuture(leftX, leftY))
                        {
                            Map[leftX, leftY].ResourceScore = 1;
                        }

                        int upX = entity.Position.X;
                        int upY = entity.Position.Y - 1;
                        if (upY >= 0 && PassableInFuture(upX, upY))
                        {
                            Map[upX, upY].ResourceScore = 1;
                        }

                        int rightX = entity.Position.X - 1;
                        int rightY = entity.Position.Y;
                        if (rightX >= 0 && PassableInFuture(rightX, rightY))
                        {
                            Map[rightX, rightY].ResourceScore = 1;
                        }

                        int downX = entity.Position.X;
                        int downY = entity.Position.Y + 1;
                        if (downY < 80 && PassableInFuture(downX, downY))
                        {
                            Map[downX, downY].ResourceScore = 1;
                        }
                    }
                }
            }
        }

        public static bool Passable(Vec2Int p)
        {
            return Map[p.X, p.Y].Entity == null;
        }

        public static bool PassableInFuture(Vec2Int p) => PassableInFuture(p.X, p.Y);

        public static bool PassableInFuture(int x, int y)
        {
            Entity? entity = Map[x, y].Entity;
            return entity == null ||
                   entity.Value.EntityType == EntityType.BuilderUnit ||
                   entity.Value.EntityType == EntityType.MeleeUnit ||
                   entity.Value.EntityType == EntityType.RangedUnit;
        }

        public static bool PassableLeft(Vec2Int p, int size)
        {
            for (int x = 0; x < size; x++)
            {
                if (p.X + x >= 80)
                {
                    return false;
                }

                for (int y = 0; y < size; y++)
                {
                    if (p.Y + y >= 80)
                    {
                        return false;
                    }

                    if (Map[p.X + x, p.Y + y].Entity != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void BuildLeft(Vec2Int p, int size)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Map[p.X + x, p.Y + y].Entity = new Entity();
                }
            }
        }
    }
}