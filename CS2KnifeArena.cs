using CounterStrikeSharp.API.Core;

namespace CS2KnifeArena;

public class CS2KnifeArena : BasePlugin
{
    public override string ModuleName => "CS2KnifeArena";
    public override string ModuleVersion => "v0.0.1";

    public override void Load(bool hotReload)
    {
        Console.WriteLine("Hello World!");
    }

}
