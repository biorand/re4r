﻿using System;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class ItemModifier : Modifier
    {
        private static string[] GetDataFiles(ChainsawRandomizer randomizer)
        {
            if (randomizer.Campaign == Campaign.Leon)
            {
                return
                [
                    "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2",
                    "natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_1401/itemdefinitionuserdata_dlc_1401.user.2",
                    "natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_1402/itemdefinitionuserdata_dlc_1402.user.2"
                ];
            }
            else
            {
                return
                [
                    "natives/stm/_anotherorder/appsystem/ui/userdata/itemdefinitionuserdata_ao.user.2",
                    "natives/stm/_anotherorder/appsystem/ui/userdata/itemdefinitionuserdata_ovr_ao.user.2"
                ];
            }
        }

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var fileRepository = randomizer.FileRepository;
            foreach (var path in GetDataFiles(randomizer))
            {
                var itemDefinitions = fileRepository.GetUserFile(path);
                var datas = itemDefinitions.RSZ!.ObjectList[0].GetArray<RszInstance>("_Datas");
                foreach (var data in datas)
                {
                    var itemId = data.Get<int>("_ItemId");
                    var itemDefinition = ItemDefinitionRepository.Default.Find(itemId);
                    if (itemDefinition == null)
                        continue;

                    if (!IsStackable(itemDefinition))
                        continue;

                    var stackMax = IsWeapon(itemDefinition) ?
                        data.Get("_WeaponDefineData._StackMax") :
                        data.Get("_ItemDefineData._StackMax");

                    logger.LogLine($"{itemDefinition.Name}, stack = {stackMax}");
                }
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var stackMultiplier = randomizer.GetConfigOption<double>("inventory-stack-multiplier", 1);
            if (stackMultiplier == 1)
                return;

            var fileRepository = randomizer.FileRepository;
            foreach (var path in GetDataFiles(randomizer))
            {
                fileRepository.ModifyUserFile(path, (_, root) =>
                {
                    var datas = root.GetArray<RszInstance>("_Datas");
                    foreach (var data in datas)
                    {
                        var itemId = data.Get<int>("_ItemId");
                        var itemDefinition = ItemDefinitionRepository.Default.Find(itemId);
                        if (itemDefinition == null)
                            continue;

                        if (!IsStackable(itemDefinition))
                            continue;

                        var key = IsWeapon(itemDefinition) ?
                            "_WeaponDefineData._StackMax" :
                            "_ItemDefineData._StackMax";

                        var stackMax = data.Get<int>(key);
                        var newStackMax = Math.Clamp((int)Math.Round(stackMax * stackMultiplier), 1, 999);
                        data.Set(key, newStackMax);
                    }
                });
            }
        }

        private static bool IsStackable(ItemDefinition definition)
        {
            // Green herbs combine
            if (definition.Id == ItemIds.HerbG) return false;
            return
                definition.Kind == ItemKinds.Ammo ||
                definition.Kind == ItemKinds.Grenade ||
                definition.Kind == ItemKinds.Knife ||
                definition.Kind == ItemKinds.Gunpowder ||
                definition.Kind == ItemKinds.Resource ||
                definition.Kind == ItemKinds.Egg ||
                definition.Kind == ItemKinds.Fish ||
                definition.Kind == ItemKinds.Health;
        }

        private static bool IsWeapon(ItemDefinition definition)
        {
            return
                definition.Kind == ItemKinds.Weapon ||
                definition.Kind == ItemKinds.Grenade ||
                definition.Kind == ItemKinds.Knife ||
                definition.Kind == ItemKinds.Egg;
        }
    }
}
