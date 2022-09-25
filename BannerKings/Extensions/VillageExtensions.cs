﻿using TaleWorlds.CampaignSystem.Settlements;

namespace BannerKings.Extensions
{
    public static class VillageExtensions
    {

        public static bool IsMiningVillage(this Village village)
        {
            var type = village.VillageType;
            return type == DefaultVillageTypes.SilverMine || type == DefaultVillageTypes.IronMine ||
                type == DefaultVillageTypes.SaltMine || type == DefaultVillageTypes.ClayMine;
        }

        public static bool IsFarmingVillage(this Village village)
        {
            var type = village.VillageType;
            return type == DefaultVillageTypes.WheatFarm || type == DefaultVillageTypes.DateFarm ||
                type == DefaultVillageTypes.FlaxPlant || type == DefaultVillageTypes.SilkPlant || 
                type == DefaultVillageTypes.OliveTrees || type == DefaultVillageTypes.VineYard;
        }

        public static bool IsAnimalVillage(this Village village)
        {
            var type = village.VillageType;
            return type == DefaultVillageTypes.CattleRange || type == DefaultVillageTypes.HogFarm ||
                     type == DefaultVillageTypes.SheepFarm || type == DefaultVillageTypes.BattanianHorseRanch || 
                     type == DefaultVillageTypes.DesertHorseRanch || type == DefaultVillageTypes.EuropeHorseRanch || 
                     type == DefaultVillageTypes.SteppeHorseRanch || type == DefaultVillageTypes.SturgianHorseRanch ||
                     type == DefaultVillageTypes.VlandianHorseRanch;
        }
    }
}
