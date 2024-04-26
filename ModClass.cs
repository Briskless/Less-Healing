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
        public override string GetVersion() => "v1.0.1";


        
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
        private bool lifeblood;

        private bool configureHealthOptionsSubscribed;
        private bool benchHealingSubscribed;
        private bool focusHealingSubscribed;
        private bool retainHealthSubscribed;
        private bool hotspringHealingSubscribed;
        private bool lifebloodSubscribed;


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

            configureHealthOptionsSubscribed = false;
            benchHealingSubscribed = false;
            focusHealingSubscribed = false;
            retainHealthSubscribed = false;
            hotspringHealingSubscribed = false;
            lifebloodSubscribed = false;

            benchHealing = GS.benchHealing;
            focusHealing = GS.focusHealing;
            retainHealth = GS.retainHealth;
            hotspringHealing = GS.hotspringHealing;
            lifeblood = GS.lifeblood;

            On.HeroController.Awake += ConfigureHealthOptions;

            On.HeroController.Awake += UpdateSettings;

            //ModHooks.HeroUpdateHook += DebugFunction;
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

            currentHealth = self.playerData.health;
            isHeartEquipped = true;
        }

        private void DebugFunction()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                Log("Taking HEALTH");

                Log("Player health prev: " + HeroController.instance.playerData.health);
                HeroController.instance.TakeHealth(1);
                Log("Player health after: " + HeroController.instance.playerData.health);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                Log("Adding HEALTH");

                Log("Player health prev: " + HeroController.instance.playerData.health);
                HeroController.instance.AddHealth(1);
                Log("Player health after: " + HeroController.instance.playerData.health);
            }
        }


        private void ConfigureHealthOptions(On.HeroController.orig_Awake orig, HeroController self)
        {
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
                else if (hotspringHealing == true && hotspringHealingSubscribed == true)
                {
                    Log("Enabling HOTSPRING HEALING");
                    On.PlayMakerFSM.Awake -= DisableHotspringHealing;
                    hotspringHealingSubscribed = false;
                }

                if (lifeblood == false && lifebloodSubscribed == false)
                {
                    Log("Disabling LIFEBLOOD");
                    ModHooks.HeroUpdateHook += DisablingLifeblood;
                    lifebloodSubscribed = true;
                }
                else if (lifeblood == true && lifebloodSubscribed == true)
                {
                    Log("Enabling LIFEBLOOD");
                    ModHooks.HeroUpdateHook -= DisablingLifeblood;
                    lifebloodSubscribed = false;
                }

                
                configureHealthOptionsSubscribed = true;
            }

            orig(self);

        }

        private void DisablingLifeblood()
        {
            var playerData = HeroController.instance.playerData;
            playerData.healthBlue = 0;
            playerData.joniHealthBlue = 0;
        }

        private void LoadLocalHealth(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);

            self.playerData.health = currentHealth;

        }

        private void SaveLocalHealth(SaveGameData data)
        {
            saveData.lastSavedHealth = currentHealth;
        }

        private void RemoveFakeHealth()
        {
            float currentTime = Time.time;

            if (HeroController.instance.playerData.atBench == false && playerAtBench == true)
            {

                maxHealthCounter = 0;

                var data = HeroController.instance.playerData;


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

            if (HeroController.instance.playerData.maxHealth <= 5)
            {
                TakeFakeHealth(currentTime, 0.7);
            }
            else if (HeroController.instance.playerData.maxHealth <= 8)
            {
                TakeFakeHealth(currentTime, 0.9);
            }
            else if (HeroController.instance.playerData.maxHealth <= 11)
            {
                TakeFakeHealth(currentTime, 1.5);
            }


            playerAtBench = HeroController.instance.playerData.atBench;
        }


        private void TakeFakeHealth(float currentTime, double duration)
        {
            if (takeHealthFlag == true && currentTime - healthFlagStart >= duration)
            {
                HeroController.instance.TakeHealth(0);
                takeHealthFlag = false;
            }
        }



        private void DisableHotspringHealing(On.PlayMakerFSM.orig_Awake orig, PlayMakerFSM self)
        {
            orig(self);

            if (self.name == "Spa Region")
            {
                //Log("Removing Healing FSM");
                self.RemoveAction("Heal", 3);
            }

            
        }




        private void MaxHealthCounterInterference(PlayerData data, HeroController controller)
        {
            if (maxHealthCounter == 1 && data.equippedCharm_23 && isHeartEquipped == false)
            {
                currentHealth = prevHealth;
            }

            isHeartEquipped = data.equippedCharm_23;
        
        }


        private void DisableBenchHealing(On.PlayerData.orig_MaxHealth orig, PlayerData self)
        {
            maxHealthCounter++;
            //Log("Max Health Index: " + maxHealthCounter);


            if (maxHealthCounter == 1)
            {
                prevHealth = currentHealth;
                currentHealth = self.health;
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