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
            Entity?[,] map = new Entity?[80, 80];

            foreach (Entity entity in playerView.Entities)
            {
                var properties = playerView.EntityProperties[entity.EntityType];
                if (properties.Size > 1)
                {
                    for (int y = entity.Position.Y; y < entity.Position.Y + properties.Size; y++)
                    {
                        for (int x = entity.Position.X; x < entity.Position.X + properties.Size; x++)
                        {
                            map[x, y] = entity;
                        }
                    }
                }
                else
                {
                    map[entity.Position.X, entity.Position.Y] = entity;
                }
            }


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

            foreach (Entity entity in playerView.Entities)
            {
                if (entity.PlayerId != null && entity.PlayerId != playerView.MyId)
                {
                    continue;
                }

                if (entity.PlayerId != playerView.MyId)
                {
                    continue;
                }

                var properties = playerView.EntityProperties[entity.EntityType];

                MoveAction? moveAction = null;
                BuildAction? buildAction = null;
                AttackAction? attackAction = null;
                RepairAction? repairAction = null;

                switch (entity.EntityType)
                {
                    case EntityType.Wall:
                        continue;

                    case EntityType.House:
                        continue;

                    case EntityType.BuilderBase:
                        var entityType = properties.Build.Value.Options[0];
                        var position = new Vec2Int(
                            entity.Position.X,
                            entity.Position.Y - 1
                        );

                        buildAction = new BuildAction(entityType, position);
                        entityActions.Add(entity.Id, new EntityAction(null, buildAction, null, null));
                        continue;

                    case EntityType.BuilderUnit:
                    {
                        int leftUpEval = 0;
                        for (int y = entity.Position.Y; y >= entity.Position.Y - 10 && y >= 0; y--)
                        {
                            for (int x = entity.Position.X; x <= entity.Position.X + 10 && x < 80; x++)
                            {
                                var entityOnMap = map[x, y];
                                if (entityOnMap == null)
                                {
                                    continue;
                                }

                                if (entityOnMap.Value.EntityType == EntityType.Resource)
                                {
                                    var distance = GetDistance(entity.Position, x, y);
                                    leftUpEval += entityOnMap.Value.Health / distance;
                                }

                                if (entityOnMap.Value.EntityType == EntityType.BuilderUnit &&
                                    entityOnMap.Value.PlayerId == playerView.MyId)
                                {
                                    leftUpEval -= entityOnMap.Value.Health;
                                }
                            }
                        }

                        int rightUpEval = 0;
                        for (int y = entity.Position.Y; y >= entity.Position.Y - 10 && y >= 0; y--)
                        {
                            for (int x = entity.Position.X; x >= entity.Position.X - 10 && x >= 0; x--)
                            {
                                var entityOnMap = map[x, y];
                                if (entityOnMap == null)
                                {
                                    continue;
                                }

                                if (entityOnMap.Value.EntityType == EntityType.Resource)
                                {
                                    var distance = GetDistance(entity.Position, x, y);
                                    rightUpEval += entityOnMap.Value.Health / distance;
                                }

                                if (entityOnMap.Value.EntityType == EntityType.BuilderUnit &&
                                    entityOnMap.Value.PlayerId == playerView.MyId)
                                {
                                    rightUpEval -= entityOnMap.Value.Health;
                                }
                            }
                        }

                        int rightDownEval = 0;
                        for (int y = entity.Position.Y; y <= entity.Position.Y + 10 && y < 80; y++)
                        {
                            for (int x = entity.Position.X; x >= entity.Position.X - 10 && x >= 0; x--)
                            {
                                var entityOnMap = map[x, y];
                                if (entityOnMap == null)
                                {
                                    continue;
                                }

                                if (entityOnMap.Value.EntityType == EntityType.Resource)
                                {
                                    var distance = GetDistance(entity.Position, x, y);
                                    rightDownEval += entityOnMap.Value.Health / distance;
                                }

                                if (entityOnMap.Value.EntityType == EntityType.BuilderUnit && 
                                    entityOnMap.Value.PlayerId == playerView.MyId)
                                {
                                    rightDownEval -= entityOnMap.Value.Health;
                                }
                            }
                        }

                        int leftDownEval = 0;
                        for (int y = entity.Position.Y; y <= entity.Position.Y + 10 && y < 80; y++)
                        {
                            for (int x = entity.Position.X; x <= entity.Position.X + 10 && x < 80; x++)
                            {
                                var entityOnMap = map[x, y];
                                if (entityOnMap == null)
                                {
                                    continue;
                                }

                                if (entityOnMap.Value.EntityType == EntityType.Resource)
                                {
                                    var distance = GetDistance(entity.Position, x, y);
                                    leftDownEval += entityOnMap.Value.Health / distance;
                                }

                                if (entityOnMap.Value.EntityType == EntityType.BuilderUnit &&
                                    entityOnMap.Value.PlayerId == playerView.MyId)
                                {
                                    leftDownEval -= entityOnMap.Value.Health;
                                }
                            }
                        }

                        int leftEval = entity.Position.X + 1 < 80 && map[entity.Position.X + 1, entity.Position.Y] == null ? leftUpEval + leftDownEval : int.MinValue;
                        int upEval = entity.Position.Y - 1 >= 0 && map[entity.Position.X, entity.Position.Y - 1] == null ? leftUpEval + rightUpEval : int.MinValue;
                        int rightEval = entity.Position.X - 1 >= 0 && map[entity.Position.X - 1, entity.Position.Y] == null ? rightUpEval + rightDownEval : int.MinValue;
                        int downEval = entity.Position.Y + 1 < 80 && map[entity.Position.X, entity.Position.Y + 1] == null ? rightDownEval + leftDownEval : int.MinValue;

                        if (leftEval >= upEval &&
                            leftEval >= rightEval &&
                            leftEval >= downEval)
                        {
                            moveAction = new MoveAction(new Vec2Int(entity.Position.X + 1, entity.Position.Y), false,
                                false);
                        }
                        else if (upEval >= leftEval &&
                                 upEval >= rightEval &&
                                 upEval >= downEval)
                        {
                            moveAction = new MoveAction(new Vec2Int(entity.Position.X, entity.Position.Y - 1), false,
                                false);
                        }
                        else if (rightEval >= upEval &&
                                 rightEval >= leftEval &&
                                 rightEval >= downEval)
                        {
                            moveAction = new MoveAction(new Vec2Int(entity.Position.X - 1, entity.Position.Y), false,
                                false);
                        }
                        else if (downEval >= upEval &&
                                 downEval >= rightEval &&
                                 downEval >= leftEval)
                        {
                            moveAction = new MoveAction(new Vec2Int(entity.Position.X, entity.Position.Y + 1), false,
                                false);
                        }

                        EntityType[] resource = { EntityType.Resource };
                        attackAction = new AttackAction(null, new AutoAttack(1, resource));
                        entityActions.Add(entity.Id, new EntityAction(moveAction, null, attackAction, null));
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
                        attackAction = new AttackAction(null, new AutoAttack(properties.SightRange, unitTargets));
                        entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
                        continue;
                }
            }

            return new Action(entityActions);
        }

        public int GetDistance(Vec2Int p1, int x, int y) => Math.Abs(p1.X - x) + Math.Abs(p1.Y - y);

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }
    }
}