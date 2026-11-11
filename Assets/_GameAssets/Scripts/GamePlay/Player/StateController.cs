using UnityEngine;

public class StateController : MonoBehaviour
{
    private PlayerState currentplayerstate = PlayerState.Idle;
    public void ChangeState(PlayerState newPlayerState1)
    {
        if(currentplayerstate == newPlayerState1) {return;}
        currentplayerstate = newPlayerState1;
    }
    public PlayerState GetCurrentState()
    {
        return currentplayerstate;
    }
    void Start()
    {
        ChangeState(PlayerState.Idle);
    }

    
    void Update()
    {
        
    }
}
