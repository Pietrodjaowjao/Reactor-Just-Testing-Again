using System;
using System.Linq;
using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using Reactor.Utilities.Attributes;
using Reactor.Utilities.ImGui;
using UnityEngine;

namespace Reactor.Debugger;

[BepInAutoPlugin("gg.reactor.debugger")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class DebuggerPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);
    public DebuggerComponent Component { get; private set; } = null!;

    public override void Load()
    {
        Component = this.AddComponent<DebuggerComponent>();

        GameOptionsData.MaxImpostors = GameOptionsData.RecommendedImpostors = Enumerable.Repeat((int) byte.MaxValue, byte.MaxValue).ToArray();
        GameOptionsData.MinPlayers = Enumerable.Repeat(1, 4).ToArray();

        Harmony.PatchAll();
    }

    [RegisterInIl2Cpp]
    public class DebuggerComponent : MonoBehaviour
    {
        [HideFromIl2Cpp]
        public bool DisableGameEnd { get; set; }

        [HideFromIl2Cpp]
        public DragWindow TestWindow { get; }

        public DebuggerComponent(IntPtr ptr) : base(ptr)
        {
            TestWindow = new DragWindow(new Rect(20, 20, 0, 0), "Debugger", () =>
            {
                GUILayout.Label("Name: " + DataManager.Player.Customization.Name);
                DisableGameEnd = GUILayout.Toggle(DisableGameEnd, "Disable game end");

                if (ShipStatus.Instance)
                {
                    if (GUILayout.Button("Force game end"))
                    {
                        ShipStatus.Instance.enabled = true;
                        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, true);
                    }

                    if (GUILayout.Button("Call a meeting"))
                    {
                        PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                    }
                }

                if (PlayerControl.LocalPlayer)
                {
                    var data = PlayerControl.LocalPlayer.Data;

                    var newIsImpostor = GUILayout.Toggle(data.Role.IsImpostor, "Is Impostor");
                    if (data.Role.IsImpostor != newIsImpostor)
                    {
                        PlayerControl.LocalPlayer.RpcSetRole(newIsImpostor ? RoleTypes.Impostor : RoleTypes.Crewmate);
                    }

                    if (GUILayout.Button("Spawn a dummy"))
                    {
                        var playerControl = Instantiate(AmongUsClient.Instance.PlayerPrefab);
                        var i = playerControl.PlayerId = (byte) GameData.Instance.GetAvailableId();
                        GameData.Instance.AddPlayer(playerControl);
                        AmongUsClient.Instance.Spawn(playerControl, -2, SpawnFlags.None);
                        playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
                        playerControl.GetComponent<DummyBehaviour>().enabled = false;
                        playerControl.NetTransform.enabled = false;
                        playerControl.SetName($"The Rock {i}");
                        var color = (byte) (i % Palette.PlayerColors.Length);
                        playerControl.SetColor(color);
                        playerControl.SetHat(HatManager.Instance.allHats[i % HatManager.Instance.allHats.Count].ProdId, playerControl.Data.DefaultOutfit.ColorId);
                        playerControl.SetPet(HatManager.Instance.allPets[i % HatManager.Instance.allPets.Count].ProdId);
                        playerControl.SetSkin(HatManager.Instance.allSkins[i % HatManager.Instance.allSkins.Count].ProdId, color);
                        GameData.Instance.RpcSetTasks(playerControl.PlayerId, new Il2CppStructArray<byte>(0));
                    }
                    
                                        if (GUILayout.Button("Remove BlackScreen"))
                    {
                        UnityEngine.Object.Destroy(GameObject.Find("FullScreen500"));
                    }
                }

                if (PlayerControl.LocalPlayer)
                {
                    var position = PlayerControl.LocalPlayer.transform.position;
                    GUILayout.Label($"x: {position.x}");
                    GUILayout.Label($"y: {position.y}");
                }
            })
            {
                Enabled = false,
            };
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TestWindow.Enabled = !TestWindow.Enabled;
            }
                        if (Input.GetKeyDown(KeyCode.F2))
            {
                UnityEngine.Object.Destroy(GameObject.Find("GameData(Clone)"));
            }
        }

        private void OnGUI()
        {
            TestWindow.OnGUI();
        }
    }
}
