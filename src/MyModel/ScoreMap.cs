using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;

namespace Aicup2020.MyModel
{
    public static class ScoreMap
    {
        public static readonly Vec2Int MyBase = new Vec2Int(20, 20);
        public static readonly Vec2Int EnemyBase = new Vec2Int(60, 60);

        private static readonly ScoreCell[,] Map = new ScoreCell[80, 80];
        public static ScoreCell Get(Vec2Int p) => Map[p.X, p.Y];
        public static ScoreCell Get(int x, int y) => Map[x, y];

        public static int MyId;
        public static int MyResource;

        public static List<Entity> MyProduction = new List<Entity>();
        public static List<Vec2Int> EnemyTargets = new List<Vec2Int>();

        public static List<Entity> Resources = new List<Entity>();
        public static List<Vec2Int> BuilderUnitTargets = new List<Vec2Int>();

        public static List<Entity> MyBuilderUnits = new List<Entity>();
        public static List<Entity> MyMeleeUnits = new List<Entity>();
        public static List<Entity> MyRangedUnits = new List<Entity>();

        public static List<Entity> MyActiveBuilderBases = new List<Entity>();
        public static List<Entity> MyNotActiveBuilderBases = new List<Entity>();

        public static List<Entity> MyActiveMeleeBases = new List<Entity>();
        public static List<Entity> MyNotActiveMeleeBases = new List<Entity>();

        public static List<Entity> MyActiveRangedBases = new List<Entity>();
        public static List<Entity> MyNotActiveRangedBases = new List<Entity>();

        public static List<Entity> MyActiveHouses = new List<Entity>();
        public static List<Entity> MyNotActiveHouses = new List<Entity>();

        public static int Limit;
        public static int AvailableLimit;

        public static EntityProperties BuilderUnitProperties;
        public static EntityProperties MeleeUnitProperties;
        public static EntityProperties RangedUnitProperties;

        public static EntityProperties BuilderBaseProperties;
        public static EntityProperties MeleeBaseProperties;
        public static EntityProperties RangedBaseProperties;
        public static EntityProperties HouseProperties;

