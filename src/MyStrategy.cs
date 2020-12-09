using System;
using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Action = Aicup2020.Model.Action;

namespace Aicup2020
{
    public class MyStrategy
    {
        public class ScoreCell
        {
            public Entity? Entity;
            public int ResourceScore;
            // other scores
        }

        private static readonly ScoreCell[,] ScoreMap = new ScoreCell[80, 80];

        private static readonly Entity?[,] Map = new Entity?[80, 80];

        private static readonly Vec2Int MyBase = new Vec2Int(0, 0);

        public static int MyId;

        public static int MyResource;

        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            InitMap(playerView);
            InitScoreMap(playerView);
            MyId = playerView.MyId;
            MyResource = playerView.Players[MyId - 1].Resource;

            var entityActions = new Dictionary<int, EntityAction>();

            int availableLimit = playerView.Entities
                .Count(e =>
                    e.PlayerId == MyId &&
                    e.Active &&
                    (e.EntityType == EntityType.BuilderBase ||
                     e.EntityType == EntityType.MeleeBase ||
                     e.EntityType == EntityType.RangedBase ||
                     e.EntityType == EntityType.House)
                ) * 5;

            int limit = playerView.Entities
                .Count(e =>
                    e.PlayerId == MyId &&
                    (e.EntityType == EntityType.BuilderUnit ||
                     e.EntityType == EntityType.MeleeUnit ||
                     e.EntityType == EntityType.RangedUnit)
                );

            var builders = playerView.Entities
                .Where(e =>
                    e.PlayerId == MyId &&
                    e.EntityType == EntityType.BuilderUnit
                )
                .OrderBy(e => Distance(e.Position, MyBase))
                .ToList();

            foreach (Entity builder in builders)
            {
                //var buildEntityType = turrets > houses && houses < 15 ? EntityType.House : EntityType.Turret;
                var buildEntityType = EntityType.House;
                var buildEntity = playerView.EntityProperties[buildEntityType];

                if (MyResource >= buildEntity.InitialCost && limit >= availableLimit - 10)
                {
                    var buildPosition = new Vec2Int(builder.Position.X + 1, builder.Position.Y);
                    var buildingNeighbors = Neighbors(buildPosition, buildEntity.Size);

                    if (Passable(buildPosition) && PassableLeft(buildPosition, buildEntity.Size) && buildingNeighbors.All(PassableInFuture))
                    {
                        var buildAction = new BuildAction(buildEntityType, buildPosition);
                        entityActions.Add(builder.Id, new EntityAction(null, buildAction, null, null));

                        BuildLeft(buildPosition, buildEntity.Size);
                        MyResource -= buildEntity.InitialCost;
                        break;
                    }
                }
            }


            foreach (Entity entity in playerView.Entities)
            {
                if (entity.PlayerId != MyId)
                {
                    continue;
                }

                var properties = playerView.EntityProperties[entity.EntityType];

                switch (entity.EntityType)
                {
                    case EntityType.Wall:
                        continue;

                    case EntityType.House:
                        continue;

                    case EntityType.BuilderBase:
                        
                        BuildAction? buildAction = null;

                        var builderProperties = playerView.EntityProperties[EntityType.BuilderUnit];
                        if (MyResource >= builderProperties.InitialCost + builders.Count)
                        {
                            var neighbors = Neighbors(entity.Position, properties.Size);
                            foreach (var position in neighbors)
                            {
                                if (Passable(position))
                                {
                                    buildAction = new BuildAction(EntityType.BuilderUnit, position);

                                    BuildLeft(position, 1);
                                    MyResource -= builderProperties.InitialCost + builders.Count;
                                    break;
                                }
                            }
                        }

                        entityActions.Add(entity.Id, new EntityAction(null, buildAction, null, null));
                        continue;

                    case EntityType.BuilderUnit:
                    {
                        SetBuilderRepairAction(entity, entityActions);
                        SetBuilderAttackAction(entity, entityActions);
                        SetMoveAction(entity, entityActions);
                        continue;
                    }

                    case EntityType.MeleeBase:
                        continue;

                    case EntityType.MeleeUnit:
                        continue;

                    case EntityType.RangedBase:
                        continue;

                    case EntityType.RangedUnit:
                        continue;

                    case EntityType.Resource:
                        continue;

                    case EntityType.Turret:
                        EntityType[] unitTargets =
                        {
                            EntityType.MeleeUnit,
                            EntityType.RangedUnit,
                            EntityType.BuilderUnit,
                            EntityType.Turret
                        };
                        var attackAction = new AttackAction(null, new AutoAttack(properties.SightRange, unitTargets));
                        entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
                        continue;
                }
            }

