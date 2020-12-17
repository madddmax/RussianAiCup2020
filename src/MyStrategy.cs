using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Aicup2020.Model;
using Aicup2020.MyModel;

namespace Aicup2020
{
    public class MyStrategy
    {
        public static Entity? NearestEnemy;

        public static bool IsDanger;

        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            ScoreMap.InitMap(playerView);

            var entityActions = new Dictionary<int, EntityAction>();

            var dangerPoints = new List<Entity>(ScoreMap.MyActiveBuilderBases);
            dangerPoints.AddRange(ScoreMap.MyNotActiveBuilderBases);

            dangerPoints.AddRange(ScoreMap.MyActiveMeleeBases);
            dangerPoints.AddRange(ScoreMap.MyNotActiveMeleeBases);

            dangerPoints.AddRange(ScoreMap.MyActiveRangedBases);
            dangerPoints.AddRange(ScoreMap.MyNotActiveRangedBases);

            dangerPoints.AddRange(ScoreMap.MyActiveHouses.TakeLast(5));
            dangerPoints.AddRange(ScoreMap.MyNotActiveHouses);

            dangerPoints.AddRange(ScoreMap.MyBuilderUnits.TakeLast(5));
            foreach (var dangerPoint in dangerPoints)
            {
                NearestEnemy = ScoreMap.Enemies
                    .OrderBy(e => e.Position.Distance(dangerPoint.Position))
                    .FirstOrDefault();

                IsDanger = NearestEnemy.Value.Position.Distance(dangerPoint.Position) <= Params.DangerDistance;
                if (IsDanger)
                {
                    break;
                }
            }

            // repairing
            foreach (Entity entity in playerView.Entities)
            {
                if (entity.PlayerId == ScoreMap.MyId &&
                    entity.EntityType == EntityType.BuilderUnit)
                {
                    SetBuilderRepairAction(playerView, entity, entityActions);
                }
            }

            // building
            if (ScoreMap.MyActiveRangedBases.Count == 0 &&
                ScoreMap.Limit >= Params.RangedBaseBuildingLimit &&
                ScoreMap.MyResource >= ScoreMap.RangedBaseProperties.InitialCost &&
                ScoreMap.MyNotActiveRangedBases.Count == 0)
            {
                SetBuildAction(EntityType.RangedBase, ScoreMap.RangedBaseProperties.Size, entityActions);
                ScoreMap.MyResource -= ScoreMap.RangedBaseProperties.InitialCost;
            }

            if (((ScoreMap.MyActiveRangedBases.Count == 0 &&
                  ScoreMap.Limit >= Params.RangedBaseBuildingLimit &&
                  ScoreMap.MyResource >=
                  ScoreMap.RangedBaseProperties.InitialCost + ScoreMap.HouseProperties.InitialCost) ||

                 ((ScoreMap.MyActiveRangedBases.Count > 0 ||
                   ScoreMap.MyNotActiveRangedBases.Count > 0 ||
                   ScoreMap.Limit < Params.RangedBaseBuildingLimit) &&
                  ScoreMap.MyResource >= ScoreMap.HouseProperties.InitialCost)
                ) &&
                ScoreMap.Limit + 10 >= ScoreMap.AvailableLimit &&
                ScoreMap.MyNotActiveHouses.Count <= 1)
            {
                SetBuildAction(EntityType.House, ScoreMap.HouseProperties.Size, entityActions);
                ScoreMap.MyResource -= ScoreMap.HouseProperties.InitialCost;
            }

