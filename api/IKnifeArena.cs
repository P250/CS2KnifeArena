using CounterStrikeSharp.API.Core;

namespace CS2KnifeArena;

public enum MenuState 
{
    ARENA_SETUP,
    ARENA_CONFIG
}

public interface IKnifeArena 
{
    void OpenArenaMenu(CCSPlayerController player, MenuState menuState);
    void EnableArenaCreation(CCSPlayerController player);
    void DisableArenaCreation(CCSPlayerController player);
    
    
    
}