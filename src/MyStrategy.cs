using System;
using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Action = Aicup2020.Model.Action;

namespace Aicup2020
{
    public class MyStrategy
    {
        private static readonly Entity?[,] Map = new Entity?[80, 80];

        public static Dictionary<int, EntityAction> PrevEntityActions = new Dictionary<int, EntityAction>();

        private static readonly Vec2Int MyBase = new Vec2Int(1, 1);

        public static int MyId;

        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            InitMap(playerView);
            MyId = playerView.MyId;

            var entityActions = new Dictionary<int, EntityAction>();

            var turrets = playerView.Entities
                .Count(e =>
                    e.PlayerId == MyId &&
                    e.Active &&
                    e.EntityType == EntityType.Turret
                );

            var houses = playerView.Entities
                .Count(e =>
                    e.PlayerId == MyId &&
                    e.Active &&
                    e.EntityType == EntityType.House
                );


            Entity? builderUnit = null;
            int minDistance = int.MaxValue;
            foreach (Entity entity in playerView.Entities)
            {
                if (entity.PlayerId != MyId || 
                    entity.EntityType != EntityType.BuilderUnit)
                {
                    continue;
                }

                if (PrevEntityActions.TryGetValue(entity.Id, out var entityAction) && 
                    entityAction.MoveAction != null)
                {
                    int distance = GetDistance(entity.Position, MyBase);
                    if (distance < minDistance)
                    {
                        // todo unit unable to build
                        builderUnit = entity;
                        minDistance = distance;
                    }
                }
            }

            if (builderUnit != null)
            {
                var myPlayer = playerView.Players[MyId - 1];
                //var buildEntityType = turrets > houses && houses < 15 ? EntityType.House : EntityType.Turret;
                var buildEntityType = EntityType.House;
                var buildEntity = playerView.EntityProperties[buildEntityType];

                if (myPlayer.Resource >= buildEntity.Cost)
                {
                    var position = new Vec2Int(
                        builderUnit.Value.Position.X + 1,
                        builderUnit.Value.Position.Y
                    );
                    var buildAction = new BuildAction(buildEntityType, position);
                    entityActions.Add(builderUnit.Value.Id, new EntityAction(null, buildAction, null, null));
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
                        var entityType = properties.Build.Value.Options[0];
                        var position = new Vec2Int(
                            entity.Position.X,
                            entity.Position.Y - 1
                        );

                        var buildAction = new BuildAction(entityType, position);
                        entityActions.Add(entity.Id, new EntityAction(null, buildAction, null, null));
                        continue;

                    case EntityType.BuilderUnit:
                    {
                        SetBuilderRepairAction(entity, entityActions);
                        SetBuilderAttackAction(entity, entityActions);
                        SetMoveAction(entity, entityActions, BuilderUnitMoveEval);
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

            PrevEntityActions = entityActions;
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

        private static void SetMoveAction(Entity entity, Dictionary<int, EntityAction> entityActions, Func<Entity, int, int, int> getEval)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            MoveAction? moveAction = null;

            int leftUpEval = 0;
            for (int y = entity.Position.Y; y >= entity.Position.Y - 10 && y >= 0; y--)
            {
                for (int x = entity.Position.X; x <= entity.Position.X + 10 && x < 80; x++)
                {
                    leftUpEval += getEval(entity, x, y);
                }
            }

            int rightUpEval = 0;
            for (int y = entity.Position.Y; y >= entity.Position.Y - 10 && y >= 0; y--)
            {
                for (int x = entity.Position.X; x >= entity.Position.X - 10 && x >= 0; x--)
                {
                    rightUpEval += getEval(entity, x, y);
                }
            }

            int rightDownEval = 0;
            for (int y = entity.Position.Y; y <= entity.Position.Y + 10 && y < 80; y++)
            {
                for (int x = entity.Position.X; x >= entity.Position.X - 10 && x >= 0; x--)
                {
                    rightDownEval += getEval(entity, x, y);
                }
            }

            int leftDownEval = 0;
            for (int y = entity.Position.Y; y <= entity.Position.Y + 10 && y < 80; y++)
            {
                for (int x = entity.Position.X; x <= entity.Position.X + 10 && x < 80; x++)
                {
                    leftDownEval += getEval(entity, x, y);
                }
            }

            int leftEval = entity.Position.X + 1 < 80 && Map[entity.Position.X + 1, entity.Position.Y] == null
                ? leftUpEval + leftDownEval
                : int.MinValue;

            int upEval = entity.Position.Y - 1 >= 0 && Map[entity.Position.X, entity.Position.Y - 1] == null
                ? leftUpEval + rightUpEval
                : int.MinValue;

            int rightEval = entity.Position.X - 1 >= 0 && Map[entity.Position.X - 1, entity.Position.Y] == null
                ? rightUpEval + rightDownEval
                : int.MinValue;

            int downEval = entity.Position.Y + 1 < 80 && Map[entity.Position.X, entity.Position.Y + 1] == null
                ? rightDownEval + leftDownEval
                : int.MinValue;

            if (leftEval != int.MinValue &&
                leftEval >= upEval &&
                leftEval >= rightEval &&
                leftEval >= downEval)
            {
                moveAction = new MoveAction(new Vec2Int(entity.Position.X + 1, entity.Position.Y), false,
                    false);
            }
            else if (upEval != int.MinValue &&
                     upEval >= leftEval &&
                     upEval >= rightEval &&
                     upEval >= downEval)
            {
                moveAction = new MoveAction(new Vec2Int(entity.Position.X, entity.Position.Y - 1), false,
                    false);
            }
            else if (rightEval != int.MinValue &&
                     rightEval >= upEval &&
                     rightEval >= leftEval &&
                     rightEval >= downEval)
            {
                moveAction = new MoveAction(new Vec2Int(entity.Position.X - 1, entity.Position.Y), false,
                    false);
            }
            else if (downEval != int.MinValue &&
                     downEval >= upEval &&
                     downEval >= rightEval &&
                     downEval >= leftEval)
            {
                moveAction = new MoveAction(new Vec2Int(entity.Position.X, entity.Position.Y + 1), false,
                    false);
            }

            if (moveAction != null)
            {
                Map[entity.Position.X, entity.Position.Y] = null;
                Map[moveAction.Value.Target.X, moveAction.Value.Target.Y] = entity;
            }

            entityActions.Add(entity.Id, new EntityAction(moveAction, null, null, null));
        }

        private static int BuilderUnitMoveEval(Entity entity, int x, int y)
        {
            if (Map[x, y] == null || 
                entity.Position.X == x && 
                entity.Position.Y == y)
            {
                return 0;
            }

            int eval = 0;
            var entityOnMap = Map[x, y].Value;
            var distance = GetDistance(entity.Position, x, y);

            if (entityOnMap.EntityType == EntityType.Resource)
            {
                eval += entityOnMap.Health / distance;
            }

            if (entityOnMap.EntityType == EntityType.BuilderUnit &&
                entityOnMap.PlayerId == MyId)
            {
                eval -= entityOnMap.Health / distance;
            }

            return eval;
        }

        public static int GetDistance(Vec2Int p1, Vec2Int p2) => Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);

        public static int GetDistance(Vec2Int p1, int x, int y) => Math.Abs(p1.X - x) + Math.Abs(p1.Y - y);

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }
    }
}