            foreach (Entity entity in playerView.Entities)
            {
                if (entity.PlayerId != ScoreMap.MyId)
                {
                    continue;
                }

                var properties = playerView.EntityProperties[entity.EntityType];

                switch (entity.EntityType)
                {
                    case EntityType.BuilderUnit:
                    {
                        SetBuilderAttackAction(entity, entityActions);

                        var target = ScoreMap.Resources.Count > 0
                            ? ScoreMap.Resources.Last().Position
                            : (Vec2Int?)null;

                        SetMoveAction(entity, target, entityActions, false);
                        continue;
                    }

                    case EntityType.MeleeUnit:
                    {
                        var target = NearestEnemy?.Position;
                        SetMoveAction(entity, target, entityActions, true);
                        continue;
                    }

                    case EntityType.RangedUnit:
                    {
                        var target = NearestEnemy?.Position;
                        SetMoveAction(entity, target, entityActions, true);
                        continue;
                    }

                    case EntityType.BuilderBase:
                    {
                        var unitProperties = playerView.EntityProperties[EntityType.BuilderUnit];
                        int unitCost = unitProperties.InitialCost + ScoreMap.MyBuilderUnits.Count;

                        if ((IsDanger && ScoreMap.MyResource >= unitCost * 2 ||
                            !IsDanger && ScoreMap.MyResource >= unitCost) &&
                            ScoreMap.MyBuilderUnits.Count < Params.MaxBuilderUnitsCount)
                        {
                            SetBuildUnitAction(entity, EntityType.BuilderUnit, unitCost, entityActions);
                        }

                        if (!entityActions.ContainsKey(entity.Id))
                        {
                            entityActions.Add(entity.Id, new EntityAction(null, null, null, null));
                        }

                        continue;
                    }

                    case EntityType.MeleeBase:
                    {
                        var unitProperties = playerView.EntityProperties[EntityType.MeleeUnit];
                        int unitCost = unitProperties.InitialCost + ScoreMap.MyMeleeUnits.Count;

                        if ((IsDanger && ScoreMap.MyResource >= unitCost ||
                             !IsDanger && ScoreMap.MyResource >= unitCost * 2) &&
                            ScoreMap.MyRangedUnits.Count * 2 >= Params.MaxRangedUnitsCount &&
                            ScoreMap.MyMeleeUnits.Count < Params.MaxMeleeUnitsCount)
                        {
                            SetBuildUnitAction(entity, EntityType.MeleeUnit, unitCost, entityActions);
                        }

                        if (!entityActions.ContainsKey(entity.Id))
                        {
                            entityActions.Add(entity.Id, new EntityAction(null, null, null, null));
                        }

                        continue;
                    }

                    case EntityType.RangedBase:
                    {
                        var unitProperties = playerView.EntityProperties[EntityType.RangedUnit];
                        int unitCost = unitProperties.InitialCost + ScoreMap.MyRangedUnits.Count;

                        if ((IsDanger && ScoreMap.MyResource >= unitCost ||
                             !IsDanger && ScoreMap.MyResource >= unitCost * 3) &&
                            ScoreMap.MyRangedUnits.Count < Params.MaxRangedUnitsCount)
                        {
                            SetBuildUnitAction(entity, EntityType.RangedUnit, unitCost, entityActions);
                        }

                        if (!entityActions.ContainsKey(entity.Id))
                        {
                            entityActions.Add(entity.Id, new EntityAction(null, null, null, null));
                        }

                        continue;
                    }

                    case EntityType.Turret:
                    {
                        var attackAction = new AttackAction(null, new AutoAttack(properties.SightRange, ScoreMap.UnitTargets));
                        entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
                        continue;
                    }
                }
            }

            return new Action(entityActions);
        }

