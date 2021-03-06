﻿using Events;
using System;
using PlayGroup;
using Sprites;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;

namespace Equipment
{
    public class Equipment : NetworkBehaviour
    {
        public SyncListInt syncEquipSprites = new SyncListInt();
        public ClothingItem[] clothingSlots;
        private PlayerNetworkActions playerNetworkActions;
        private PlayerScript playerScript;

        public NetworkIdentity networkIdentity { get; set; }

        private bool isInit = false;

        void Start()
        {
            networkIdentity = GetComponent<NetworkIdentity>();
            playerNetworkActions = gameObject.GetComponent<PlayerNetworkActions>();
            playerScript = gameObject.GetComponent<PlayerScript>();
        }

        public override void OnStartServer()
        {
            InitEquipment();

            EquipmentPool equipPool = FindObjectOfType<EquipmentPool>();
            if (equipPool == null)
            {
                Instantiate(Resources.Load("EquipmentPool") as GameObject, Vector2.zero, Quaternion.identity);
            }

            base.OnStartServer();
        }

        public override void OnStartClient()
        {
            InitEquipment();
            base.OnStartClient();
        }

        void InitEquipment()
        {
            if (isInit)
                return;

            syncEquipSprites.Callback = SyncSprites;
            for (int i = 0; i < clothingSlots.Length; i++)
            {
                //All the other slots:
                clothingSlots[i].Reference = -1;
                if (isServer)
                {
                    syncEquipSprites.Add(-1);
                }
                else
                {
                    clothingSlots[i].Reference = syncEquipSprites[i];
                }
            }
            isInit = true;
            //Player sprite offset:
            clothingSlots[10].Reference = 33;

        }

        public void SyncSprites(SyncListInt.Operation op, int index)
        {
            clothingSlots[index].Reference = syncEquipSprites[index];
        }

        public IEnumerator SetPlayerLoadOuts()
        {
            //Waiting for player name resolve
            yield return new WaitForSeconds(0.2f);

            // Null Job players dont get a loadout
            if (playerScript.JobType == JobType.NULL)
                yield break;

            PlayerScript pS = GetComponent<PlayerScript>();
            pS.JobType = playerScript.JobType;

            JobOutfit standardOutfit = GameManager.Instance.StandardOutfit.GetComponent<JobOutfit>();
            JobOutfit jobOutfit = GameManager.Instance.GetOccupationOutfit(playerScript.JobType);

            Dictionary<string, string> gear = new Dictionary<string, string>();

            gear.Add("uniform", standardOutfit.uniform);
            //gear.Add("id", standardOutfit.id);
            gear.Add("ears", standardOutfit.ears);
            gear.Add("belt", standardOutfit.belt);
            gear.Add("back", standardOutfit.back);
            gear.Add("shoes", standardOutfit.shoes);
            gear.Add("glasses", standardOutfit.glasses);
            gear.Add("gloves", standardOutfit.gloves);
            gear.Add("suit", standardOutfit.suit);
            gear.Add("head", standardOutfit.head);
            //gear.Add("accessory", standardOutfit.accessory);
            gear.Add("mask", standardOutfit.mask);
            //gear.Add("backpack", standardOutfit.backpack);
            //gear.Add("satchel", standardOutfit.satchel);
            //gear.Add("duffelbag", standardOutfit.duffelbag);
            //gear.Add("box", standardOutfit.box);
            //gear.Add("l_hand", standardOutfit.l_hand);
            //gear.Add("l_pocket", standardOutfit.l_pocket);
            //gear.Add("r_pocket", standardOutfit.r_pocket);
            //gear.Add("suit_store", standardOutfit.suit_store);

            if (!String.IsNullOrEmpty(jobOutfit.uniform))
                gear["uniform"] = jobOutfit.uniform;
            /*if (!String.IsNullOrEmpty(jobOutfit.id))
                gear["id"] = jobOutfit.id;*/
            if (!String.IsNullOrEmpty(jobOutfit.ears))
                gear["ears"] = jobOutfit.ears;
            if (!String.IsNullOrEmpty(jobOutfit.belt))
                gear["belt"] = jobOutfit.belt;
            if (!String.IsNullOrEmpty(jobOutfit.back))
                gear["back"] = jobOutfit.back;
            if (!String.IsNullOrEmpty(jobOutfit.shoes))
                gear["shoes"] = jobOutfit.shoes;
            if (!String.IsNullOrEmpty(jobOutfit.glasses))
                gear["glasses"] = jobOutfit.glasses;
            if (!String.IsNullOrEmpty(jobOutfit.gloves))
                gear["gloves"] = jobOutfit.gloves;
            if (!String.IsNullOrEmpty(jobOutfit.suit))
                gear["suit"] = jobOutfit.suit;
            if (!String.IsNullOrEmpty(jobOutfit.head))
                gear["head"] = jobOutfit.head;
            /*if (!String.IsNullOrEmpty(jobOutfit.accessory))
                gear["accessory"] = jobOutfit.accessory;*/
            if (!String.IsNullOrEmpty(jobOutfit.mask))
                gear["mask"] = jobOutfit.mask;
            /*if (!String.IsNullOrEmpty(jobOutfit.backpack))
                gear["backpack"] = jobOutfit.backpack;
            if (!String.IsNullOrEmpty(jobOutfit.satchel))
                gear["satchel"] = jobOutfit.satchel;
            if (!String.IsNullOrEmpty(jobOutfit.duffelbag))
                gear["duffelbag"] = jobOutfit.duffelbag;
            if (!String.IsNullOrEmpty(jobOutfit.box))
                gear["box"] = jobOutfit.box;
            if (!String.IsNullOrEmpty(jobOutfit.l_hand))
                gear["l_hand"] = jobOutfit.l_hand;
            if (!String.IsNullOrEmpty(jobOutfit.l_pocket))
                gear["l_pocket"] = jobOutfit.l_pocket;
            if (!String.IsNullOrEmpty(jobOutfit.r_pocket))
                gear["r_pocket"] = jobOutfit.r_pocket;
            if (!String.IsNullOrEmpty(jobOutfit.suit_store))
                gear["suit_store"] = jobOutfit.suit_store;*/

            foreach(KeyValuePair<string,string> gearItem in gear)
            {
                // TODO: Add check for non cloth and call ItemFactory
                if (gearItem.Value.Contains("cloth"))
                {
                    GameObject obj = ClothFactory.CreateCloth(gearItem.Value, Vector3.zero);
                    ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
                    SetItem(GetLoadOutEventName(gearItem.Key), itemAtts);
                }
            }

            
        }

