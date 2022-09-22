﻿using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace BannerKings.Managers.Populations
{
    public class MineralData : BannerKingsData
    {
        public MineralData(PopulationData data)
        {
            Settlement = data.Settlement;
            Composition = new Dictionary<MineralType, float>();
            Init();
        }

        [SaveableProperty(1)] private Settlement Settlement { get; set; }

        [SaveableProperty(2)] private Dictionary<MineralType, float> Composition { get; set; }

        [SaveableProperty(3)] public MineralRichness Richness { get; private set; }

        public MBReadOnlyDictionary<MineralType, float> Compositions => Composition.GetReadOnlyDictionary();

        public TerrainType Terrain => Campaign.Current.MapSceneWrapper != null ? 
            Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(Settlement.Position2D) : TerrainType.Plain;


        private void Init()
        {
            var mineral1 = MineralType.NONE;
            var mineral2 = MineralType.NONE;
            var mineral3 = MineralType.NONE;
            var terrain = Terrain;

            var mineral1Ratio = 0f;
            var mineral2Ratio = 0f;
            var mineral3Ratio = 0f;

            if (Settlement.IsVillage)
            {
                mineral1 = GetVillageMineral(Settlement.Village);
                if (mineral1 != MineralType.NONE)
                {
                    mineral1Ratio = MBRandom.RandomFloatRanged(0.6f, 0.9f);
                }
            }


            if (mineral1 == MineralType.NONE)
            {
                List<(MineralType, float)> options = new List<(MineralType, float)>();
                options.Add(new(MineralType.CLAY, 10f));
                options.Add(new(MineralType.SALT, terrain == TerrainType.Desert ? 12f : 6f));
                options.Add(new(MineralType.LIMESTONE, 20f));
                options.Add(new(MineralType.IRON, terrain == TerrainType.Mountain ? 4f : 2f));

                MineralType result = MBRandom.ChooseWeighted(options);
                mineral1 = result;
                mineral1Ratio = MBRandom.RandomFloatRanged(0.6f, 0.9f);
            }

            if (mineral2 == MineralType.NONE)
            {
                List<(MineralType, float)> options = new List<(MineralType, float)>();
                options.Add(new(MineralType.MARBLE, 10f));
                options.Add(new(MineralType.SILVER, 6f));
                if (mineral1 != MineralType.IRON)
                {
                    options.Add(new(MineralType.IRON, 20f));
                }
                  

                MineralType result = MBRandom.ChooseWeighted(options);
                mineral2 = result;
                mineral2Ratio = MBRandom.RandomFloatRanged(0.1f, 0.4f);
            }

            if (MBRandom.RandomFloat < 0.08f)
            {
                mineral3 = MineralType.GOLD;
                mineral3Ratio = MBRandom.RandomFloatRanged(0.01f, 0.03f);
            }

            var total = mineral1Ratio + mineral2Ratio + mineral3Ratio;
            if (total != 1f)
            {
                var diff = 1f - total;
                mineral2Ratio += diff;
            }

            Composition.Add(mineral1, mineral1Ratio);
            Composition.Add(mineral2, mineral2Ratio);
            if (mineral3 != MineralType.NONE)
            {
                Composition.Add(mineral3, mineral3Ratio);
            }

            var random = MBRandom.RandomFloat;
            var richness = MineralRichness.POOR;
            if (random < 0.6f)
            {
                richness = MineralRichness.POOR;
            }
            else if (random < 0.85f)
            {
                richness = MineralRichness.ADEQUATE;
            }
            else
            {
                richness = MineralRichness.RICH;
            }

            Richness = richness;
        }

        public MineralType GetVillageMineral(Village village)
        {
            var type = village.VillageType;
            if (type == DefaultVillageTypes.IronMine)
            {
                return MineralType.IRON;
            }
            else if (type == DefaultVillageTypes.SaltMine)
            {
                return MineralType.SALT;
            }
            else if (type == DefaultVillageTypes.ClayMine)
            {
                return MineralType.CLAY;
            }
            else if (type == DefaultVillageTypes.SilverMine)
            {
                return MineralType.SILVER;
            }

            return MineralType.NONE;
        }


        internal override void Update(PopulationData data)
        {
            
        }
    }

    public enum MineralRichness
    {
        RICH,
        ADEQUATE,
        POOR
    }

    public enum MineralType
    {
        CLAY,
        SALT,
        IRON,
        SILVER,
        LIMESTONE,
        MARBLE,
        GOLD,
        NONE
    }
}