using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool DebugMode = false;

    public bool isDragging = false;
    public bool menuShown = false;
    public bool isStarted = false;
    public bool CraftMode = false;

    private CameraController camControl;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        camControl = Camera.main.GetComponent<CameraController>();
        if (!DebugMode)
        {
            camControl.enabled = false;
        }
        else
        {
            Camera.main.transform.position = new Vector3(0, 0, -10);
        }
    }

    //Craft recipe:
    //Bumper = "RedBall" + "RedBall"
    //SpawnRedBall = "RedBall" + "RedBall" + "RedBall" + "RedBall" + "RedBall"
    //Craft is relate if there are the same amount of ball selected, and the same type of ball selected. (But the order change anything !)

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //CraftMode
            CraftMode = !CraftMode;

            //Change cursor of game to the one of the craft 

            //Nothing can be clickable exept ball
            //Ball can be clickable and draggable
            //When ball is clicked.
            // - Stock type of ball is it
            // - The ball will have white circle around it
            // - Instantiate CraftModeCollider
            //    - this Object will have animation of growing
            //    - At fully grow, if something with the layer "Objects" is inside it (Check with raycast or overlap circle) Cancel craft mode (Cancel animation : reduce size to 0 and set color to red)
            //    - Else, if nothing is inside it, Wait second ball click.
            //    - If Space pressed, cancel craft mode (Hide animation : Reduce Size to 0)
            //    - If second ball clicked, Stock type of ball is it too, Set white circle around it
            //    - Link the two balls with a line
            //    - if this two ball coorespond to a recipe, Instanciate the ShapeObject relate to the recipe at the center of all the ball (If its Bumper, instantiate var GameObject named ShapeBumper)
            //    - if Space Pressed : add force to all ball selected in direction of the center of all balls, start merge animation for balls. Remove ShapeObject
            //    - After merge animation, Instantiate the new object (Bumper or SpawnRedBall) at the center of all balls selected
        }



        if (DebugMode)
            return;
        if (menuShown && camControl.enabled)
        {
            camControl.enabled = false;
        }
        else if (!menuShown && !camControl.enabled)
        {
            camControl.enabled = true;
        }
    }
}