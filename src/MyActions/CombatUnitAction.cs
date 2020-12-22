using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Aicup2020.MyModel;

namespace Aicup2020.MyActions
{
    public static class CombatUnitAction
    {
        public static void SetAttack(Entity entity, int attackRange, int size, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            if (entity.EntityType == EntityType.RangedUnit &&
                ScoreMap.Get(entity.Position).MeleeDamage > 0)
            {
                return;
            }

            List<Vec2Int> range = new List<Vec2Int>();
            if (size > 1)
            {
                for (int y = entity.Position.Y; y < entity.Position.Y + size; y++)
                {
                    for (int x = entity.Position.X; x < entity.Position.X + size; x++)
                    {
                        var position = new Vec2Int(x, y);
                        range.AddRange(position.Range(attackRange));
                    }
                }

                range = range.Distinct().ToList();
            }
            else
            {
                range = entity.Position.Range(attackRange);
            }

            var enemiesUnderAttack = new List<Entity>();
            foreach (var position in range)
            {
                var cell = ScoreMap.Get(position);
                if (cell.Entity != null &&
                    cell.Entity?.EntityType != EntityType.Resource &&
                    cell.Entity?.PlayerId != ScoreMap.MyId)
                {
                    enemiesUnderAttack.Add(cell.Entity.Value);
                }
            }

            SetAttack(entity, EntityType.RangedUnit, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.BuilderUnit, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.MeleeUnit, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.Turret, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.RangedBase, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.MeleeBase, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.BuilderBase, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.House, enemiesUnderAttack, entityActions);
        }

        private static void SetAttack(Entity entity, EntityType attackedEntityType, List<Entity> enemiesUnderAttack, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            bool hasEnemy = enemiesUnderAttack.Any(e => e.EntityType == attackedEntityType);
            if (hasEnemy)
            {
                var enemy = enemiesUnderAttack.First(e => e.EntityType == attackedEntityType);
                var attackAction = new AttackAction(enemy.Id, null);
                entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));
            }
        }

        public static Vec2Int? GetAttackTarget(Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return null;
            }

            Vec2Int? target = null;
            int minDistance = int.MaxValue;

            foreach (var enemyTarget in ScoreMap.EnemyTargets)
            {
                var distance = enemyTarget.Distance(entity.Position);
                if (target == null || distance < minDistance)
                {
                    target = enemyTarget;
                    minDistance = distance;
                }
            }

            return target;
        }
    }
}