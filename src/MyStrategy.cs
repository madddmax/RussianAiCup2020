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
                    if (entitiesToRepair.Count > 0 && 
                        entitiesToRepair.Any(e => Distance(e.Position, entity.Position) < 10))
                    {
                        var minDistance = entitiesToRepair.Min(e => Distance(e.Position, entity.Position));
                        var entityToRepair = entitiesToRepair.First(e => Math.Abs(Distance(e.Position, entity.Position) - minDistance) < 0.001);
                        moveAction = new MoveAction(entityToRepair.Position, false, false);
                        repairAction = new RepairAction(entityToRepair.Id);
                    }
                    else if(entitiesToRepair.Count == 0)
                    {
                        EntityType buildEntityType;
                        if (houses == 0)
                        {
                            buildEntityType = EntityType.House;
                        }
                        else
                        {
                            buildEntityType = turrets / houses > 2 && houses < 10 ? EntityType.House : EntityType.Turret;
                        }
                        
                        var position = new Vec2Int(
                            entity.Position.X + properties.Size,
                            entity.Position.Y + properties.Size - 1
                        );
                        buildAction = new BuildAction(buildEntityType, position);


                        EntityType[] builderTargets = { EntityType.Resource };
                        attackAction = new AttackAction(null, new AutoAttack(playerView.MapSize, builderTargets));
                    }
                    else
                    {
                        EntityType[] builderTargets = { EntityType.Resource };
                        attackAction = new AttackAction(null, new AutoAttack(playerView.MapSize, builderTargets));
                    }
                }
                else if (entity.EntityType == EntityType.MeleeUnit || 
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
                        entity.Position.X + properties.Size,
                        entity.Position.Y + properties.Size - 1
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
    }
}