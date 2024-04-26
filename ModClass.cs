﻿using Modding;
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
        public override string GetVersion() => "v0.9.0";


        
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







        private bool debugLog = true;

        private bool playerAtBench;
        private int prevHealth;
        private int currentHealth;

        private bool takeHealthFlag;
        private float healthFlagStart;

        private int maxHealthCounter;

        private bool benchHealing;
        private bool focusHealing;
        private bool retainHealth;
        private bool hotspringHealing;

        private bool configureHealthOptionsSubscribed;
        private bool benchHealingSubscribed;
        private bool focusHealingSubscribed;
        private bool retainHealthSubscribed;
        private bool hotspringHealingSubscribed;


        private bool isHeartEquipped;


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

                }
            };
        }

        
        private void dLog(string message)
        {
            if (debugLog)
            {
                Log(message);
            }
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;

            Log("Initialized");

            configureHealthOptionsSubscribed = false;
            benchHealingSubscribed = false;
            focusHealingSubscribed = false;
            retainHealthSubscribed = false;
            hotspringHealingSubscribed = false;

            benchHealing = GS.benchHealing;
            focusHealing = GS.focusHealing;
            retainHealth = GS.retainHealth;
            hotspringHealing = GS.hotspringHealing;

            //dLog("benchHealing: " + GS.benchHealing);
            //dLog("focusHealing: " + GS.focusHealing);
            //dLog("retainHealth: " + GS.retainHealth);
            //dLog("hotspringHealing: " + GS.hotspringHealing);

            On.HeroController.Awake += ConfigureHealthOptions;

            On.HeroController.Awake += UpdateSettings;

            ModHooks.HeroUpdateHook += DebugFunction;
        }


        private void UpdateSettings(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig(self);

            Log("Updating Global Settings");

            GS.benchHealing = this.benchHealing;
            GS.focusHealing = this.focusHealing;
            GS.retainHealth = this.retainHealth;
            GS.hotspringHealing = this.hotspringHealing;

            self.playerData.health = saveData.lastSavedHealth;
            Log("Current HEALTH overwritten to: " + self.playerData.health);

            currentHealth = self.playerData.health;
            isHeartEquipped = true;

            //Log("benchHealing: "+GS.benchHealing);
            //Log("focusHealing: "+GS.focusHealing);
            //Log("retainHealth: "+GS.retainHealth);
            //Log("hotspringHealing: " + GS.hotspringHealing);
        }

        private void DebugFunction()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                //dLog("Taking HEALTH");

                //dLog("Player health prev: " + HeroController.instance.playerData.health);
                HeroController.instance.TakeHealth(1);
                //dLog("Player health after: " + HeroController.instance.playerData.health);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                //dLog("Adding HEALTH");

                //dLog("Player health prev: " + HeroController.instance.playerData.health);
                HeroController.instance.AddHealth(1);
                //dLog("Player health after: " + HeroController.instance.playerData.health);
            }
        }


        private void ConfigureHealthOptions(On.HeroController.orig_Awake orig, HeroController self)
        {
            //Log("Configure Health Options : " + configureHealthOptionsSubscribed);
            if (configureHealthOptionsSubscribed == false)
            {
                if (benchHealing == false && benchHealingSubscribed == false)
                {
                    Log("Removing BENCH HEALING");
                    On.PlayerData.MaxHealth += DisableBenchHealing;
                    ModHooks.HeroUpdateHook += RemoveFakeHealth;
                    ModHooks.CharmUpdateHook += MaxHealthCounterInterference;
                    benchHealingSubscribed = true;
                }
                else if (benchHealing == true && benchHealingSubscribed == true)
                {
                    Log("Enabling BENCH HEALING");
                    On.PlayerData.MaxHealth -= DisableBenchHealing;
                    ModHooks.HeroUpdateHook -= RemoveFakeHealth;
                    ModHooks.CharmUpdateHook -= MaxHealthCounterInterference;
                    benchHealingSubscribed = false;
                }

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
                else
                {
                    Log("Enabling HOTSPRING HEALING");
                    On.PlayMakerFSM.Awake -= DisableHotspringHealing;
                    hotspringHealingSubscribed = false;

                }
                
                configureHealthOptionsSubscribed = true;
            }

            orig(self);

        }

        private void LoadLocalHealth(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);

            //Log("Local HEALTH loaded to: " + currentHealth);
            self.playerData.health = currentHealth;

        }

        private void SaveLocalHealth(SaveGameData data)
        {
            saveData.lastSavedHealth = currentHealth;
            //dLog("Local HEALTH saved as: " + saveData.lastSavedHealth);
        }

        private void RemoveFakeHealth()
        {
            float currentTime = Time.time;

            if (HeroController.instance.playerData.atBench == false && playerAtBench == true)
            {

                //dLog("Reset Counter");
                maxHealthCounter = 0;

                var data = HeroController.instance.playerData;

                //dLog("Called");

                if (currentHealth <= data.maxHealth)
                {
                    data.health = currentHealth;
                }
                else
                {
                    data.health = data.maxHealth;
                }

                takeHealthFlag = true;
                healthFlagStart = currentTime;
                
            }

            //Log("Time: " + (currentTime- healthFlagStart));
            if (takeHealthFlag == true && currentTime - healthFlagStart >= 0.8)
            {
                HeroController.instance.TakeHealth(0);
                takeHealthFlag = false;
            }

            playerAtBench = HeroController.instance.playerData.atBench;
        }



        private void DisableHotspringHealing(On.PlayMakerFSM.orig_Awake orig, PlayMakerFSM self)
        {
            //Log("Scene: " + self.gameObject.scene.name);
            orig(self);

            if (self.name == "Spa Region")
            {
                //Log("Removing Healing FSM");
                self.RemoveAction("Heal", 3);
            }

            
        }




        private void MaxHealthCounterInterference(PlayerData data, HeroController controller)
        {
            dLog("Charm Update Called");

            //dLog("Previous equippped: " + isHeartEquipped);
            //dLog("Currently equipped: " + data.equippedCharm_23);
            if (maxHealthCounter == 1 && data.equippedCharm_23 && isHeartEquipped == false)
            {
                dLog("Reverting health to: " + prevHealth);
                currentHealth = prevHealth;
            }

            isHeartEquipped = data.equippedCharm_23;
        
        }


        private void DisableBenchHealing(On.PlayerData.orig_MaxHealth orig, PlayerData self)
        {
            maxHealthCounter++;
            dLog("Max Health Index: " + maxHealthCounter);


            if (maxHealthCounter == 1)
            {
                prevHealth = currentHealth;
                currentHealth = self.health;
                dLog("Set current health to: " + currentHealth);
            }

            

            orig(self);

        }




        private void DisableFocusHealing(On.HeroController.orig_Start orig, HeroController self)
        {
            

            orig(self);
            EditFocusFSM(self);
        }

        private void EditFocusFSM(HeroController self)
        {
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