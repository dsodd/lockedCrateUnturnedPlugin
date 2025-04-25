# Unturned Locked Crate Plugin
## Example config
```
<?xml version="1.0" encoding="utf-8"?>
<LockedCrateConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <CrateId>52015</CrateId>
  <SpawnTable>1</SpawnTable>
  <ItemCountMin>3</ItemCountMin>
  <ItemCountMax>5</ItemCountMax>
  <UnlockTimer>15</UnlockTimer>
  <RespawnTimerMin>30</RespawnTimerMin>
  <RespawnTimerMax>30</RespawnTimerMax>
  <DespawnTimer>15</DespawnTimer>
  <SpawnLocations>
    <Location>
      <x>0</x>
      <y>46</y>
      <z>0</z>
    </Location>
    <Location>
      <x>10</x>
      <y>46</y>
      <z>10</z>
    </Location>
  </SpawnLocations>
</LockedCrateConfiguration>
```
## Config/values explanations and docs
- <CrateId> - Defines the storage asset that should be used by the plugin to store the items in.
- <SpawnTable> - Defines the Unturned spawntable that will be used to add loot to the crate.
- <ItemCountMin> - Minimum item amout that will spawn.
- <ItemCountMax> - Maximum item amout that will spawn.
- <UnlockTimer> - The amout of time it will take for the crate to unlock after its first interacted with. (seconds)
- <RespawnTimerMin> - Minimum amout of time a crate will take to respawn. (seconds)
- <RespawnTimerMax> - Maximum amout of time a crate will take to respawn. (seconds)
- <DespawnTimer> - This timer is started as soon as a crate spawns, stopped while its being unlocked and restarted after the unlocking, once it reaches 0, the crate is destroyed. (seconds)
- <SpawnLocations> - Defines a list of all locations where a crate can spawn at random.
- <Location> - Sections off each specific location. The <x>, <y> and <z> and the coordinates of where it should spawn in the map.
## Notes
- This plugin still needs fine tuning and is nowhere near perfect so if you have any suggestions/bug reports please feel free to contact me on discord. (user: dsodd)
- There are a few obvious flaws that I will start working on asap as I've noticed them myself.
- This plugin was originally a "Rusturned" server plugin, but as I worked on it more, I realized that it could actually be used for many semi-rp servers and others of such kind.
