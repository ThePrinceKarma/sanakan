﻿#pragma warning disable 1591

using Sanakan.Database.Models;
using Sanakan.Services.PocketWaifu;

namespace Sanakan.Extensions
{
    public static class CardExtension
    {
        public static string GetString(this Card card, bool withoutId = false, bool withUpgrades = false, bool nameAsUrl = false)
        {
            string idStr = withoutId ? "" : $"**[{card.Id}]** ";
            string upgCnt = withUpgrades ? $"_(U:{card.UpgradesCnt})_" : "";
            string name = nameAsUrl ? $"[{card.Name}]({Shinden.API.Url.GetCharacterURL(card.Character)})" : card.Name; 
            
            return $"{idStr} {name} **{card.Rarity}** 🔥{card.Attack} 🛡{card.Defence} {upgCnt}";
        }

        public static string GetDesc(this Card card, string title = "")
        {
            return $"*{title}*\n\n"
                + $"**Relacja:** {card.GetAffectionString()}\n"
                + $"**Doświadczenie:** {card.ExpCnt.ToString("F")}\n"
                + $"**Dostępne ulepszenia:** {card.UpgradesCnt}\n\n"
                + $"**Aktywna:** {card.Active.GetYesNo()}\n"
                + $"**Możliwość wymiany:** {card.IsTradable.GetYesNo()}\n\n"
                + $"**Arena:** **W**: {card?.ArenaStats?.Wins} **L**: {card?.ArenaStats?.Loses} **D**: {card?.ArenaStats?.Draws}\n\n"
                + $"**WID:** {card.Id}\n\n";
        }

        public static string GetYesNo(this bool b) => b ? "Tak" : "Nie";

        public static string GetAffectionString(this Card card)
        {
            if (card.Affection <= -5) return "Nienawiść";
            if (card.Affection <= -4) return "Zawiść";
            if (card.Affection <= -3) return "Wrogość";
            if (card.Affection <= -2) return "Złośliwość";
            if (card.Affection <= -1) return "Chłodność";
            if (card.Affection >= 5)  return "Miłość";
            if (card.Affection >= 4)  return "Zauroczenie";
            if (card.Affection >= 3)  return "Przyjaźń";
            if (card.Affection >= 2)  return "Fascynacja";
            if (card.Affection >= 1)  return "Zaciekawienie";
            return "Obojętność";
        }

        public static int GetAttackMin(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SS: return 90;
                case Rarity.S:  return 80;
                case Rarity.A:  return 65;
                case Rarity.B:  return 50;
                case Rarity.C:  return 32;
                case Rarity.D:  return 20;

                case Rarity.E:
                default: return 1;
            }
        }

        public static int GetDefenceMin(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SS: return 77;
                case Rarity.S:  return 68;
                case Rarity.A:  return 60;
                case Rarity.B:  return 50;
                case Rarity.C:  return 32;
                case Rarity.D:  return 15;
                
                case Rarity.E:
                default: return 1;
            }
        }

        public static int GetAttackMax(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SS: return 100;
                case Rarity.S:  return 96;
                case Rarity.A:  return 87;
                case Rarity.B:  return 84;
                case Rarity.C:  return 68;
                case Rarity.D:  return 50;
                
                case Rarity.E:
                default: return 35;
            }
        }

        public static int GetDefenceMax(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SS: return 90;
                case Rarity.S:  return 78;
                case Rarity.A:  return 75;
                case Rarity.B:  return 70;
                case Rarity.C:  return 65;
                case Rarity.D:  return 53;
                
                case Rarity.E:
                default: return 38;
            }
        }
    }
}
