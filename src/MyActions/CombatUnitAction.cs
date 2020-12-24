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

            var enemiesUnderAttack = new HashSet<Entity>();
            foreach (var position in range)
            {
                var cell = ScoreMap.Get(position);
                if (cell.Entity != null &&
                    cell.Entity?.EntityType != EntityType.Resource &&
                    cell.Entity?.PlayerId != ScoreMap.MyId &&
                    !enemiesUnderAttack.Contains(cell.Entity.Value))
                {
                    enemiesUnderAttack.Add(cell.Entity.Value);
                }
            }

            SetAttack(entity, EntityType.RangedUnit, 1, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.BuilderUnit, 1, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.MeleeUnit, 1, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.Turret, 2, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.RangedBase, 5, enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.MeleeBase, 5,  enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.BuilderBase, 5,  enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.House, 3,  enemiesUnderAttack, entityActions);
            SetAttack(entity, EntityType.Wall, 1, enemiesUnderAttack, entityActions);
        }

        private static void SetAttack(Entity entity, EntityType attackedEntityType, int attackedEntitySize, HashSet<Entity> enemiesUnderAttack, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return;
            }

            Entity? bestEnemy = null;
            var enemies = enemiesUnderAttack.Where(e => e.EntityType == attackedEntityType).ToList();
            foreach (var enemy in enemies)
            {
                if (bestEnemy == null ||
                    enemy.Health < bestEnemy.Value.Health)
                {
                    bestEnemy = enemy;
                }

                if (enemy.Health == bestEnemy.Value.Health &&
                    entity.Position.Distance(enemy.Position) > entity.Position.Distance(bestEnemy.Value.Position))
                {
                    bestEnemy = enemy;
                }
            }

            if (bestEnemy != null)
            {
                var attackAction = new AttackAction(bestEnemy.Value.Id, null);
                entityActions.Add(entity.Id, new EntityAction(null, null, attackAction, null));

                var newEnemyEntity = bestEnemy.Value.Health > 5
                    ? new Entity
                    {
                        Active = bestEnemy.Value.Active,
                        EntityType = bestEnemy.Value.EntityType,
                        Health = bestEnemy.Value.Health - 5,
                        Id = bestEnemy.Value.Id,
                        Position = bestEnemy.Value.Position,
                        PlayerId = bestEnemy.Value.PlayerId
                    }
                    : (Entity?)null;

                if (attackedEntitySize > 1)
                {
                    for (int y = 0; y < attackedEntitySize; y++)
                    {
                        for (int x = 0; x < attackedEntitySize; x++)
                        {
                            ScoreMap.Set(bestEnemy.Value.Position.X + x, bestEnemy.Value.Position.Y + y, newEnemyEntity);
                        }
                    }
                }
                else
                {
                    ScoreMap.Set(bestEnemy.Value.Position, newEnemyEntity);
                }
            }
        }

        public static Vec2Int GetAttackTarget(Entity entity, Dictionary<int, EntityAction> entityActions)
        {
            if (entityActions.ContainsKey(entity.Id))
            {
                return ScoreMap.EnemyBase;
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

            return target ?? ScoreMap.EnemyBase;
        }
    }
}