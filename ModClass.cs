using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using SFCore;
using SFCore.Generics;
using SFCore.Utils;
using UnityEngine;
using UObject = UnityEngine.Object;
using HutongGames.PlayMaker.Actions;
using GlobalEnums;
using System.Threading;

namespace Less_Healing
{

    public class Less_Healing : Mod, IMenuMod, IGlobalSettings<GlobalSettings>, ILocalSettings<LocalData>
    {
        internal static Less_Healing Instance;

        public Less_Healing() : base("LessHealing")
        {
            Instance = this;
        }
        public override string GetVersion() => "v1.0.2";


        
        //Create and initalize a local variable to be able to access the settings
        public static GlobalSettings GS { get; set; } = new GlobalSettings();

        // First method to implement. The parameter is the read settings from the file
        public void OnLoadGlobal(GlobalSettings s)
        {
            GS = s; // save the read data into local variable;
        }

        public GlobalSettings OnSaveGlobal()
        {
            return GS;//return the local variable so it can be written in the json file
        }

        public static LocalData saveData { get; set; } = new LocalData();
        public void OnLoadLocal(LocalData s) => saveData = s;
        public LocalData OnSaveLocal() => saveData;



        private void dLog(string message)
        {
            if (debugLog)
                Log(message);
        }

        private bool debugLog = false;

        private bool playerAtBench;
        private int currentHealth;

        private bool healthFlag = false;
        private float healthFlagStart = 0;

        private int maxHealthCounter;

        private bool benchHealing;
        private bool focusHealing;
        private bool retainHealth;
        private bool hotspringHealing;
        private bool lifeblood;

        private bool focusHealingSubscribed;
        private bool retainHealthSubscribed;
        private bool hotspringHealingSubscribed;


        public bool ToggleButtonInsideMenu => throw new NotImplementedException();

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            return new List<IMenuMod.MenuEntry>
            {
                new IMenuMod.MenuEntry
                {
                    Name = "Bench Healing:",
                    Description = null,
                    Values = new string[]
                    {
                        "Disabled",
                        "Enabled"
                    },
                    Saver = opt => this.benchHealing = opt switch
                    {
                        0 => false,
                        1 => true,

                        _ => throw new InvalidOperationException()
                    },
                    Loader = () => this.benchHealing switch
                    {
                        false => 0,
                        true => 1,
                    }

                },
                new IMenuMod.MenuEntry
                {
                    Name = "Focus Healing:",
                    Description = null,
                    Values = new string[]
                    {
                        "Disabled",
                        "Enabled"
                    },
                    Saver = opt => this.focusHealing = opt switch
                    {
                        0 => false,
                        1 => true,

                        _ => throw new InvalidOperationException()
                    },
                    Loader = () => this.focusHealing switch
                    {
                        false => 0,
                        true => 1,
                    }

                },
                new IMenuMod.MenuEntry
                {
                    Name = "Retain Health:",
                    Description = "After quit out",
                    Values = new string[]
                    {
                        "Disabled",
                        "Enabled"
                    },
                    Saver = opt => this.retainHealth = opt switch
                    {
                        0 => false,
                        1 => true,

                        _ => throw new InvalidOperationException()
                    },
                    Loader = () => this.retainHealth switch
                    {
                        false => 0,
                        true => 1,
                    }

                },
                new IMenuMod.MenuEntry
                {
                    Name = "Hotspring Heal:",
                    Description = null,
                    Values = new string[]
                    {
                        "Disabled",
                        "Enabled"
                    },
                    Saver = opt => this.hotspringHealing = opt switch
                    {
                        0 => false,
                        1 => true,

                        _ => throw new InvalidOperationException()
                    },
                    Loader = () => this.hotspringHealing switch
                    {
                        false => 0,
                        true => 1,
                    }

                },
                new IMenuMod.MenuEntry
                {
                    Name = "Lifeblood:",
                    Description = null,
                    Values = new string[]
                    {
                        "Disabled",
                        "Enabled"
                    },
                    Saver = opt => this.lifeblood = opt switch
                    {
                        0 => false,
                        1 => true,

                        _ => throw new InvalidOperationException()
                    },
                    Loader = () => this.lifeblood switch
                    {
                        false => 0,
                        true => 1,
                    }

                }
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;

            Log("Initialized");

            focusHealingSubscribed = false;
            retainHealthSubscribed = false;
            hotspringHealingSubscribed = false;


            benchHealing = GS.benchHealing;
            focusHealing = GS.focusHealing;
            retainHealth = GS.retainHealth;
            hotspringHealing = GS.hotspringHealing;
            lifeblood = GS.lifeblood;

            On.HeroController.Awake += ConfigureHealthOptions;

            On.HeroController.Awake += UpdateSettings;

            ModHooks.HeroUpdateHook += GlobalOwner;
        }

        private void UpdateSettings(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig(self);

            Log("Updating Global Settings");

            GS.benchHealing = this.benchHealing;
            GS.focusHealing = this.focusHealing;
            GS.retainHealth = this.retainHealth;
            GS.hotspringHealing = this.hotspringHealing;
            GS.lifeblood = this.lifeblood;

            self.playerData.health = saveData.lastSavedHealth;

            currentHealth = saveData.lastSavedHealth;
        }




