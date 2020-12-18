using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Aicup2020.MyActions;
using Aicup2020.MyModel;

namespace Aicup2020
{
    public class MyStrategy
    {
        public static bool IsDanger;

        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            var entityActions = new Dictionary<int, EntityAction>();
            ScoreMap.InitMap(playerView);
            IsDanger = DangerCheck();

            // repairing
            foreach (Entity entity in playerView.Entities)
            {
                if (entity.PlayerId == ScoreMap.MyId &&
                    entity.EntityType == EntityType.BuilderUnit)
                {
                    BuilderUnitActions.SetRepair(playerView, entity, entityActions);
                }
            }

            // building
            if (ScoreMap.MyActiveRangedBases.Count == 0 &&
                ScoreMap.Limit >= Params.RangedBaseBuildingLimit &&
                ScoreMap.MyResource >= ScoreMap.RangedBaseProperties.InitialCost &&
                ScoreMap.MyNotActiveRangedBases.Count == 0)
            {
                BuilderUnitActions.SetBuild(EntityType.RangedBase, ScoreMap.RangedBaseProperties.Size, entityActions);
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
                ScoreMap.MyNotActiveHouses.Count <= 1 &&
                ScoreMap.MyNotActiveHouses.Count + ScoreMap.MyActiveHouses.Count < Params.MaxHouseCount)
            {
                BuilderUnitActions.SetBuild(EntityType.House, ScoreMap.HouseProperties.Size, entityActions);
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
                        BuilderUnitActions.SetAttack(entity, entityActions);

                        var approxTarget = BuilderUnitActions.GetApproxTarget(entity);
                        SetMoveAction(entity, approxTarget, entityActions, false);
                        continue;
                    }

                    case EntityType.MeleeUnit:
                    {
                        var approxTarget = CombatUnitAction.SetAttack(entity, 1, entityActions);
                        SetMoveAction(entity, approxTarget, entityActions, true);
                        continue;
                    }

                    case EntityType.RangedUnit:
                    {
                        var approxTarget = CombatUnitAction.SetAttack(entity, 5,  entityActions);
                        SetMoveAction(entity, approxTarget, entityActions, true);
                        continue;
                    }

                    case EntityType.Turret:
                    {
                        CombatUnitAction.SetAttack(entity, 5, entityActions);
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
                }
            }

            return new Action(entityActions);
        }

        private static bool DangerCheck()
        {
            bool isDanger = false;
            foreach (var enemy in ScoreMap.Enemies)
            {
                isDanger = ScoreMap.MyProduction.Any(
                    p => p.Position.Distance(enemy.Position) <= Params.DangerDistance
                );

                if (isDanger)
                {
                    break;
                }
            }

            return isDanger;
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

            cameFrom[entity.Position] = entity.Position;
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
            foreach (var cost in costScore)
            {
                var current = cost.Key;
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

                foreach (var cost in costScore)
                {
                    var current = cost.Key;
                    var scoreCell = ScoreMap.Get(current);

                    if ((moveTarget == null ||
                        approxTarget.Value.Distance(current) < approxTargetDistance) &&
                        (entity.EntityType != EntityType.BuilderUnit || scoreCell.DamageScore == 0))
                    {
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

            var moveAction = moveTarget != null ? new MoveAction(moveTarget.Value, false, true) : (MoveAction?) null;
            entityActions.Add(entity.Id, new EntityAction(moveAction, null, null, null));
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