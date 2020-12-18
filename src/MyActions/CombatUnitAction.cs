using Aicup2020.Model;
using Aicup2020.MyModel;

namespace Aicup2020.MyActions
{
    public static class CombatUnitAction
    {
        public static Vec2Int? GetApproxTarget(Entity entity)
        {
            Vec2Int? target = null;
            int minDistance = int.MaxValue;

            foreach (var enemy in ScoreMap.Enemies)
            {
                var distance = enemy.Position.Distance(entity.Position);

                if (target == null || distance < minDistance)
                {
                    target = enemy.Position;
                    minDistance = distance;
                }
            }

            return target;
        }
    }
}