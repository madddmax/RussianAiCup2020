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

        public static int MyId;

        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            InitMap(playerView);
            MyId = playerView.MyId;

            var entityActions = new Dictionary<int, EntityAction>();

            var entitiesToRepair = playerView.Entities
                .Where(e =>
                    e.PlayerId == MyId &&
                    e.Health < playerView.EntityProperties[e.EntityType].MaxHealth
                ).ToList();

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

            Entity? builderBase = playerView.Entities
                .FirstOrDefault(e =>
                    e.PlayerId == MyId &&
                    e.Active &&
                    e.EntityType == EntityType.BuilderBase
                );

            foreach (Entity entity in playerView.Entities)
            {
                if (entity.PlayerId != null && entity.PlayerId != MyId)
                {
                    continue;
                }

                if (entity.PlayerId != MyId)
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
                        moveAction = GetMoveAction(entity, BuilderUnitEval);

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

        private static MoveAction? GetMoveAction(Entity entity, Func<Entity, int, int, int> getEval)
        {
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

            if (leftEval > upEval &&
                leftEval > rightEval &&
                leftEval > downEval)
            {
                moveAction = new MoveAction(new Vec2Int(entity.Position.X + 1, entity.Position.Y), false,
                    false);
            }
            else if (upEval > leftEval &&
                     upEval > rightEval &&
                     upEval > downEval)
            {
                moveAction = new MoveAction(new Vec2Int(entity.Position.X, entity.Position.Y - 1), false,
                    false);
            }
            else if (rightEval > upEval &&
                     rightEval > leftEval &&
                     rightEval > downEval)
            {
                moveAction = new MoveAction(new Vec2Int(entity.Position.X - 1, entity.Position.Y), false,
                    false);
            }
            else if (downEval > upEval &&
                     downEval > rightEval &&
                     downEval > leftEval)
            {
                moveAction = new MoveAction(new Vec2Int(entity.Position.X, entity.Position.Y + 1), false,
                    false);
            }

            return moveAction;
        }

        private static int BuilderUnitEval(Entity entity, int x, int y)
        {
            if (Map[x, y] == null)
            {
                return 0;
            }

            int eval = 0;
            var entityOnMap = Map[x, y].Value;

            if (entityOnMap.EntityType == EntityType.Resource)
            {
                var distance = GetDistance(entity.Position, x, y);
                eval += entityOnMap.Health / distance;
            }

            if (entityOnMap.EntityType == EntityType.BuilderUnit &&
                entityOnMap.PlayerId == MyId)
            {
                eval -= entityOnMap.Health;
            }

            return eval;
        }

        public static int GetDistance(Vec2Int p1, int x, int y) => Math.Abs(p1.X - x) + Math.Abs(p1.Y - y);

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }
    }
}