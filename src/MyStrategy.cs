using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Aicup2020.MyModel;

namespace Aicup2020
{
    public class MyStrategy
    {
        public const int MaxBuildersCount = 100;

        public const int MaxKnightsCount = 20;

        public const int MaxRangersCount = 100;

        public const int DangerDistance = 30;

        public const int MaxSearchMove = 200;

        private static readonly Vec2Int MyBase = new Vec2Int(0, 0);

        public static int MyId;

        public static int MyResource;

        public static Entity? DistantResource;

        public static Entity? DistantBuilder;

        public static Entity? NearestEnemy;

        public static bool IsDanger;

        public static readonly EntityType[] UnitTargets =
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

        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            ScoreMap.InitMap(playerView);
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

            int buildedHouse = playerView.Entities
                .Count(e =>
                    e.PlayerId == MyId &&
                    !e.Active &&
                    e.EntityType == EntityType.House
                );

            int buildedTurret = playerView.Entities
                .Count(e =>
                    e.PlayerId == MyId &&
                    !e.Active &&
                    e.EntityType == EntityType.Turret
                );

            var builders = playerView.Entities
                .Where(e =>
                    e.PlayerId == MyId &&
                    e.EntityType == EntityType.BuilderUnit
                )
                .ToList();

            var knights = playerView.Entities
                .Where(e =>
                    e.PlayerId == MyId &&
                    e.EntityType == EntityType.MeleeUnit
                )
                .ToList();

            var rangers = playerView.Entities
                .Where(e =>
                    e.PlayerId == MyId &&
                    e.EntityType == EntityType.RangedUnit
                )
                .ToList();

            DistantResource = playerView.Entities
                .Where(e => e.EntityType == EntityType.Resource)
                .OrderByDescending(e => e.Position.Distance(MyBase))
                .FirstOrDefault();

            DistantBuilder = playerView.Entities
                .Where(e =>
                    e.PlayerId == MyId &&
                    e.EntityType == EntityType.BuilderUnit
                )
                .OrderByDescending(e => e.Position.Distance(MyBase))
                .FirstOrDefault();

            var comparedPosition = DistantBuilder?.Position ?? MyBase;
            NearestEnemy = playerView.Entities
                .Where(e =>
                    e.PlayerId != MyId &&
                    e.EntityType != EntityType.Resource
                )
                .OrderBy(e => e.Position.Distance(comparedPosition))
                .FirstOrDefault();

            IsDanger = NearestEnemy.Value.Position.Distance(comparedPosition) <= DangerDistance;

            //var buildEntityType = limit < 50 ? EntityType.House : EntityType.Turret;
            EntityType? buildEntityType = null;
            //if (NearestEnemy != null && IsDanger)
            //{
            //    buildEntityType = EntityType.Turret;
            //}

            if (limit + 10 > availableLimit && buildedHouse < 2)
            {
                buildEntityType = EntityType.House;
            }

            //if (buildEntityType != null && buildEntityType == EntityType.Turret && buildedTurret == 0)
            //{
            //    builders = builders.OrderBy(e => e.Position.Distance(NearestEnemy.Value.Position)).ToList();
            //}

            if (buildEntityType != null && buildEntityType == EntityType.House)
            {
                builders = builders.OrderBy(e => e.Position.Distance(MyBase)).ToList();
            }

