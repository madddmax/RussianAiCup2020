using System;
using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Action = Aicup2020.Model.Action;

namespace Aicup2020
{
    public class MyStrategy
    {
        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            var entityActions = new Dictionary<int, EntityAction>();

            var entitiesToRepair = playerView.Entities
                .Where(e =>
                    e.PlayerId == playerView.MyId &&
                    e.Health < playerView.EntityProperties[e.EntityType].MaxHealth
                ).ToList();

            var turrets = playerView.Entities
                .Count(e =>
                    e.PlayerId == playerView.MyId &&
                    e.Active &&
                    e.EntityType == EntityType.Turret
                );

            var houses = playerView.Entities
                .Count(e =>
                    e.PlayerId == playerView.MyId &&
                    e.Active &&
                    e.EntityType == EntityType.House
                );

            Entity? builderBase = playerView.Entities
                .FirstOrDefault(e =>
                    e.PlayerId == playerView.MyId &&
                    e.Active &&
                    e.EntityType == EntityType.BuilderBase
                );

            foreach (var entity in playerView.Entities)
            {
                if(entity.PlayerId != playerView.MyId)
                {
                    continue;
                }

                var properties = playerView.EntityProperties[entity.EntityType];

                MoveAction? moveAction = null;
                BuildAction? buildAction = null;
                AttackAction? attackAction = null;
                RepairAction? repairAction = null;

                if (entity.EntityType == EntityType.BuilderUnit)
                {
                    double? minDistanceToRepair = entitiesToRepair.Count > 0
                        ? entitiesToRepair.Min(e => Distance(e.Position, entity.Position))
                        : (double?) null;

                    if (minDistanceToRepair.HasValue &&
                        minDistanceToRepair > 1.5 &&
                        minDistanceToRepair < 4)
                    {
                        var entityToRepair = entitiesToRepair.First(
                            e => Math.Abs(Distance(e.Position, entity.Position) - minDistanceToRepair.Value) < 0.001
                        );
                        moveAction = new MoveAction(entityToRepair.Position, true, false);

                        entityActions.Add(entity.Id, new EntityAction(moveAction, null, null, null));
                        continue;
                    }

                    if (minDistanceToRepair.HasValue &&
                        minDistanceToRepair < 1.5)
                    {
                        var entityToRepair = entitiesToRepair.First(
                            e => Math.Abs(Distance(e.Position, entity.Position) - minDistanceToRepair.Value) < 0.001
                        );
                        repairAction = new RepairAction(entityToRepair.Id);

                        entityActions.Add(entity.Id, new EntityAction(null, null, null, repairAction));
                        continue;
                    }

                    var myPlayer = playerView.Players[playerView.MyId - 1];
                    var buildEntityType = turrets > houses && houses < 15 ? EntityType.House : EntityType.Turret;
                    var buildEntity = playerView.EntityProperties[buildEntityType];

                    if (myPlayer.Resource >= buildEntity.Cost)
                    {
                        var position = new Vec2Int(
                            entity.Position.X + properties.Size,
                            entity.Position.Y + properties.Size - 1
                        );

                        if (!IsInRect(builderBase.Value.Position, position, buildEntity.Size))
                        {
                            buildAction = new BuildAction(buildEntityType, position);
                        }
                    }

                    EntityType[] builderTargets = { EntityType.Resource };
                    attackAction = new AttackAction(null, new AutoAttack(playerView.MapSize * 2, builderTargets));

                    entityActions.Add(entity.Id, new EntityAction(null, buildAction, attackAction, null));
                    continue;
                }

                if (entity.EntityType == EntityType.MeleeUnit || 
                    entity.EntityType == EntityType.RangedUnit ||
                    entity.EntityType == EntityType.Turret)
                {
                    EntityType[] unitTargets = { EntityType.MeleeUnit, EntityType.RangedUnit, EntityType.BuilderUnit };
                    attackAction = new AttackAction(null, new AutoAttack(properties.SightRange, unitTargets));
                }
                else if (entity.EntityType == EntityType.MeleeBase)
                {
                    continue;
                }
                else if (entity.EntityType == EntityType.RangedBase)
                {
                    continue;
                }
                else if (properties.Build.HasValue)
                {
                    var entityType = properties.Build.Value.Options[0];
                    var position = new Vec2Int(
                        entity.Position.X,
                        entity.Position.Y - 1
                    );

                    buildAction = new BuildAction(entityType, position);
                }

                var entityAction = new EntityAction(moveAction, buildAction, attackAction, repairAction);
                entityActions.Add(entity.Id, entityAction);
            }

            return new Action(entityActions);
        }

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }

        public static double Distance(Vec2Int p1, Vec2Int p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X)
                             + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        public static bool IsInRect(Vec2Int p1, Vec2Int p2, int size)
        {
            if (p1.X >= p2.X &&
                p1.Y >= p2.Y &&
                p1.X <= p2.X + size &&
                p1.Y <= p2.Y + size)
            {
                return true;
            }

            return false;
        }
    }
}