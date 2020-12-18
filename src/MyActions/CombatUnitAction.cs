using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;
using Aicup2020.MyModel;

namespace Aicup2020.MyActions
{
    public static class CombatUnitAction
    {
        public static Vec2Int? SetAttack(Entity entity, int range, Dictionary<int, EntityAction> entityActions)
        {
            Vec2Int? target = null;
            int minDistance = int.MaxValue;

            var enemiesUnderAttack = new List<Entity>();
            foreach (var enemy in ScoreMap.Enemies)
            {
                var distance = enemy.Position.Distance(entity.Position);
                if (distance <= range)
                {
                    enemiesUnderAttack.Add(enemy);
                }

                if (target == null || distance < minDistance)
                {
                    target = enemy.Position;
                    minDistance = distance;
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

            return target;
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
    }
}