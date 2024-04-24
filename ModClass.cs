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

namespace Less_Healing
{

    public class Less_Healing : Mod, IMenuMod, IGlobalSettings<GlobalSettings>
    {
        internal static Less_Healing Instance;

        //Credits (SFGrenade | Exempt-Medic)

        public Less_Healing() : base("Less Healing")
        {
            Instance = this;
        }
        public override string GetVersion() => "v0.6";


        
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





        private bool benchHealing;
        private bool focusHealing;
        private bool retainHealth;
        private bool hotspringHealing;

        private bool benchHealingSubscribed;
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

                }
            };
        }


        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;

            Log("Initialized");

            benchHealingSubscribed = false;
            focusHealingSubscribed = false;
            retainHealthSubscribed = false;
            hotspringHealingSubscribed = false;

            benchHealing = GS.benchHealing;
            focusHealing = GS.focusHealing;
            retainHealth = GS.retainHealth;
            hotspringHealing = GS.hotspringHealing;

            Log("benchHealing: " + GS.benchHealing);
            Log("focusHealing: " + GS.focusHealing);
            Log("retainHealth: " + GS.retainHealth);
            Log("hotspringHealing: " + GS.hotspringHealing);

            On.HeroController.Awake += ConfigureHealthOptions;

            On.HeroController.Awake += UpdateGlobalSettings;
        }

        private void ConfigureHealthOptions(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig(self);

            if (benchHealing == false && benchHealingSubscribed == false)
            {
                Log("Removing BENCH HEALING");
                On.PlayerData.MaxHealth += DisableBenchHealing;
                benchHealingSubscribed = true;
            }
            else if (benchHealing == true && benchHealingSubscribed == true)
            {
                Log("Enabling BENCH HEALING");
                On.PlayerData.MaxHealth -= DisableBenchHealing;
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
                On.HeroController.Awake -= LoadPlayerHealth;
                PlayerData.instance.atBench = false; //Forces game to update player health
                retainHealthSubscribed = false;
            }
            else if (retainHealth == true && retainHealthSubscribed == false)
            {
                Log("Enabling RETAIN HEALTH");
                On.HeroController.Awake += LoadPlayerHealth;
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
        }

        private void UpdateGlobalSettings(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig(self);

            Log("Updating Global Settings");

            GS.benchHealing = this.benchHealing;
            GS.focusHealing = this.focusHealing;
            GS.retainHealth = this.retainHealth;
            GS.hotspringHealing = this.hotspringHealing;

            //Log("benchHealing: "+GS.benchHealing);
            //Log("focusHealing: "+GS.focusHealing);
            //Log("retainHealth: "+GS.retainHealth);
            //Log("hotspringHealing: " + GS.hotspringHealing);
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


        private void SavePlayerHealth(SaveGameData data)
        {

            PlayerData.instance.atBench = true; //Necessary so that health loading is not overwritten
        }

        private void LoadPlayerHealth(On.HeroController.orig_Awake orig, HeroController self)
        {
            PlayerData.instance.atBench = true;

            orig(self);

            //Log("Health: "+PlayerData.instance.health);


        }


        private void DisableBenchHealing(On.PlayerData.orig_MaxHealth orig, PlayerData self)
        {
            
            int currentHealth = self.health;
            orig(self);

            if (self.atBench)
            {
                self.health = currentHealth;
                GameManager.instance.StartCoroutine(HealthUpdate());
                //Log("At Bench");
            }
        }

        private IEnumerator HealthUpdate()
        {
            yield return new WaitForSeconds(0.01f);
            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");
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