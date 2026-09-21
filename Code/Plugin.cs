using BepInEx;
using MonoDetour;
using R2API.Networking;
using RoR2;
namespace RefriedSkills;


[BepInAutoPlugin]
[BepInDependency(NetworkingAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(ModSupport.RiskyTweaksMod.GUID, BepInDependency.DependencyFlags.SoftDependency)]
public partial class Plugin : BaseUnityPlugin
{
    public static PluginInfo PluginInfo { get; private set; }
    public void Awake()
    {
        PluginInfo = Info;
        ConfigOptions.BindAllConfigOptions(Config);
        Log.Init(Logger);
        NetworkingAPI.RegisterMessageType<Bandit.LightsOut.SyncRechargesFromNewLightsOut>();
        MonoDetourManager.InvokeHookInitializers(typeof(Plugin).Assembly);
    }
}