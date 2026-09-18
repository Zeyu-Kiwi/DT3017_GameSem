using UnityEngine;

public class Test_EscapeEvent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void hasHammer()
    {
        Debug.Log("Player has hammer. Door breaks animation. Run any other functions.");
    }

    public void noHammer()
    {
        Debug.Log("Player don't have hammer. Plays a door shake animation. Run any other functions.");
    }
}
