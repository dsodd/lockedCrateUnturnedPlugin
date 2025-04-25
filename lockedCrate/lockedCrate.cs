using Rocket.API;
using Rocket.Unturned.Player;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
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
        private Timer respawnTimer;

        protected override void Load()
        {
            U.Events.OnPlayerConnected += OnPlayerConnected;
            BarricadeManager.onOpenStorageRequested += OnOpenStorageRequested;

            Logger.Log("LockedCrate loaded.");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            BarricadeManager.onOpenStorageRequested -= OnOpenStorageRequested;

            Logger.Log("LockedCrate unloaded.");

            despawnTimer?.Stop();
            despawnTimer?.Dispose();
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            ClearAllCratesById();

            if (spawnedCrate == null)
            {
                DebugLog("First player connected, spawning crate...");
                StartRespawnTimer();
            }
        }

        private void ClearAllCratesById()
        {
            ushort crateId = Configuration.Instance.CrateId;

            for (byte x = 0; x < BarricadeManager.BARRICADE_REGIONS; x++)
            {
                for (byte y = 0; y < BarricadeManager.BARRICADE_REGIONS; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == null || region.drops == null) continue;

                    for (int i = region.drops.Count - 1; i >= 0; i--)
                    {
                        var drop = region.drops[i];
                        if (drop.asset.id == crateId)
                        {
                            byte dropX = (byte)drop.model.transform.position.x;
                            byte dropY = (byte)drop.model.transform.position.y;
                            ushort dropInstanceID = (ushort)drop.model.transform.GetInstanceID();

                            BarricadeManager.destroyBarricade(region, dropX, dropY, drop.asset.id, dropInstanceID);
                            DebugLog($"Destroyed crate with ID {crateId} at {drop.model.transform.position}");
                        }
                    }
                }
            }
        }

        private void SpawnLockedCrate()
        {
            if (Configuration.Instance.SpawnLocations == null || Configuration.Instance.SpawnLocations.Count == 0)
            {
                DebugLogErr("No spawn locations defined in configuration.");
                return;
            }

            var random = new System.Random();
            var randomLocation = Configuration.Instance.SpawnLocations[random.Next(Configuration.Instance.SpawnLocations.Count)];

            Vector3 location = randomLocation.ToVector3();
            string name = randomLocation.Name;

            if (!Regions.tryGetCoordinate(location, out byte x, out byte y))
            {
                DebugLogErr("Invalid spawn location — outside of map bounds.");
                return;
            }
            
            var asset = Assets.find(EAssetType.ITEM, Configuration.Instance.CrateId);
            var crateAsset = asset as ItemBarricadeAsset;
            if (crateAsset != null)
            {
                //
            } 
            else
            {
                DebugLogErr($"Crate ID {Configuration.Instance.CrateId} not found or is not a barricade.");
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

                Logger.Log($"Locked crate spawned at {name} ({location}).");
                UnturnedChat.Say($"The Locked Crate has been spawned at {name}!");

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

            Logger.Log($"Despawn timer started ({(Configuration.Instance.DespawnTimer/60)} minutes).");

            despawnTimer = new Timer(Configuration.Instance.DespawnTimer * 1000)
            {
                AutoReset = false
            };
            despawnTimer.Elapsed += (sender, args) =>
            {
                // run on main thread since we're interacting with Unity stuff
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    if (spawnedCrate == null)
                    {
                        DebugLogWarn("Despawn timer triggered but no crate info available.");
                        return;
                    }

                    BarricadeDrop drop = spawnedCrate;
                    if (!BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
                    {
                        DebugLogErr("Failed to find crate region during despawn.");
                        return;
                    }

                    int index = region.drops.IndexOf(drop);
                    if (index < 0)
                    {
                        Logger.LogError("Crate not found in region drops list.");
                        return;
                    }

                    DebugLog($"Despawning crate at region x:{x} y:{y}, plant:{plant}, index:{index}");

                    BarricadeManager.destroyBarricade(region, x, y, plant, (ushort)index);
                    StartRespawnTimer();
                    spawnedCrate = null;
                    isCrateLocked = true;
                    unlockTimerStarted = false;

                    UnturnedChat.Say($"The locked crate at {name} has despawned!");
                });
            };
            despawnTimer.Start();
        }

        private void StartRespawnTimer()
        {
            respawnTimer?.Stop();
            respawnTimer?.Dispose();

            DebugLog($"Respawn timer started ({(Configuration.Instance.DespawnTimer / 60)} minutes).");

            respawnTimer = new Timer(Configuration.Instance.RespawnTimerMin * 1000)
            {
                AutoReset = false
            };
            respawnTimer.Elapsed += (sender, args) =>
            {
                // Run on main thread since we're interacting with Unity stuff
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    DebugLog("Respawn timer ended!");

                    SpawnLockedCrate();
                });
            };
            respawnTimer.Start();
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
                        // stop despawn timer
                        despawnTimer?.Stop();
                        despawnTimer?.Dispose();
                        despawnTimer = null;

                        DebugLog("Despawn timer paused due to player interaction.");

                        unlockTimerStarted = true;
                        DebugLog($"Player [{steamID}] triggered crate unlock timer ({(Configuration.Instance.DespawnTimer / 60)} minutes).");
                        SendAreaMessage(spawnedCrate, $"The crate will be unlocked in {(Configuration.Instance.DespawnTimer / 60)} minutes!");

                        Timer timer = new Timer(Configuration.Instance.UnlockTimer * 1000);
                        timer.Elapsed += (sender, args) =>
                        {
                            timer.Stop();
                            timer.Dispose();
                            isCrateLocked = false;

                            DebugLog($"The crate at {name} is now unlocked!");
                            
                            FillCrateWithItems();
                            StartDespawnTimer();
                        };
                        timer.AutoReset = false;
                        timer.Start();
                    }
                    else
                    {
                        DebugLog($"Player [{steamID}] attempted to open crate — still locked.");
                        SendAreaMessage(spawnedCrate, $"The crate unlock timer has already begun, total time is {(Configuration.Instance.DespawnTimer / 60)} minutes.");
                    }
                }
            }
        }

        private void FillCrateWithItems()
        {
            if (!(spawnedCrate?.interactable is InteractableStorage storage))
                return;

            ushort spawnTableID = Configuration.Instance.SpawnTable;

            var spawnTableAsset = Assets.find(EAssetType.SPAWN, spawnTableID) as SpawnAsset;
            if (spawnTableAsset != null)
            {
                //
            } 
            else
            {
                DebugLogErr($"Invalid spawn table ID: {spawnTableID}");
                return;
            }

            DebugLog($"Item spawn table ID valid: {spawnTableID}");
            DebugLog($"SpawnTableAsset hash: {spawnTableAsset.hash}");

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var rand = new System.Random();
                int targetCount = rand.Next(Configuration.Instance.ItemCountMin, Configuration.Instance.ItemCountMax + 1);
                DebugLog($"Target: spawn {targetCount} items into crate.");

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
                            DebugLog($"Attempt {attempts}: Trying to add {itemAsset.itemName} (ID: {itemAsset.id})");

                            var item = new Item(itemAsset.id, true);
                            bool success = storage.items.tryAddItem(item);

                            if (success)
                            {
                                added++;
                                DebugLog($"Success! Added {itemAsset.itemName} (ID: {itemAsset.id}) [{added}/{targetCount}]");
                            }
                            else
                            {
                                DebugLogWarn($"Attempt {attempts}: Not enough space for {itemAsset.itemName} (ID: {itemAsset.id})");
                            }
                        }
                        else
                        {
                            DebugLogWarn($"Attempt {attempts}: Resolved asset is not an ItemAsset.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Exception during item spawn attempt {attempts}: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                int totalItems = storage.items.getItemCount();
                Logger.Log($"Finished item spawning. Requested: {targetCount}, Successfully added: {added}, Storage now contains: {totalItems} items.");

                SendAreaMessage(spawnedCrate, $"The crate is now unlocked!");
            });
        }

        private void DebugLog(string logMessage)
        {
            if ( Configuration.Instance.DebugLogs == true )
            {
                string logMessage1 = "[Debug] " + logMessage;
                Logger.Log(logMessage1);
            }
        }

        private void DebugLogWarn(string logMessage)
        {
            if ( Configuration.Instance.DebugLogs == true)
            {
                string logMessage1 = "[Warn] " + logMessage;
                Logger.LogWarning(logMessage1);
            }
        }

        private void DebugLogErr(string logMessage)
        {
            if ( Configuration.Instance.DebugLogs == true)
            {
                string logMessage1 = "[Error] " + logMessage;
                Logger.LogError(logMessage1);
            }
        }

        private void SendAreaMessage(BarricadeDrop barricadeDrop, string message, float? preRadius = null)
        {
            float radius = preRadius ?? Configuration.Instance.AreaMessageDistance;

            Vector3 cratePosition = barricadeDrop.model.transform.position;

            foreach (var player in Provider.clients)
            {
                UnturnedPlayer unturnedPlayer = UnturnedPlayer.FromSteamPlayer(player);

                if (Vector3.Distance(unturnedPlayer.Position, cratePosition) <= radius)
                {
                    Logger.Log($"Checking player {unturnedPlayer.CharacterName}, distance: {Vector3.Distance(unturnedPlayer.Position, cratePosition)}");

                    UnturnedChat.Say(unturnedPlayer, message);
                }
            }
        }
    }
}
