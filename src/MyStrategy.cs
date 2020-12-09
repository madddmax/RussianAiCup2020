using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Action = Aicup2020.Model.Action;

namespace Aicup2020
{
    public class MyStrategy
    {
        public const int MaxBuildersCount = 100;

        public class ScoreCell
        {
            public Entity? Entity;
            public int ResourceScore;
            // other scores
        }

        public static readonly ScoreCell[,] ScoreMap = new ScoreCell[80, 80];

        private static readonly Vec2Int MyBase = new Vec2Int(0, 0);

        public static int MyId;

        public static int MyResource;

        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
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


            var buildEntityType = limit < 50 ? EntityType.House : EntityType.Turret;
            var builders = playerView.Entities
                .Where(e =>
                    e.PlayerId == MyId &&
                    e.EntityType == EntityType.BuilderUnit
                )
                
                .ToList();

            builders = buildEntityType == EntityType.House
                ? builders.OrderBy(e => e.Position.Distance(MyBase)).ToList()
                : builders.OrderByDescending(e => e.Position.Distance(MyBase)).ToList();

            foreach (Entity builder in builders)
            {
                //var buildEntityType = EntityType.House;
                var buildEntity = playerView.EntityProperties[buildEntityType];

                if (MyResource >= buildEntity.InitialCost)
                {
                    var buildPosition = new Vec2Int(builder.Position.X + 1, builder.Position.Y);
                    var buildingNeighbors = buildPosition.Neighbors(buildEntity.Size);

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
                        if (MyResource >= builderProperties.InitialCost + builders.Count && builders.Count <= MaxBuildersCount)
                        {
                            var neighbors = entity.Position.Neighbors(properties.Size);
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
                            EntityType.Turret,
                            EntityType.House,
                            EntityType.BuilderBase,
                            EntityType.MeleeBase,
                            EntityType.RangedBase,
                            EntityType.Wall
                        };
                        var attackAction = new AttackAction(null, new AutoAttack(properties.SightRange, unitTargets));
                        entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
                        continue;
                }
            }

            return new Action(entityActions);
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

            var neighbors = entity.Position.Neighbors();
            foreach (var target in neighbors)
            {
                var targetEntity = ScoreMap[target.X, target.Y].Entity;
                if (targetEntity?.Active == false)
                {
                    var repairAction = new RepairAction(targetEntity.Value.Id);
                    entityActions.Add(entity.Id, new EntityAction(null, null, null, repairAction));
                    break;
                }
            }
        }

        private static void SetBuilderAttackAction(Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            var neighbors = entity.Position.Neighbors();
            foreach (var target in neighbors)
            {
                var targetEntity = ScoreMap[target.X, target.Y].Entity;
                if (targetEntity?.EntityType == EntityType.Resource)
                {
                    var attackAction = new AttackAction(targetEntity.Value.Id, null);
                    entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
                    break;
                }
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

                foreach (Vec2Int next in current.Neighbors())
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

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }
    }
}