        private void ConfigureHealthOptions(On.HeroController.orig_Awake orig, HeroController self)
        {




            if (focusHealing == false && focusHealingSubscribed == false)
                {
                    Log("Removing FOCUS HEALING");
                    On.HeroController.Start += DisableFocusHealing;
                    focusHealingSubscribed = true;
                }
            else if (focusHealing == true && focusHealingSubscribed == true)
                {
                    Log("Enabling FOCUS HEALING");
                    On.HeroController.Start -= DisableFocusHealing;
                    focusHealingSubscribed = false;
                }

            if (retainHealth == false && retainHealthSubscribed == true)
                {
                    Log("Disabling RETAIN HEALTH");
                    ModHooks.BeforeSavegameSaveHook -= SaveLocalHealth;
                    On.HeroController.Start += LoadLocalHealth;
                    retainHealthSubscribed = false;
                }
            else if (retainHealth == true && retainHealthSubscribed == false)
                {
                    Log("Enabling RETAIN HEALTH");
                    ModHooks.BeforeSavegameSaveHook += SaveLocalHealth;
                    On.HeroController.Start += LoadLocalHealth;
                    retainHealthSubscribed = true;
                }

            if (hotspringHealing == false && hotspringHealingSubscribed == false)
                {
                    Log("Removing HOTSPRING HEALING");
                    On.PlayMakerFSM.Awake += DisableHotspringHealing;
                    hotspringHealingSubscribed = true;
                }
            else if (hotspringHealing == true && hotspringHealingSubscribed == true)
                {
                    Log("Enabling HOTSPRING HEALING");
                    On.PlayMakerFSM.Awake -= DisableHotspringHealing;
                    hotspringHealingSubscribed = false;
                }

                

            orig(self);

        }



        private void GlobalOwner()
        {
            var hero = HeroController.instance;
            float currentTime = Time.time;

            DebugCommands(hero);


            if (!hero.playerData.atBench && !playerAtBench) // Current health updater
            {
                currentHealth = hero.playerData.health;
            }


            HealthCheck(hero, currentTime);
            
            playerAtBench = hero.playerData.atBench;

        }



        private void HealthCheck(HeroController hero, float currentTime)
        {
            if (!benchHealing)
            {

                if (!hero.playerData.atBench && playerAtBench && hero.playerData.health > currentHealth)
                {
                    hero.playerData.health = currentHealth;

                    healthFlag = true;
                    healthFlagStart = Time.time;
                }
            }

            if (!lifeblood)
            {
                if (!hero.playerData.atBench && !playerAtBench && hero.playerData.healthBlue > 0) // Check lifeblood
                {
                    hero.playerData.healthBlue = 0;

                    healthFlag = true;
                    healthFlagStart = Time.time;
                }

                if (!hero.playerData.atBench && !playerAtBench && hero.playerData.joniHealthBlue > 0) // Check Joni health
                {
                    hero.playerData.joniHealthBlue = 0;

                    healthFlag = true;
                    healthFlagStart = Time.time;
                }
            }


            if (!hero.playerData.atBench && !playerAtBench)
                UpdateHealthHud(hero, currentTime, healthFlagStart, 1.5f);

        }


        private void UpdateHealthHud(HeroController hero, float currentTime, float healthFlagStart, float delay)
        {
            if (!hero.controlReqlinquished && healthFlag && (currentTime - healthFlagStart) >= delay)
            {
                hero.TakeHealth(0);
                healthFlag = false;
            }
        }





        private void DebugCommands(HeroController hero)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                Log("Taking health");
                hero.TakeHealth(1);
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                Log("Adding health");
                hero.AddHealth(1);
            }
        }



        private void LoadLocalHealth(On.HeroController.orig_Start orig, HeroController self)
        {
            dLog("IN LoadLocalHealth");
            orig(self);

            self.playerData.health = currentHealth;

        }

        private void SaveLocalHealth(SaveGameData data)
        {
            dLog("IN SaveLocalHealth");
            saveData.lastSavedHealth = currentHealth;
        }


        private void DisableHotspringHealing(On.PlayMakerFSM.orig_Awake orig, PlayMakerFSM self)
        {
            dLog("IN DisableHotspringHealing");

            orig(self);

            if (self.name == "Spa Region")
            {
                //Log("Removing Healing FSM");
                self.RemoveAction("Heal", 3);
            }

            
        }


        private void DisableFocusHealing(On.HeroController.orig_Start orig, HeroController self)
        {
            dLog("IN DisableFocusHealing");

            orig(self);
            EditFocusFSM(self);
        }

        private void EditFocusFSM(HeroController self)
        {
            dLog("IN EditFocusFSM");

            var spellFsm = self.gameObject.LocateMyFSM("Spell Control");
            var spellFsmVar = spellFsm.FsmVariables;


            var zeroAdd = new IntAdd
            {
                intVariable = spellFsmVar.FindFsmInt("Health Increase"),
                add = 0
            };

            spellFsm.RemoveAction("Set HP Amount", 0);
            spellFsm.RemoveAction("Set HP Amount", 1);

            spellFsm.AddAction("Set HP Amount", zeroAdd);   
        }

        
    }
}