            return new Action(entityActions);
        }

        private static void InitMap(PlayerView playerView)
        {
            for (int y = 0; y < 80; y++)
            {
                for (int x = 0; x < 80; x++)
                {
                    Map[x, y] = null;
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
                            Map[x, y] = entity;
                        }
                    }
                }
                else
                {
                    Map[entity.Position.X, entity.Position.Y] = entity;
                }
            }
        }

        private static void InitScoreMap(PlayerView playerView)
        {
            for (int y = 0; y < 80; y++)
            {
                for (int x = 0; x < 80; x++)
                {
                    ScoreMap[x, y] = new ScoreCell
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
                            ScoreMap[x, y].Entity = entity;
                        }
                    }
                }
                else
                {
                    ScoreMap[entity.Position.X, entity.Position.Y].Entity = entity;
                }
            }

            for (int y = 0; y < 80; y++)
            {
                for (int x = 0; x < 80; x++)
                {
                    if (ScoreMap[x, y].Entity == null)
                    {
                        continue;
                    }

                    Entity entity = ScoreMap[x, y].Entity.Value;
                    if (entity.EntityType == EntityType.Resource)
                    {
                        int leftX = entity.Position.X + 1;
                        int leftY = entity.Position.Y;
                        if (leftX < 80 && ResourceEval(leftX, leftY))
                        {
                            ScoreMap[leftX, leftY].ResourceScore = 1;
                        }

                        int upX = entity.Position.X;
                        int upY = entity.Position.Y - 1;
                        if (upY >= 0 && ResourceEval(upX, upY))
                        {
                            ScoreMap[upX, upY].ResourceScore = 1;
                        }

                        int rightX = entity.Position.X - 1;
                        int rightY = entity.Position.Y;
                        if (rightX >= 0 && ResourceEval(rightX, rightY))
                        {
                            ScoreMap[rightX, rightY].ResourceScore = 1;
                        }

                        int downX = entity.Position.X;
                        int downY = entity.Position.Y + 1;
                        if (downY < 80 && ResourceEval(downX, downY))
                        {
                            ScoreMap[downX, downY].ResourceScore = 1;
                        }
                    }
                }
            }
        }

        private static bool ResourceEval(int x, int y)
        {
            Entity? entity = ScoreMap[x, y].Entity;
            return entity == null ||
                   entity.Value.EntityType == EntityType.BuilderUnit ||
                   entity.Value.EntityType == EntityType.MeleeUnit ||
                   entity.Value.EntityType == EntityType.RangedUnit;
        }

        private static void SetBuilderRepairAction(Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            RepairAction? repairAction = null;

            var leftEntity = entity.Position.X + 1 < 80
                ? Map[entity.Position.X + 1, entity.Position.Y]
                : null;

            var upEntity = entity.Position.Y - 1 >= 0
                ? Map[entity.Position.X, entity.Position.Y - 1]
                : null;

            var rightEntity = entity.Position.X - 1 >= 0
                ? Map[entity.Position.X - 1, entity.Position.Y]
                : null;

            var downEntity = entity.Position.Y + 1 < 80
                ? Map[entity.Position.X, entity.Position.Y + 1]
                : null;

            if (leftEntity != null && !leftEntity.Value.Active)
            {
                repairAction = new RepairAction(leftEntity.Value.Id);
            }
            else if (upEntity != null && !upEntity.Value.Active)
            {
                repairAction = new RepairAction(upEntity.Value.Id);
            }
            else if (rightEntity != null && !rightEntity.Value.Active)
            {
                repairAction = new RepairAction(rightEntity.Value.Id);
            }
            else if (downEntity != null && !downEntity.Value.Active)
            {
                repairAction = new RepairAction(downEntity.Value.Id);
            }

            if (repairAction != null)
            {
                entityActions.Add(entity.Id, new EntityAction(null, null, null, repairAction));
            }
        }

        private static void SetBuilderAttackAction(Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            AttackAction? attackAction = null;

            var leftEntity = entity.Position.X + 1 < 80
                ? Map[entity.Position.X + 1, entity.Position.Y]
                : null;

            var upEntity = entity.Position.Y - 1 >= 0
                ? Map[entity.Position.X, entity.Position.Y - 1]
                : null;

            var rightEntity = entity.Position.X - 1 >= 0
                ? Map[entity.Position.X - 1, entity.Position.Y]
                : null;

            var downEntity = entity.Position.Y + 1 < 80
                ? Map[entity.Position.X, entity.Position.Y + 1]
                : null;

            var leftEval = leftEntity != null && leftEntity.Value.EntityType == EntityType.Resource
                ? 30 / leftEntity.Value.Health
                : int.MinValue;

            var upEval = upEntity != null && upEntity.Value.EntityType == EntityType.Resource
                ? 30 / upEntity.Value.Health
                : int.MinValue;

            var rightEval = rightEntity != null && rightEntity.Value.EntityType == EntityType.Resource
                ? 30 / rightEntity.Value.Health
                : int.MinValue;

            var downEval = downEntity != null && downEntity.Value.EntityType == EntityType.Resource
                ? 30 / downEntity.Value.Health
                : int.MinValue;

            if (leftEntity != null &&
                leftEval != int.MinValue &&
                leftEval >= upEval &&
                leftEval >= rightEval &&
                leftEval >= downEval)
            {
                attackAction = new AttackAction(leftEntity.Value.Id, null);
            }
            else if (upEntity != null &&
                     upEval != int.MinValue &&
                     upEval >= leftEval &&
                     upEval >= rightEval &&
                     upEval >= downEval)
            {
                attackAction = new AttackAction(upEntity.Value.Id, null);
            }
            else if (rightEntity != null &&
                     rightEval != int.MinValue &&
                     rightEval >= upEval &&
                     rightEval >= leftEval &&
                     rightEval >= downEval)
            {
                attackAction = new AttackAction(rightEntity.Value.Id, null);
            }
            else if (downEntity != null &&
                     downEval != int.MinValue &&
                     downEval >= upEval &&
                     downEval >= rightEval &&
                     downEval >= leftEval)
            {
                attackAction = new AttackAction(downEntity.Value.Id, null);
            }

            if (attackAction != null)
            {
                entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
            }
        }

        private static void SetMoveAction(Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            // AStarSearch
            Vec2Int? moveTarget = null;

            var frontier = new PriorityQueue<Vec2Int>();
            frontier.Enqueue(entity.Position, 0);

            //var cameFrom = new Dictionary<Vec2Int, Vec2Int>();
            var costSoFar = new Dictionary<Vec2Int, int>();

            //cameFrom[entity.Position] = entity.Position;
            costSoFar[entity.Position] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (ScoreMap[current.X, current.Y].ResourceScore > 0)
                {
                    moveTarget = current;
                    break;
                }

                foreach (Vec2Int next in Neighbors(current))
                {
                    int newCost = costSoFar[current] + 1;
                    if (Passable(next) && (!costSoFar.ContainsKey(next) || newCost < costSoFar[next]))
                    {
                        costSoFar[next] = newCost;
                        frontier.Enqueue(next, newCost);
                        //cameFrom[next] = current;
                    }
                }
            }

            //if (moveTarget != null)
            //{
            //    Map[entity.Position.X, entity.Position.Y] = null;
            //    Map[moveTarget.X, moveTarget.Y] = entity;
            //}

            var moveAction = moveTarget != null ? new MoveAction(moveTarget.Value, false, false) : (MoveAction?) null;
            entityActions.Add(entity.Id, new EntityAction(moveAction, null, null, null));
        }

        public static List<Vec2Int> Neighbors(Vec2Int p)
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

        public static List<Vec2Int> Neighbors(Vec2Int p, int size)
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

        public static bool Passable(Vec2Int p)
        {
            return ScoreMap[p.X, p.Y].Entity == null;
        }

        public static bool PassableInFuture(Vec2Int p)
        {
            var entity = ScoreMap[p.X, p.Y].Entity;

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

                    if (ScoreMap[p.X + x, p.Y + y].Entity != null)
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
                    ScoreMap[p.X + x, p.Y + y].Entity = new Entity();
                }
            }
        }

        public static int Distance(Vec2Int p1, Vec2Int p2) => Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);

        public static int Distance(Vec2Int p1, int x, int y) => Math.Abs(p1.X - x) + Math.Abs(p1.Y - y);

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }
    }
}