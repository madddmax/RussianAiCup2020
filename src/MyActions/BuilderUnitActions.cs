using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Aicup2020.MyModel;

namespace Aicup2020.MyActions
{
    public static class BuilderUnitActions
    {
        public static Vec2Int GetApproxTarget(Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return ScoreMap.MyBase;
            }

            Vec2Int? nearestTarget = null;
            int minDistance = int.MaxValue;

            foreach (var target in ScoreMap.BuilderUnitTargets)
            {
                var distance = target.Distance(entity.Position);

                if (nearestTarget == null || distance < minDistance)
                {
                    nearestTarget = target;
                    minDistance = distance;
                }
            }

            return nearestTarget ?? ScoreMap.MyBase;
        }

        public static void SetRepair(PlayerView playerView, Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            if (ScoreMap.Get(entity.Position).AllDamage > 0)
            {
                return;
            }

            Entity? builder = null;
            Entity? repairEntity = null;
            int maxHealth = int.MinValue;

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
                    targetEntity.Value.Health < entityProperties.MaxHealth &&
                    (targetEntity.Value.EntityType == EntityType.BuilderBase ||
                     targetEntity.Value.EntityType == EntityType.MeleeBase ||
                     targetEntity.Value.EntityType == EntityType.RangedBase ||
                     targetEntity.Value.EntityType == EntityType.House ||
                     targetEntity.Value.EntityType == EntityType.Turret))
                {
                    if (targetEntity.Value.Health > maxHealth)
                    {
                        builder = entity;
                        repairEntity = targetEntity;
                        maxHealth = targetEntity.Value.Health;
                    }
                }
            }

            if (builder != null && repairEntity != null)
            {
                var repairAction = new RepairAction(repairEntity.Value.Id);
                entityActions.Add(builder.Value.Id, new EntityAction(null, null, null, repairAction));
            }
        }

        public static void SetBuild(EntityType buildEntityType, int size, Dictionary<int, EntityAction> entityActions, DebugInterface debugInterface)
        {
            Entity? builder = null;
            Vec2Int? buildPosition = null;
            int minDistance = int.MaxValue;

            foreach (Entity entity in ScoreMap.MyBuilderUnits)
            {
                if (entityActions.ContainsKey(entity.Id))
                {
                    continue;
                }

                var range = entity.Position.Range(10);
                if (range.Any(e => ScoreMap.Get(e).AllDamage > 0))
                {
                    continue;
                }

                if (buildEntityType == EntityType.Turret)
                {
                    if (range.Count(e => ScoreMap.Get(e).Entity?.EntityType == EntityType.Resource) < 25 ||
                        range.Any(e => ScoreMap.Get(e).Entity?.EntityType == EntityType.Turret))
                    {
                        continue;
                    }
                }

                var buildPositions = entity.Position.BuildPositions(size);
                foreach (var position in buildPositions)
                {
                    var diagonals = position.Diagonals(size);

                    if (ScoreMap.Passable(position, size) &&
                        diagonals.All(ScoreMap.PassableInFutureOrResource))
                    {
                        int distance = buildEntityType == EntityType.House
                            ? position.Distance(ScoreMap.MyHouseBase)
                            : position.Distance(ScoreMap.EnemyBase);

                        if (distance < minDistance)
                        {
                            builder = entity;
                            buildPosition = position;
                            minDistance = distance;
                        }
                    }
                }
            }

            if (builder != null && buildPosition != null)
            {
                var buildAction = new BuildAction(buildEntityType, buildPosition.Value);
                entityActions.Add(builder.Value.Id, new EntityAction(null, buildAction, null, null));

                ScoreMap.Build(buildPosition.Value, size);

                if (Params.IsDebug)
                {
                    MyStrategy.DrawRegion(buildPosition.Value.X, buildPosition.Value.Y, MyStrategy.Lemon, debugInterface);
                }
            }
        }

        public static void SetAttack(Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            if (ScoreMap.Get(entity.Position).AllDamage > 0)
            {
                return;
            }

            Entity? resource = null;
            int minHealth = int.MaxValue;

            var neighbors = entity.Position.Neighbors();
            foreach (var target in neighbors)
            {
                var targetEntity = ScoreMap.Get(target).Entity;
                if (targetEntity?.EntityType == EntityType.Resource)
                {
                    if (targetEntity.Value.Health < minHealth)
                    {
                        resource = targetEntity;
                        minHealth = targetEntity.Value.Health;
                    }
                }
            }

            if (resource != null)
            {
                var attackAction = new AttackAction(resource.Value.Id, null);
                entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
            }
        }
    }
}