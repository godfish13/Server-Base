using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static S_PlayerList;

public class PlayerMgr
{
    MyPlayer _myplayer;
    Dictionary<int, Player> _players = new Dictionary<int, Player>();

    public static PlayerMgr Instance { get; } = new PlayerMgr();

    public void Add(S_PlayerList packet)
    {
        Object obj = Resources.Load("Player");

        foreach (S_PlayerList.Player p in packet.players)
        {
            GameObject go = Object.Instantiate(obj) as GameObject;

            if (p.isSelf)
            {
                MyPlayer myPlayer = go.AddComponent<MyPlayer>();
                myPlayer.playerID = p.playerID;
                myPlayer.transform.position = new Vector3(p.posX, p.posY, p.posZ);
                _myplayer = myPlayer;
            }
            else
            {
                Player player = go.AddComponent<Player>();
                player.playerID = p.playerID;
                player.transform.position = new Vector3(p.posX, p.posY, p.posZ);
                _players.Add(p.playerID, player);
            }
        }
    }

    public void Move(S_BroadCastMove packet)
    {
        if (_myplayer.playerID == packet.playerID)
        {
            _myplayer.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        }
        else
        {
            Player player = null;
            if (_players.TryGetValue(packet.playerID, out player))
            {
                player.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
                //if (packet.playerID == 1)
                //    Debug.Log($"{packet.playerID} : move to ({packet.posX}, {packet.posY}, {packet.posZ})");
            }
        }
    }

    public void EnterGame(S_BroadCastEnterGame packet)
    {
        if (packet.playerID == _myplayer.playerID)
            return;
        Object obj = Resources.Load("Player");
        GameObject go = Object.Instantiate(obj) as GameObject;

        Player player = go.AddComponent<Player>();
        player.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        _players.Add(packet.playerID, player);
    }

    public void LeaveGame(S_BroadCastLeaveGame packet) 
    {
        if (_myplayer.playerID == packet.playerID)
        {
            GameObject.Destroy(_myplayer.gameObject);
            _myplayer = null;
        }
        else
        {
            Player player = null;
            if (_players.TryGetValue(packet.playerID, out player))
            {
                GameObject.Destroy(player.gameObject);
                _players.Remove(packet.playerID);
            }
        }
    }
}
