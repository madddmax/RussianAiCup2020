using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Aicup2020.MyActions;
using Aicup2020.MyModel;

namespace Aicup2020
{
    public class MyStrategy
    {
        public static Color Red = new Color(255, 0, 0, 100);
        public static Color Green = new Color(0, 255, 0, 100);
        public static Color Blue = new Color(0, 0, 255, 100);

        public static bool IsDanger;

        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            if (Params.IsDebug)
            {
                debugInterface.Send(new DebugCommand.SetAutoFlush(true));
            }

            var entityActions = new Dictionary<int, EntityAction>();
            ScoreMap.InitMap(playerView);
            DrawScoreMap(debugInterface);
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

                switch (entity.EntityType)
                {
                    case EntityType.BuilderUnit:
                    {
                        BuilderUnitActions.SetAttack(entity, entityActions);
                        var approxTarget = BuilderUnitActions.GetApproxTarget(entity, entityActions);
                        SetMoveAction(entity, approxTarget, entityActions, debugInterface);
                        continue;
                    }

                    case EntityType.MeleeUnit:
                    {
                        CombatUnitAction.SetAttack(entity, 1, 1, entityActions);
                        var approxTarget = CombatUnitAction.GetAttackTarget(entity, entityActions);
                        SetMoveAction(entity, approxTarget, entityActions, debugInterface);
                        continue;
                    }

                    case EntityType.RangedUnit:
                    {
                        CombatUnitAction.SetAttack(entity, 5, 1, entityActions);
                        var approxTarget = CombatUnitAction.GetAttackTarget(entity, entityActions);
                        SetMoveAction(entity, approxTarget, entityActions, debugInterface);
                        continue;
                    }

                    case EntityType.Turret:
                    {
                        CombatUnitAction.SetAttack(entity, 5, 2, entityActions);
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
            foreach (var enemyTarget in ScoreMap.EnemyTargets)
            {
                isDanger = ScoreMap.MyProduction.Any(
                    p => p.Position.Distance(enemyTarget) <= Params.DangerDistance
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

        private static void SetMoveAction(Entity entity, Vec2Int approxTarget, Dictionary<int, EntityAction> entityActions, DebugInterface debugInterface)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            Vec2Int? moveTarget = null;

            // AStarSearch
            Vec2Int? bestTarget = null;
            Vec2Int distantTarget = approxTarget;
            int minDistanceCost = int.MaxValue;

            var frontier = new PriorityQueue<Vec2Int>();
            frontier.Enqueue(entity.Position, 0);

            var cameFrom = new Dictionary<Vec2Int, Vec2Int>();
            var costSoFar = new Dictionary<Vec2Int, int>();

            cameFrom[entity.Position] = entity.Position;
            costSoFar[entity.Position] = 0;

            for (int i = 0; i < Params.MaxSearchMove && frontier.Count > 0; i++)
            {
                var current = frontier.Dequeue();
                var currentCell = ScoreMap.Get(current);

                if (entity.EntityType == EntityType.BuilderUnit &&
                    (
                        currentCell.ResourceScore > 0 ||
                        currentCell.RepairScore > 0
                    )
                )
                {
                    bestTarget = current;
                    break;
                }

                if (entity.EntityType == EntityType.MeleeUnit &&
                    currentCell.MeleeAttack > 0)
                {
                    bestTarget = current;
                    break;
                }

                if (entity.EntityType == EntityType.RangedUnit &&
                    currentCell.RangedAttack > 0)
                {
                    bestTarget = current;
                    break;
                }

                var distanceCost = approxTarget.Distance(current);
                if (distanceCost < minDistanceCost)
                {
                    distantTarget = current;
                    minDistanceCost = distanceCost;
                }

                var neighbors = current.Neighbors();
                foreach (Vec2Int next in neighbors)
                {
                    var nextCell = ScoreMap.Get(next);
                    if (nextCell.Entity != null &&
                        nextCell.Entity?.EntityType != EntityType.Resource)
                    {
                        // todo учесть юнитов
                        continue;
                    }

                    int nextCost = costSoFar[current] + 1;
                    if (entity.EntityType == EntityType.BuilderUnit)
                    {
                        nextCost += nextCell.AllDamage;
                    }

                    if (entity.EntityType == EntityType.RangedUnit)
                    {
                        nextCost += nextCell.MeleeDamage + nextCell.TurretDamage;
                    }

                    if (entity.EntityType == EntityType.MeleeUnit)
                    {
                        nextCost += nextCell.TurretDamage;
                    }

                    if (entity.EntityType != EntityType.BuilderUnit &&
                        nextCell.Entity?.EntityType == EntityType.Resource)
                    {
                        nextCost += 2;
                    }

                    if (!costSoFar.ContainsKey(next) || 
                        nextCost < costSoFar[next])
                    {
                        costSoFar[next] = nextCost;

                        frontier.Enqueue(next, nextCost);
                        cameFrom[next] = current;
                    }
                }
            }

            moveTarget = bestTarget != null 
                ? GetMoveTarget(entity.Position, bestTarget.Value, cameFrom, Blue, debugInterface) 
                : GetMoveTarget(entity.Position, distantTarget, cameFrom, Green, debugInterface);

            ScoreMap.Set(entity.Position, null);
            ScoreMap.Set(moveTarget.Value, entity);

            var moveAction = new MoveAction(moveTarget.Value, false, false);
            entityActions.Add(entity.Id, new EntityAction(moveAction, null, null, null));
        }

        private static Vec2Int GetMoveTarget(Vec2Int current, Vec2Int target, Dictionary<Vec2Int, Vec2Int> cameFrom, Color color, DebugInterface debugInterface)
        {
            var fromPosition = target;

            while (cameFrom[fromPosition].X != current.X ||
                   cameFrom[fromPosition].Y != current.Y)
            {
                if (Params.IsDebug)
                {
                    DrawLine(fromPosition, cameFrom[fromPosition], color, debugInterface);
                }

                fromPosition = cameFrom[fromPosition];
            }

            return fromPosition;
        }

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }

        private static void DrawScoreMap(DebugInterface debugInterface)
        {
            if (!Params.IsDebug)
            {
                return;
            }

            for (int x = 0; x < 80; x++)
            {
                for (int y = 0; y < 80; y++)
                {
                    var scoreCell = ScoreMap.Get(x, y);
                    if (scoreCell.ResourceScore > 0)
                    {
                        DrawRegion(x, y, Green, debugInterface);
                    }

                    if (scoreCell.RepairScore > 0)
                    {
                        DrawRegion(x, y, Green, debugInterface);
                    }

                    //if (scoreCell.MeleeAttack > 0)
                    //{
                    //    DrawRegion(x, y, Blue, debugInterface);
                    //}

                    //if (scoreCell.RangedAttack > 0)
                    //{
                    //    DrawRegion(x, y, Blue, debugInterface);
                    //}

                    if (scoreCell.MeleeDamage > 0)
                    {
                        DrawRegion(x, y, Red, debugInterface);
                    }

                    if (scoreCell.TurretDamage > 0)
                    {
                        DrawRegion(x, y, Red, debugInterface);
                    }

                    if (scoreCell.RangedDamage > 0)
                    {
                        DrawRegion(x, y, Blue, debugInterface);
                    }
                }
            }
        }

        public static void DrawRegion(int x, int y, Color color, DebugInterface debugInterface)
        {
            var vertex1 = new ColoredVertex(new Vec2Float(x + 0.25f, y + 0.25f), new Vec2Float(0, 0), color);
            var vertex2 = new ColoredVertex(new Vec2Float(x + 0.75f, y + 0.25f), new Vec2Float(0, 0), color);
            var vertex3 = new ColoredVertex(new Vec2Float(x + 0.75f, y + 0.75f), new Vec2Float(0, 0), color);
            var vertex4 = new ColoredVertex(new Vec2Float(x + 0.25f, y + 0.75f), new Vec2Float(0, 0), color);

            var debugData = new DebugData.Primitives(new[] { vertex1, vertex2, vertex3, vertex4 }, PrimitiveType.Lines);
            var debugCommand = new DebugCommand.Add(debugData);

            debugInterface.Send(debugCommand);
        }

        public static void DrawLine(Vec2Int p1, Vec2Int p2, Color color, DebugInterface debugInterface)
        {
            var vertex1 = new ColoredVertex(new Vec2Float(p1.X + 0.5f, p1.Y + 0.5f), new Vec2Float(0, 0), color);
            var vertex2 = new ColoredVertex(new Vec2Float(p2.X + 0.5f, p2.Y + 0.5f), new Vec2Float(0, 0), color);

            var debugData = new DebugData.Primitives(new[] { vertex1, vertex2 }, PrimitiveType.Lines);
            var debugCommand = new DebugCommand.Add(debugData);

            debugInterface.Send(debugCommand);
        }
    }
}