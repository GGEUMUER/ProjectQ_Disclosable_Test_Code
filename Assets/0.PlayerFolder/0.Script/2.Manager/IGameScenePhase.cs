using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameScenePhase
{
    void Enter(GameSceneManager gameSceneManager);
    void UpdateLoop();
    void OnPacketReceived(Packet packet);
}