        private static void SetBuildAction(EntityType buildEntityType, int size, Dictionary<int, EntityAction> entityActions)
        {
            foreach (Entity builder in ScoreMap.MyBuilderUnits)
            {
                if (entityActions.ContainsKey(builder.Id))
                {
                    continue;
                }

                var buildPositions = builder.Position.BuildPositions(size);
                foreach (var position in buildPositions)
                {
                    var diagonals = position.Diagonals(size);
                    var neighbors = position.Neighbors(size);

                    if (ScoreMap.Passable(position, size) &&
                        diagonals.All(ScoreMap.PassableInFuture) &&
                        (size > 3 || neighbors.All(ScoreMap.PassableInFuture)))
                    {
                        var buildAction = new BuildAction(buildEntityType, position);
                        entityActions.Add(builder.Id, new EntityAction(null, buildAction, null, null));

                        ScoreMap.Build(position, size);
                        return;
                    }
                }
            }
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

                    ScoreMap.Build(position, 1);
                    ScoreMap.MyResource -= unitCost;
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
                if (targetEntity.Value.PlayerId == ScoreMap.MyId && 
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
            var frontier = new PriorityQueue<Vec2Int>();
            frontier.Enqueue(entity.Position, 0);

            var cameFrom = new Dictionary<Vec2Int, Vec2Int>();
            var costSoFar = new Dictionary<Vec2Int, int>();
            var costScore = new Dictionary<Vec2Int, int>();

            costSoFar[entity.Position] = 0;
            costScore[entity.Position] = 0;

            for (int i = 0; i < Params.MaxSearchMove && frontier.Count > 0; i++)
            {
                var current = frontier.Dequeue();
                var neighbors = current.Neighbors();

                foreach (Vec2Int next in neighbors)
                {
                    int newCost = costSoFar[current] + 1;

                    var scoreCell = ScoreMap.Get(next);
                    int newScoreCost = costScore[current] + scoreCell.DamageScore;

                    if (ScoreMap.Passable(next) && (!costSoFar.ContainsKey(next) || newCost < costSoFar[next]))
                    {
                        // todo merge costs
                        costSoFar[next] = newCost;
                        costScore[next] = newScoreCost;

                        frontier.Enqueue(next, newCost);
                        cameFrom[next] = current;
                    }
                }
            }

            Vec2Int? bestTarget = null;
            foreach (var target in cameFrom)
            {
                var current = target.Key;
                var scoreCell = ScoreMap.Get(current);

                if (entity.EntityType == EntityType.BuilderUnit &&
                    (
                        scoreCell.ResourceScore > 0 ||
                        scoreCell.RepairScore > 0
                    ) &&
                    (
                        bestTarget == null ||
                        costSoFar[current] < costSoFar[bestTarget.Value]
                    ) && 
                    scoreCell.DamageScore == 0
                    )
                {
                    bestTarget = current;
                }

                if ((
                        entity.EntityType == EntityType.MeleeUnit ||
                        entity.EntityType == EntityType.RangedUnit
                    ) &&
                    scoreCell.AttackScore > 0 &&
                    (
                        bestTarget == null ||
                        costSoFar[current] < costSoFar[bestTarget.Value]
                    )
                )
                {
                    bestTarget = current;
                }
            }

            Vec2Int? moveTarget = null;
            if (bestTarget != null)
            {
                moveTarget = GetMoveTarget(bestTarget.Value, cameFrom, costSoFar);
            }

            if (moveTarget == null && approxTarget != null)
            {
                int approxTargetDistance = int.MaxValue;

                foreach (var target in cameFrom)
                {
                    var current = target.Key;
                    var scoreCell = ScoreMap.Get(current);

                    if (moveTarget == null ||
                        approxTarget.Value.Distance(current) < approxTargetDistance)
                    {
                        if (entity.EntityType == EntityType.BuilderUnit &&
                            scoreCell.DamageScore > 0)
                        {
                            continue;
                        }

                        // todo depth
                        moveTarget = GetMoveTarget(current, cameFrom, costSoFar);
                        approxTargetDistance = approxTarget.Value.Distance(current);
                    }
                }
            }

            if (moveTarget != null)
            {
                ScoreMap.Set(entity.Position, null);
                ScoreMap.Set(moveTarget.Value, entity);
            }

            AttackAction? attackAction = null;
            if (addAutoAttack)
            {
                attackAction = new AttackAction(null, new AutoAttack(10, ScoreMap.UnitTargets));
            }

            var moveAction = moveTarget != null ? new MoveAction(moveTarget.Value, false, false) : (MoveAction?) null;
            entityActions.Add(entity.Id, new EntityAction(moveAction, null, attackAction, null));
        }

        private static Vec2Int GetMoveTarget(Vec2Int current, Dictionary<Vec2Int, Vec2Int> cameFrom, Dictionary<Vec2Int, int> costSoFar)
        {
            var fromPosition = current;

            while (costSoFar[fromPosition] > 1)
            {
                fromPosition = cameFrom[fromPosition];
            }

            return fromPosition;
        }

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }
    }
}