            if (buildEntityType != null)
            {
                foreach (Entity builder in builders)
                {
                    var buildEntity = playerView.EntityProperties[buildEntityType.Value];

                    if (MyResource >= buildEntity.InitialCost && builder.Position.X + 1 < 80)
                    {
                        var buildPosition = new Vec2Int(builder.Position.X + 1, builder.Position.Y);
                        var buildingNeighbors = buildPosition.Neighbors(buildEntity.Size);

                        if (ScoreMap.Passable(buildPosition) && ScoreMap.PassableLeft(buildPosition, buildEntity.Size) && buildingNeighbors.All(ScoreMap.PassableInFuture))
                        {
                            var buildAction = new BuildAction(buildEntityType.Value, buildPosition);
                            entityActions.Add(builder.Id, new EntityAction(null, buildAction, null, null));

                            ScoreMap.BuildLeft(buildPosition, buildEntity.Size);
                            MyResource -= buildEntity.InitialCost;
                            break;
                        }
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
                    {
                        var unitProperties = playerView.EntityProperties[EntityType.BuilderUnit];
                        int unitCost = unitProperties.InitialCost + builders.Count;

                        if ((IsDanger && MyResource >= unitCost * 3) ||
                            (!IsDanger && MyResource >= unitCost) &&
                            builders.Count <= MaxBuildersCount)
                        {
                            SetBuildUnitAction(entity, EntityType.BuilderUnit, unitCost, entityActions);
                        }

                        if (!entityActions.ContainsKey(entity.Id))
                        {
                            entityActions.Add(entity.Id, new EntityAction(null, null, null, null));
                        }

                        continue;
                    }
                    case EntityType.BuilderUnit:
                    {
                        SetBuilderRepairAction(playerView, entity, entityActions);
                        SetBuilderAttackAction(entity, entityActions);

                        var target = DistantResource?.Position;
                        SetMoveAction(entity, target, entityActions, false);
                        continue;
                    }

                    case EntityType.MeleeBase:
                    {
                        var unitProperties = playerView.EntityProperties[EntityType.MeleeUnit];
                        int unitCost = unitProperties.InitialCost + knights.Count;

                        if (IsDanger && MyResource >= unitCost &&
                            knights.Count <= MaxKnightsCount)
                        {
                            SetBuildUnitAction(entity, EntityType.MeleeUnit, unitCost, entityActions);
                        }

                        if (!entityActions.ContainsKey(entity.Id))
                        {
                            entityActions.Add(entity.Id, new EntityAction(null, null, null, null));
                        }

                        continue;
                    }

                    case EntityType.MeleeUnit:
                    {
                        var target = NearestEnemy?.Position;
                        SetMoveAction(entity, target, entityActions, true);
                        continue;
                    }

                    case EntityType.RangedBase:
                    {
                        var unitProperties = playerView.EntityProperties[EntityType.RangedUnit];
                        int unitCost = unitProperties.InitialCost + rangers.Count;

                        if (IsDanger && MyResource >= unitCost &&
                            rangers.Count <= MaxRangersCount)
                        {
                            SetBuildUnitAction(entity, EntityType.RangedUnit, unitCost, entityActions);
                        }

                        if (!entityActions.ContainsKey(entity.Id))
                        {
                            entityActions.Add(entity.Id, new EntityAction(null, null, null, null));
                        }

                        continue;
                    }

                    case EntityType.RangedUnit:
                    {
                        var target = NearestEnemy?.Position;
                        SetMoveAction(entity, target, entityActions, true);
                        continue;
                    }
                    case EntityType.Resource:
                        continue;

                    case EntityType.Turret:
                    {
                        var attackAction = new AttackAction(null, new AutoAttack(properties.SightRange, UnitTargets));
                        entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
                        continue;
                    }
                }
            }

            return new Action(entityActions);
        }

        private static void SetBuildUnitAction(Entity entity, EntityType buildUnit, int unitCost, Dictionary<int, EntityAction> entityActions)
        {
            var neighbors = entity.Position.Neighbors(5);
            foreach (var position in neighbors)
            {
                if (ScoreMap.Passable(position))
                {
                    var buildAction = new BuildAction(buildUnit, position);
                    entityActions.Add(entity.Id, new EntityAction(null, buildAction, null, null));

                    ScoreMap.BuildLeft(position, 1);
                    MyResource -= unitCost;
                    break;
                }
            }
        }

        private static void SetBuilderRepairAction(PlayerView playerView, Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            var neighbors = entity.Position.Neighbors();
            foreach (var target in neighbors)
            {
                var targetEntity = ScoreMap.Get(target).Entity;
                if (targetEntity == null)
                {
                    continue;
                }

                var entityProperties = playerView.EntityProperties[targetEntity.Value.EntityType];
                if (targetEntity.Value.PlayerId == MyId && 
                    targetEntity.Value.Health < entityProperties.MaxHealth)
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
                var targetEntity = ScoreMap.Get(target).Entity;
                if (targetEntity?.EntityType == EntityType.Resource)
                {
                    var attackAction = new AttackAction(targetEntity.Value.Id, null);
                    entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
                    break;
                }
            }
        }

        private static void SetMoveAction(Entity entity, Vec2Int? approxTarget, Dictionary<int, EntityAction> entityActions, bool addAutoAttack)
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

            for (int i = 0; i < MaxSearchMove && frontier.Count > 0; i++)
            {
                var current = frontier.Dequeue();

                if (entity.EntityType == EntityType.BuilderUnit &&
                    (ScoreMap.Get(current).ResourceScore > 0 ||
                     ScoreMap.Get(current).RepairScore > 0))
                {
                    moveTarget = current;
                    break;
                }

                if ((entity.EntityType == EntityType.MeleeUnit || 
                    entity.EntityType == EntityType.RangedUnit) &&
                    ScoreMap.Get(current).AttackScore > 0)
                {
                    moveTarget = current;
                    break;
                }

                foreach (Vec2Int next in current.Neighbors())
                {
                    int newCost = costSoFar[current] + 1;
                    if (ScoreMap.Passable(next) && (!costSoFar.ContainsKey(next) || newCost < costSoFar[next]))
                    {
                        costSoFar[next] = newCost;
                        frontier.Enqueue(next, newCost);
                        //cameFrom[next] = current;
                    }
                }
            }

            if (moveTarget == null && approxTarget != null)
            {
                moveTarget = approxTarget;
            }

            //if (moveTarget != null)
            //{
            //    Map[entity.Position.X, entity.Position.Y] = null;
            //    Map[moveTarget.X, moveTarget.Y] = entity;
            //}

            AttackAction? attackAction = null;
            if (addAutoAttack)
            {
                attackAction = new AttackAction(null, new AutoAttack(10, UnitTargets));
            }

            var moveAction = moveTarget != null ? new MoveAction(moveTarget.Value, false, false) : (MoveAction?) null;
            entityActions.Add(entity.Id, new EntityAction(moveAction, null, attackAction, null));
        }

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }
    }
}