        public static void InitMap(PlayerView playerView)
        {
            for (int y = 0; y < 80; y++)
            {
                for (int x = 0; x < 80; x++)
                {
                    Map[x, y] = new ScoreCell
                    {
                        ScoutScore = 1
                    };
                }
            }

            MyId = playerView.MyId;
            MyResource = playerView.Players[MyId - 1].Resource;

            MyProduction.Clear();
            EnemyTargets.Clear();

            Resources.Clear();
            BuilderUnitTargets.Clear();

            MyBuilderUnits.Clear();
            MyMeleeUnits.Clear();
            MyRangedUnits.Clear();

            MyActiveBuilderBases.Clear();
            MyNotActiveBuilderBases.Clear();

            MyActiveMeleeBases.Clear();
            MyNotActiveMeleeBases.Clear();

            MyActiveRangedBases.Clear();
            MyNotActiveRangedBases.Clear();

            MyActiveHouses.Clear();
            MyNotActiveHouses.Clear();

            BuilderUnitProperties = playerView.EntityProperties[EntityType.BuilderUnit];
            MeleeUnitProperties = playerView.EntityProperties[EntityType.MeleeUnit];
            RangedUnitProperties = playerView.EntityProperties[EntityType.RangedUnit];

            BuilderBaseProperties = playerView.EntityProperties[EntityType.BuilderBase];
            MeleeBaseProperties = playerView.EntityProperties[EntityType.MeleeBase];
            RangedBaseProperties = playerView.EntityProperties[EntityType.RangedBase];
            HouseProperties = playerView.EntityProperties[EntityType.House];

            foreach (Entity entity in playerView.Entities)
            {
                var properties = playerView.EntityProperties[entity.EntityType];
                if (properties.Size > 1)
                {
                    for (int y = entity.Position.Y; y < entity.Position.Y + properties.Size; y++)
                    {
                        for (int x = entity.Position.X; x < entity.Position.X + properties.Size; x++)
                        {
                            Map[x, y].Entity = entity;
                        }
                    }
                }
                else
                {
                    Map[entity.Position.X, entity.Position.Y].Entity = entity;
                }

                // my
                if (entity.PlayerId != MyId &&
                    entity.EntityType != EntityType.Resource)
                {
                    EnemyTargets.Add(entity.Position);
                }

                // enemy
                if (entity.PlayerId == MyId)
                {
                    // units
                    if (entity.EntityType == EntityType.BuilderUnit)
                    {
                        MyBuilderUnits.Add(entity);
                        MyProduction.Add(entity);
                    }

                    if (entity.EntityType == EntityType.MeleeUnit)
                    {
                        MyMeleeUnits.Add(entity);
                    }

                    if (entity.EntityType == EntityType.RangedUnit)
                    {
                        MyRangedUnits.Add(entity);
                    }

                    // buildings
                    if (entity.EntityType == EntityType.BuilderBase)
                    {
                        if (entity.Active)
                        {
                            MyActiveBuilderBases.Add(entity);
                        }
                        else
                        {
                            MyNotActiveBuilderBases.Add(entity);
                        }

                        MyProduction.Add(entity);
                    }

                    if (entity.EntityType == EntityType.MeleeBase)
                    {
                        if (entity.Active)
                        {
                            MyActiveMeleeBases.Add(entity);
                        }
                        else
                        {
                            MyNotActiveMeleeBases.Add(entity);
                        }

                        MyProduction.Add(entity);
                    }

                    if (entity.EntityType == EntityType.RangedBase)
                    {
                        if (entity.Active)
                        {
                            MyActiveRangedBases.Add(entity);
                        }
                        else
                        {
                            MyNotActiveRangedBases.Add(entity);
                        }

                        MyProduction.Add(entity);
                    }

                    if (entity.EntityType == EntityType.House)
                    {
                        if (entity.Active)
                        {
                            MyActiveHouses.Add(entity);
                        }
                        else
                        {
                            MyNotActiveHouses.Add(entity);
                        }

                        MyProduction.Add(entity);
                    }
                }
            }

            for (int y = 0; y < 80; y++)
            {
                for (int x = 0; x < 80; x++)
                {
                    if (Map[x, y].Entity == null)
                    {
                        continue;
                    }

                    Entity entity = Map[x, y].Entity.Value;
                    EntityProperties entityProperties = playerView.EntityProperties[entity.EntityType];

                    // scout
                    if (entity.PlayerId != MyId &&
                        entity.EntityType != EntityType.Resource)
                    {
                        var neighbors = entity.Position.Neighbors(entityProperties.SightRange);
                        foreach (var target in neighbors)
                        {
                            Map[target.X, target.Y].ScoutScore = 0;
                        }
                    }

                    // collect resources
                    if (entity.EntityType == EntityType.Resource)
                    {
                        Resources.Add(entity);

                        var neighbors = entity.Position.Neighbors();
                        foreach (var target in neighbors)
                        {
                            if (PassableInFuture(target))
                            {
                                Map[target.X, target.Y].ResourceScore = 1;
                                BuilderUnitTargets.Add(target);
                            }
                        }
                    }

                    // repair
                    if (entity.PlayerId == MyId && 
                        entity.Health < entityProperties.MaxHealth &&
                        (entity.EntityType == EntityType.BuilderBase ||
                         entity.EntityType == EntityType.MeleeBase ||
                         entity.EntityType == EntityType.RangedBase ||
                         entity.EntityType == EntityType.House ||
                         entity.EntityType == EntityType.Turret))
                    {
                        var neighbors = entity.Position.Neighbors(entityProperties.Size);
                        foreach (var target in neighbors)
                        {
                            if (PassableInFuture(target))
                            {
                                Map[target.X, target.Y].RepairScore = entity.Active ? 1 : 2;
                                BuilderUnitTargets.Add(target);
                            }
                        }
                    }

                    // check builders
                    if (entity.PlayerId == MyId &&
                        entity.EntityType == EntityType.BuilderUnit)
                    {
                        Map[entity.Position.X, entity.Position.Y].ResourceScore = 0;
                        Map[entity.Position.X, entity.Position.Y].RepairScore = 0;
                        BuilderUnitTargets.Remove(entity.Position);
                    }

                    // attack
                    if (entity.PlayerId != MyId &&
                        entity.EntityType != EntityType.Resource)
                    {
                        var neighbors = entity.Position.Neighbors(entityProperties.Size);
                        foreach (var target in neighbors)
                        {
                            if (PassableInFuture(target))
                            {
                                Map[target.X, target.Y].MeleeAttack = 1;
                            }
                        }

                        var position = new Vec2Int(x, y);
                        var range = position.Range(4, 3);
                        foreach (var target in range)
                        {
                            if (PassableInFuture(target))
                            {
                                Map[target.X, target.Y].RangedAttack = 1;
                            }
                        }
                    }

                    // check damage
                    if (entity.PlayerId != MyId &&
                        entity.EntityType == EntityType.MeleeUnit)
                    {
                        var range = entity.Position.Range(2);
                        foreach (var point in range)
                        {
                            Map[point.X, point.Y].MeleeDamage += 5;
                            Map[point.X, point.Y].ResourceScore = 0;
                            Map[point.X, point.Y].RepairScore = 0;
                            BuilderUnitTargets.Remove(point);
                        }
                    }

                    if (entity.PlayerId != MyId &&
                        entity.EntityType == EntityType.Turret)
                    {
                        var position = new Vec2Int(x, y);
                        var range = position.Range(5);
                        foreach (var point in range)
                        {
                            Map[point.X, point.Y].TurretDamage += 5;
                            Map[point.X, point.Y].ResourceScore = 0;
                            Map[point.X, point.Y].RepairScore = 0;
                            BuilderUnitTargets.Remove(point);
                        }
                    }

                    if (entity.PlayerId != MyId &&
                        entity.EntityType == EntityType.RangedUnit)
                    {
                        var range = entity.Position.Range(6);
                        foreach (var point in range)
                        {
                            Map[point.X, point.Y].RangedDamage += 5;
                            Map[point.X, point.Y].ResourceScore = 0;
                            Map[point.X, point.Y].RepairScore = 0;
                            BuilderUnitTargets.Remove(point);
                        }
                    }
                }
            }

            MyBuilderUnits = MyBuilderUnits
                .OrderBy(e => e.Position.Distance(MyBase))
                .ToList();

            Limit = MyBuilderUnits.Count +
                    MyMeleeUnits.Count +
                    MyRangedUnits.Count;

            AvailableLimit = (MyActiveBuilderBases.Count +
                              MyActiveMeleeBases.Count +
                              MyActiveRangedBases.Count +
                              MyActiveHouses.Count) * 5;
        }

        public static bool Passable(Vec2Int p)
        {
            return Map[p.X, p.Y].Entity == null;
        }

        public static bool PassableInFuture(Vec2Int p) => PassableInFuture(p.X, p.Y);

        public static bool PassableInFuture(int x, int y)
        {
            Entity? entity = Map[x, y].Entity;
            return entity == null ||
                   entity.Value.EntityType == EntityType.BuilderUnit ||
                   entity.Value.EntityType == EntityType.MeleeUnit ||
                   entity.Value.EntityType == EntityType.RangedUnit;
        }

        public static bool Passable(Vec2Int p, int size)
        {
            for (int x = 0; x < size; x++)
            {
                if (p.X + x >= 80)
                {
                    return false;
                }

                for (int y = 0; y < size; y++)
                {
                    if (p.Y + y >= 80)
                    {
                        return false;
                    }

                    if (Map[p.X + x, p.Y + y].Entity != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void Set(Vec2Int p, Entity? entity)
        {
            Map[p.X, p.Y].Entity = entity;
        }

        public static void Build(Vec2Int p, int size)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Map[p.X + x, p.Y + y].Entity = new Entity();
                }
            }
        }
    }
}