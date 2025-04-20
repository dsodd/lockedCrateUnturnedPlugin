using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Timers;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace lockedCrate
{
    public class LockedCrate : RocketPlugin<LockedCrateConfiguration>
    {
        private BarricadeDrop spawnedCrate;
        private bool isCrateLocked = true;
        private bool unlockTimerStarted = false;

        private Timer despawnTimer;

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

            despawnTimer?.Stop();
            despawnTimer?.Dispose();
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

            if (!Regions.tryGetCoordinate(location, out byte x, out byte y))
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
                isCrateLocked = true;
                unlockTimerStarted = false;

                Logger.Log($"Locked crate spawned at {location}.");

                StartDespawnTimer();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception while dropping barricade: {ex}");
            }
        }

        private void StartDespawnTimer()
        {
            despawnTimer?.Stop();
            despawnTimer?.Dispose();

            Logger.Log($"Despawn timer started ({Configuration.Instance.DespawnTimer}s).");

            despawnTimer = new Timer(Configuration.Instance.DespawnTimer * 1000);
            despawnTimer.AutoReset = false;
            despawnTimer.Elapsed += (sender, args) =>
            {
                Logger.Log("Despawn timer finished. Crate would be removed now.");
            };
            despawnTimer.Start();
        }

        private void OnOpenStorageRequested(CSteamID steamID, InteractableStorage storage, ref bool shouldAllow)
        {
            if (spawnedCrate == null || storage == null)
                return;

            if (storage.transform == spawnedCrate.model || storage.transform.IsChildOf(spawnedCrate.model))
            {
                if (isCrateLocked)
                {
                    shouldAllow = false;

                    if (!unlockTimerStarted)
                    {
                        unlockTimerStarted = true;
                        Logger.Log($"Player [{steamID}] triggered crate unlock timer ({Configuration.Instance.UnlockTimer}s).");

                        Timer timer = new Timer(Configuration.Instance.UnlockTimer * 1000);
                        timer.Elapsed += (sender, args) =>
                        {
                            timer.Stop();
                            timer.Dispose();
                            isCrateLocked = false;

                            Logger.Log("Crate is now unlocked!");
                            FillCrateWithItems();
                        };
                        timer.AutoReset = false;
                        timer.Start();
                    }
                    else
                    {
                        Logger.Log($"Player [{steamID}] attempted to open crate — still locked.");
                    }
                }
                else
                {
                    Logger.Log($"Player [{steamID}] opened the unlocked crate.");
                }
            }
        }

        private void FillCrateWithItems()
        {
            if (!(spawnedCrate?.interactable is InteractableStorage storage))
                return;

            ushort spawnTableID = Configuration.Instance.SpawnTable;

            var spawnTableAsset = Assets.find(EAssetType.SPAWN, spawnTableID) as SpawnAsset;
            if (spawnTableAsset == null)
            {
                Logger.LogError($"Invalid spawn table ID: {spawnTableID}");
                return;
            }

            Logger.Log($"Item spawn table ID valid: {spawnTableID}");
            Logger.Log($"SpawnTableAsset hash: {spawnTableAsset.hash}");

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var rand = new System.Random();
                int targetCount = rand.Next(Configuration.Instance.ItemCountMin, Configuration.Instance.ItemCountMax + 1);
                Logger.Log($"Target: spawn {targetCount} items into crate.");

                int added = 0;
                int attempts = 0;
                int maxAttempts = targetCount * 5;

                while (added < targetCount && attempts < maxAttempts)
                {
                    attempts++;

                    try
                    {
                        var resolvedAsset = SpawnTableTool.Resolve(spawnTableAsset, EAssetType.ITEM, () => $"Item attempt {attempts}");

                        if (resolvedAsset is ItemAsset itemAsset)
                        {
                            Logger.Log($"Attempt {attempts}: Trying to add {itemAsset.itemName} (ID: {itemAsset.id})");

                            var item = new Item(itemAsset.id, true);
                            bool success = storage.items.tryAddItem(item);

                            if (success)
                            {
                                added++;
                                Logger.Log($"Success! Added {itemAsset.itemName} (ID: {itemAsset.id}) [{added}/{targetCount}]");
                            }
                            else
                            {
                                Logger.LogWarning($"Attempt {attempts}: Not enough space for {itemAsset.itemName} (ID: {itemAsset.id})");
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"Attempt {attempts}: Resolved asset is not an ItemAsset.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Exception during item spawn attempt {attempts}: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                int totalItems = storage.items.getItemCount();
                Logger.Log($"Finished item spawning. Requested: {targetCount}, Successfully added: {added}, Storage now contains: {totalItems} items.");
            });
        }

    }
}
