﻿using PointBlank.Core;
using PointBlank.Core.Managers;
using PointBlank.Core.Managers.Events;
using PointBlank.Core.Models.Account.Players;
using PointBlank.Core.Network;
using PointBlank.Game.Data.Configs;
using PointBlank.Game.Data.Managers;
using PointBlank.Game.Data.Model;
using PointBlank.Game.Network.ServerPacket;
using System;

namespace PointBlank.Game.Network.ClientPacket
{
    public class PROTOCOL_BASE_USER_ENTER_REQ : ReceivePacket
    {
        private long pId;
        private uint erro;

        public PROTOCOL_BASE_USER_ENTER_REQ(GameClient client, byte[] data)
        {
            makeme(client, data);
        }

        public override void read()
        {
            readC();
            pId = readQ();
        }

        public override void run()
        {
            if (_client == null)
            {
                return;
            }
            try
            {
                if (_client._player != null)
                {
                    erro = 0x80000000;
                }
                else
                {
                    Account p = AccountManager.getAccountDB(pId, 2, 0);
                    if (p != null && p._status.serverId == 0)
                    {
                        _client.player_id = p.player_id;
                        p._connection = _client;
                        p.GetAccountInfos(29);
                        p.LoadInventory();
                        p.LoadMissionList();
                        p.LoadPlayerBonus();
                        EnableQuestMission(p);
                        p._inventory.LoadBasicItems();
                        p.SetPublicIP(_client.GetAddress());
                        p.Session = new PlayerSession { _sessionId = _client.SessionId, _playerId = _client.player_id };
                        p.updateCacheInfo();
                        p._status.updateServer((byte)GameConfig.serverId);
                        _client._player = p;
                        ComDiv.updateDB("players", "lastip", p.PublicIP.ToString(), "player_id", p.player_id);

                        if (p._topups.Count > 0)
                        {
                            for (int i = 0; i < p._topups.Count; i++)
                            {
                                PlayerItemTopup Item = p._topups[i];
                                if (Item.ItemId != 0)
                                {
                                    _client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, p, new ItemsModel(Item.ItemId, Item.ItemName, Item.Equip, Item.Count)));
                                    PlayerManager.DeletePlayerTopup(Item.ObjectId, p.player_id);
                                }
                            }
                        }
                    }
                    else
                    {
                        erro = 0x80000000;
                    }
                }
                _client.SendPacket(new PROTOCOL_BASE_USER_ENTER_ACK(erro));
                if (erro > 0)
                {
                    _client.Close(500);
                }
            }
            catch (Exception ex)
            {
                Logger.info("PROTOCOL_BASE_USER_ENTER_REQ: " + ex.ToString());
                _client.Close(0);
            }
        }

        private void EnableQuestMission(Account player)
        {
            PlayerEvent ev = player._event;
            if (ev == null)
            {
                return;
            }
            if (ev.LastQuestFinish == 0 && EventQuestSyncer.getRunningEvent() != null)
            {
                player._mission.mission4 = 13;
            }
        }
    }
}