        //Hand item sprites after picking up an item (server)
        public void SetHandItem(string eventName, GameObject obj)
        {
            ItemAttributes att = obj.GetComponent<ItemAttributes>();
            EquipmentPool.AddGameObject(gameObject, obj);
            SetHandItemSprite(eventName, att);
            RpcSendMessage(eventName, obj);
        }

        [ClientRpc]
        void RpcSendMessage(string eventName, GameObject obj)
        {
            obj.BroadcastMessage("OnAddToInventory", eventName, SendMessageOptions.DontRequireReceiver);
        }

        public string GetLoadOutEventName(string uniformPosition)
        {
            switch (uniformPosition)
            {
                case "glasses":
                    return "eyes";
                case "head":
                    return "head";
                case "neck":
                    return "neck";
                case "mask":
                    return "mask";
                case "ears":
                    return "ear";
                case "suit":
                    return "suit";
                case "uniform":
                    return "uniform";
                case "gloves":
                    return "hands";
                case "shoes":
                    return "feet";
                case "belt":
                    return "belt";
                case "back":
                    return "back";
                case "id":
                    return "id";
                case "l_pocket":
                    return "storage02";
                case "r_pocket":
                    return "storage01";
                case "l_hand":
                    return "leftHand";
                case "r_hand":
                    return "rightHand";
                default:
                    Debug.LogWarning("GetLoadOutEventName: Unknown uniformPosition:" + uniformPosition);
                    return null;
            }
        }

        //To set the actual sprite on the player obj
        public void SetHandItemSprite(string eventName, ItemAttributes att)
        {
            Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
            if (eventName == "leftHand")
            {
                syncEquipSprites[(int)enumA] = att.NetworkInHandRefLeft();
            }
            else
            {
                syncEquipSprites[(int)enumA] = att.NetworkInHandRefRight();
            }
        }

        //Clear any sprite slot with -1 via the eventName (server)
        public void ClearItemSprite(string eventName)
        {
            Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
            syncEquipSprites[(int)enumA] = -1;
        }

        // Does not try to instantiate (already instantiated by Unicloth factory)
        private void SetItemUniclothToSlot(string eventName, GameObject uniCloth)
        {
            ItemAttributes att = uniCloth.GetComponent<ItemAttributes>();
            EquipmentPool.AddGameObject(gameObject, uniCloth);

            playerNetworkActions.TrySetItem(eventName, uniCloth);
            //Sync all clothing items across network using SyncListInt syncEquipSprites
            if (att.spriteType == SpriteType.Clothing)
            {
                Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
                syncEquipSprites[(int)enumA] = att.clothingReference;
            }
        }

        private void SetItem(string eventName, ItemAttributes itemAtts)
        {
            if (String.IsNullOrEmpty(eventName) || itemAtts == null)
                return;

            EquipmentPool.AddGameObject(gameObject, itemAtts.gameObject);

            playerNetworkActions.TrySetItem(eventName, itemAtts.gameObject);
            //Sync all clothing items across network using SyncListInt syncEquipSprites
            if (itemAtts.spriteType == SpriteType.Clothing)
            {
                Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
                syncEquipSprites[(int)enumA] = itemAtts.clothingReference;
            }
        }
    }
}