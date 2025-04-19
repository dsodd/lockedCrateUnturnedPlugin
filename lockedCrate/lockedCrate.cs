using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
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

            Logger.Log("LockedCrate loaded. Waiting for first player to spawn crate.");
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
                Logger.Log("First player connected, spawning crate...");
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

            byte x, y;
            if (!Regions.tryGetCoordinate(location, out x, out y))
            {
                Logger.LogError("Invalid spawn location — outside of map bounds.");
                return;
            }

            var asset = Assets.find(EAssetType.ITEM, Configuration.Instance.CrateId);
            var crateAsset = asset as ItemBarricadeAsset;
            if (crateAsset == null)
            {
                Logger.LogError($"Crate ID {Configuration.Instance.CrateId} not found or is not a barricade.");
                return;
            }

            var crate = new Barricade(crateAsset);

            try
            {
                Transform transform = BarricadeManager.dropNonPlantedBarricade(crate, location, Quaternion.identity, 0, 0);
                if (transform == null)
                {
                    Logger.LogError("Failed to drop barricade — transform is null.");
                    return;
                }

                spawnedCrate = BarricadeManager.FindBarricadeByRootTransform(transform);
                Logger.Log($"Locked crate spawned at {location}.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception while dropping barricade: {ex}");
            }
        }

        private void OnOpenStorageRequested(CSteamID steamID, InteractableStorage storage, ref bool shouldAllow)
        {
            if (spawnedCrate == null || storage == null)
                return;

            if (storage.transform == spawnedCrate.model || storage.transform.IsChildOf(spawnedCrate.model))
            {
                shouldAllow = false; // Block opening the crate
                Logger.Log($"Player [{steamID}] tried to open the locked crate — access denied.");
            }
        }
    }
}
