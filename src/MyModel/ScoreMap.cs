﻿using System.Collections.Generic;
using System.Linq;
using Aicup2020.Model;

namespace Aicup2020.MyModel
{
    public static class ScoreMap
    {
        private static readonly Vec2Int MyBase = new Vec2Int(0, 0);

        public static readonly EntityType[] UnitTargets =
        {
            EntityType.MeleeUnit,
            EntityType.RangedUnit,
            EntityType.BuilderUnit,
            EntityType.Turret,
            EntityType.House,
            EntityType.BuilderBase,
            EntityType.MeleeBase,
            EntityType.RangedBase,
            EntityType.Wall
        };

        private static readonly ScoreCell[,] Map = new ScoreCell[80, 80];
        public static ScoreCell Get(Vec2Int p) => Map[p.X, p.Y];

        public static int MyId;
        public static int MyResource;

        public static List<Entity> Resources = new List<Entity>();
        public static List<Entity> Enemies = new List<Entity>();
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
                    Map[x, y] = new ScoreCell();
                }
            }

            MyId = playerView.MyId;
            MyResource = playerView.Players[MyId - 1].Resource;

            Resources.Clear();
            Enemies.Clear();
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

                if (entity.PlayerId != MyId &&
                    entity.EntityType != EntityType.Resource)
                {
                    Enemies.Add(entity);
                }

                if (entity.PlayerId == MyId)
                {
                    // units
                    if (entity.EntityType == EntityType.BuilderUnit)
                    {
                        MyBuilderUnits.Add(entity);
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

                    // attack
                    if (entity.PlayerId != MyId &&
                        entity.EntityType != EntityType.Resource)
                    {
                        var neighbors = entity.Position.Neighbors(entityProperties.Size);
                        foreach (var target in neighbors)
                        {
                            if (PassableInFuture(target))
                            {
                                Map[target.X, target.Y].AttackScore = 1;
                            }
                        }
                    }

                    // check damage
                    if (entity.PlayerId != MyId &&
                        (entity.EntityType == EntityType.MeleeUnit ||
                         entity.EntityType == EntityType.RangedUnit ||
                         entity.EntityType == EntityType.Turret))
                    {
                        int size = entity.EntityType != EntityType.MeleeUnit ? 5 : 1;
                        var range = entity.Position.Range(size + 1);
                        foreach (var point in range)
                        {
                            Map[point.X, point.Y].DamageScore += 5;
                        }
                    }
                }
            }

            Resources = Resources
                .OrderBy(e => e.Position.Distance(MyBase))
                .ToList();

            Enemies = Enemies
                .OrderBy(e => e.Position.Distance(MyBase))
                .ToList();

            MyBuilderUnits = MyBuilderUnits
                .OrderBy(e => e.Position.Distance(MyBase))
                .ToList();

            MyActiveHouses = MyActiveHouses
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