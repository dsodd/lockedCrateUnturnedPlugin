using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace lockedCrate
{
    public class LockedCrate : RocketPlugin<LockedCrateConfiguration>
    {
        private BarricadeDrop spawnedCrate;

        protected override void Load()
        {
            U.Events.OnPlayerConnected += OnPlayerConnected;
            BarricadeManager.onOpenStorageRequested += OnOpenStorageRequested;

            SpawnLockedCrate();
            Logger.Log("LockedCrate loaded.");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            BarricadeManager.onOpenStorageRequested -= OnOpenStorageRequested;
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (spawnedCrate == null)
            {
                SpawnLockedCrate();
            }
        }

        private void SpawnLockedCrate()
        {
            if (Configuration.Instance.SpawnLocations == null || Configuration.Instance.SpawnLocations.Count == 0)
            {
                Logger.LogError("No spawn locations defined in configuration.");
                return;
            }

            var random = new System.Random();
            var location = Configuration.Instance.SpawnLocations[random.Next(Configuration.Instance.SpawnLocations.Count)].ToVector3();

            var asset = Assets.find(EAssetType.ITEM, Configuration.Instance.CrateId);
            var crateAsset = asset as ItemBarricadeAsset;
            if (crateAsset == null)
            {
                Logger.LogError($"Crate ID {Configuration.Instance.CrateId} not found or is not a barricade.");
                return;
            }

            var crate = new Barricade(crateAsset);
            Transform transform = BarricadeManager.dropNonPlantedBarricade(crate, location, Quaternion.identity, 0, 0);
            spawnedCrate = BarricadeManager.FindBarricadeByRootTransform(transform);
        }

        private void OnOpenStorageRequested(CSteamID steamID, InteractableStorage storage, ref bool shouldAllow)
        {
            if (spawnedCrate == null || storage == null)
                return;

            if (storage.transform == spawnedCrate.model || storage.transform.IsChildOf(spawnedCrate.model))
            {
                shouldAllow = false; // block opening the crate
            }
        }
    }
}
