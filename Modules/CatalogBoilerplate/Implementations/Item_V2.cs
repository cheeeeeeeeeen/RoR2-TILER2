﻿using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace TILER2 {
    public abstract class Item_V2<T>:Item_V2 where T : Item_V2<T> {
        public static T instance {get;private set;}

        public Item_V2() {
            if(instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }
    
    public abstract class Item_V2 : CatalogBoilerplate {
        public override string configCategoryPrefix => "Items.";

        public ItemIndex catalogIndex {get; private set;}
        public ItemDef itemDef {get; private set;}
        public CustomItem customItem {get; private set;}

        public abstract ItemTier itemTier {get;}

        [AutoConfig("If true, the item will not be given to enemies by Evolution nor in the arena map, and it will not be found by Scavengers.")]
        public virtual bool itemIsAIBlacklisted {get;protected set;} = false;

        public virtual ReadOnlyCollection<ItemTag> itemTags {get; private set;}
        protected ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);

        public override void SetupConfig() {
            base.SetupConfig();
            ConfigEntryChanged += (sender, args) => {
                if(args.target.boundProperty.Name == nameof(enabled)) {
                    var runIsActive = Run.instance != null && Run.instance.enabled;
                    if(runIsActive)
                        Run.instance.BuildDropTable();
                    if(args.oldValue != args.newValue) {
                        if((bool)args.newValue == true) {
                            if(Run.instance != null && Run.instance.enabled) Chat.AddMessage($"<color=#{ColorCatalog.GetColorHexString(itemDef.colorIndex)}>{displayName}</color> has been <color=#aaffaa>ENABLED</color>. It will now drop, and existing copies will start working again.");
                        } else {
                            if(Run.instance != null && Run.instance.enabled) Chat.AddMessage($"<color=#{ColorCatalog.GetColorHexString(itemDef.colorIndex)}>{displayName}</color> has been <color=#ffaaaa>DISABLED</color>. It will no longer drop, and existing copies will stop working.");
                        }
                    }
                } else if(args.target.boundProperty.Name == nameof(itemIsAIBlacklisted)) {
                    var hasAIB = itemDef.tags.Contains(ItemTag.AIBlacklist);
                    if(hasAIB && !itemIsAIBlacklisted) {
                        itemDef.tags = itemDef.tags.Where(tag => tag != ItemTag.AIBlacklist).ToArray();
                    } else if(!hasAIB && itemIsAIBlacklisted) {
                        var nl = itemDef.tags.ToList();
                        nl.Add(ItemTag.AIBlacklist);
                        itemDef.tags = nl.ToArray();
                    }
                }
            };
        }

        public override void SetupAttributes() {
            base.SetupAttributes();
            
            var _itemTags = new List<ItemTag>(itemTags);
            if(itemIsAIBlacklisted) _itemTags.Add(ItemTag.AIBlacklist);
            var iarr = _itemTags.ToArray();
            itemDef = new ItemDef {
                name = modInfo.shortIdentifier+name,
                tier = itemTier,
                pickupModelPath = modelResourcePath,
                pickupIconPath = iconResourcePath,
                nameToken = this.nameToken,
                pickupToken = this.pickupToken,
                descriptionToken = this.descToken,
                loreToken = this.loreToken,
                tags = iarr
            };

            itemTags = Array.AsReadOnly(iarr);
            customItem = new CustomItem(itemDef, displayRules);
            catalogIndex = ItemAPI.Add(customItem);
        }

        public int GetCount(Inventory inv) {
            return (inv == null) ? 0 : inv.GetItemCount(catalogIndex);
        }
        public int GetCount(CharacterMaster chrm) {
            if(!chrm || !chrm.inventory) return 0;
            return chrm.inventory.GetItemCount(catalogIndex);
        }
        public int GetCount(CharacterBody body) {
            if(!body || !body.inventory) return 0;
            return body.inventory.GetItemCount(catalogIndex);
        }
        public int GetCountOnDeployables(CharacterMaster master) {
            if(master == null) return 0;
            var dplist = master.deployablesList;
            if(dplist == null) return 0;
            int count = 0;
            foreach(DeployableInfo d in dplist) {
                count += GetCount(d.deployable.gameObject.GetComponent<Inventory>());
            }
            return count;
        }

        public override ConsoleStrings GetConsoleStrings() {
            return new ConsoleStrings {
                className = "Item",
                objectName = this.name,
                formattedIndex = ((int)this.catalogIndex).ToString()
            };
        }
    }
}
