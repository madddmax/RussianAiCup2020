using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Aicup2020.MyModel;

namespace Aicup2020.MyActions
{
    public static class BuilderUnitActions
    {
        public static Vec2Int? GetApproxTarget(Entity entity)
        {
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

            return nearestTarget;
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
                    var repairAction = new RepairAction(targetEntity.Value.Id);
                    entityActions.Add(entity.Id, new EntityAction(null, null, null, repairAction));
                    break;
                }
            }
        }

        public static void SetBuild(EntityType buildEntityType, int size, Dictionary<int, EntityAction> entityActions)
        {
            foreach (Entity entity in ScoreMap.MyBuilderUnits)
            {
                if (entityActions.ContainsKey(entity.Id))
                {
                    continue;
                }

                if (ScoreMap.Get(entity.Position).AllDamage > 0)
                {
                    continue;
                }

                var buildPositions = entity.Position.BuildPositions(size);
                foreach (var position in buildPositions)
                {
                    var diagonals = position.Diagonals(size);
                    var neighbors = position.Neighbors(size);

                    if (ScoreMap.Passable(position, size) &&
                        diagonals.All(ScoreMap.PassableInFuture) &&
                        diagonals.All(d => ScoreMap.Get(d).AllDamage == 0) &&
                        neighbors.All(d => ScoreMap.Get(d).AllDamage == 0) &&
                        (size > 3 || neighbors.All(ScoreMap.PassableInFuture)))
                    {
                        var buildAction = new BuildAction(buildEntityType, position);
                        entityActions.Add(entity.Id, new EntityAction(null, buildAction, null, null));

                        ScoreMap.Build(position, size);
                        return;
                    }
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
    }
}