using Aicup2020.Model;

namespace Aicup2020.MyModel
{
    public class ScoreCell
    {
        public Entity? Entity;

        public int ResourceScore;

        public int RepairScore;

        public int MeleeAttack;

        public int RangedAttack;

        public int MeleeDamage;

        public int TurretDamage;

        public int RangedDamage;

        public int AllDamage => MeleeDamage + TurretDamage + RangedDamage;

        // other scores
    }
}