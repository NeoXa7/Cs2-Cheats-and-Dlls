using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;
using MemEdit;
using System.Numerics;
using cs2ExternalCheat;

// init memEdit
memEdit memEdit = new memEdit("cs2");

Console.ForegroundColor = ConsoleColor.Green;
Console.Title = "CS2 EXTERNAL CHEAT";


Console.WriteLine(@"



   ___ ___ ___   _____  _______ ___ ___ _  _   _   _     
  / __/ __|_  ) | __\ \/ /_   _| __| _ \ \| | /_\ | |    
 | (__\__ \/ /  | _| >  <  | | | _||   / .` |/ _ \| |__  
  \___|___/___| |___/_/\_\ |_| |___|_|_\_|\_/_/ \_\____| 
                                                                  
 ______   __  _   _          __  __    _____ 
 | __ ) \ / / | \ | | ___  ___\ \/ /__ |___  |
 |  _ \\ V /  |  \| |/ _ \/ _ \\  // _` | / / 
 | |_) || |   | |\  |  __/ (_) /  \ (_| |/ /  
 |____/ |_|   |_| \_|\___|\___/_/\_\__,_/_/
                                                         

 CHEAT v1.1

");
Console.ResetColor();

string processName = "cs2";
int procID = memEdit.getProcIDFromName(processName);
if (procID > 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($" PROCESS ({processName} With Process ID {procID}) FOUND");
    Console.WriteLine(" CHEAT ATTACHED SUCCESSFULLY");
}
else
{
    Console.WriteLine($" PROCESS({processName}) NOT FOUND OPEN YOUR GAME FIRST AND THEN TRY AGAIN");
    Console.WriteLine(" CHEAT Failed To ATTACH ");
    Console.ForegroundColor = ConsoleColor.Green;

}

// main logics

// get client module

IntPtr client = memEdit.GetModuleBase("client.dll");

// init renderer

Renderer renderer = new Renderer();
Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
renderThread.Start();


// get screen size from renderer
Vector2 screenSize = renderer.screenSize;


// store entities
List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();


// offsets

// offsets.CS <-- last updated april 23 from  FrezzyHook

// all .CS

int dwEntityList = 0x19BDD58; // line 11 
int dwViewMatrix = 0x1A1FCB0; // line 24 
int dwLocalPlayerPawn = 0x1823A08; // line 18 

// client.dll.Cs <-- last updated april 23 from  FrezzyHook


int m_vOldOrigin = 0x1274; // line 1454 
int m_iTeamNum = 0x3C3;// line 1307   
int m_lifeState = 0x328; // line 1275  
int m_hPlayerPawn = 0x7DC; // line 277 
int m_vecViewOffset = 0xC50; // line 499  
int m_iszPlayerName = 0x630; // line 97   
int m_iHealth = 0x324; // line 1274  
int m_modelState = 0x170; // line 1114  
int m_pGameSceneNode = 0x308; // line 1270  


while (true)
{
    // reseting entity
    entities.Clear();

    // getting entity list
    IntPtr entityList = memEdit.ReadPointer(client, dwEntityList);

    // making entry
    IntPtr listEntry = memEdit.ReadPointer(entityList, 0x10);

    // getting localPlayer
    IntPtr localPlayerPawn = memEdit.ReadPointer(client, dwLocalPlayerPawn);

    // getting team 
    localPlayer.team = memEdit.ReadInt(localPlayerPawn, m_iTeamNum);

    // esp functions goes here -------------------------------------------------------------> Esp Start

    // looping through each entity list

    for (int i = 0; i < 64; i++)
    {
        // get current controller

        IntPtr currentController = memEdit.ReadPointer(listEntry, i * 0x78);

        if (currentController == IntPtr.Zero) continue; // checking 

        // get pawn handle

        int pawnHandle = memEdit.ReadInt(currentController, m_hPlayerPawn);
        if (pawnHandle == 0) continue;

        // get currentPawn , making second entry

        IntPtr listEntry2 = memEdit.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
        if (listEntry2 == IntPtr.Zero) continue;

        // get current pawn

        IntPtr currentPawn = memEdit.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));
        if (currentPawn == IntPtr.Zero) continue;

        // check if life state

        int lifeState = memEdit.ReadInt(currentPawn, m_lifeState);
        if (lifeState != 256) continue;

        // get matrix or escape the matrix before andrewtate Chase you

        float[] viewMatrix = memEdit.ReadMatrix(client + dwViewMatrix);

        IntPtr sceneNode = memEdit.ReadPointer(currentPawn, m_pGameSceneNode);
        IntPtr boneMatrix = memEdit.ReadPointer(sceneNode, m_modelState + 0x80);

        // populate entity

        Entity entity = new Entity();

        entity.name = memEdit.ReadString(currentController, m_iszPlayerName, 16).Split("\0")[0];
        entity.health = memEdit.ReadInt(currentPawn, m_iHealth);
        entity.team = memEdit.ReadInt(currentPawn, m_iTeamNum);
        entity.position = memEdit.ReadVec(currentPawn, m_vOldOrigin);
        entity.viewOffsets = memEdit.ReadVec(currentPawn, m_vecViewOffset);
        entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
        entity.viewPosition2D = Calculate.WorldToScreen(viewMatrix, Vector3.Add(entity.position, entity.viewOffsets), screenSize);
        entity.distance = Vector3.Distance(entity.position, localPlayer.position);
        entity.bones = Calculate.ReadBones(boneMatrix, memEdit);
        entity.bones2d = Calculate.ReadBones2d(entity.bones, viewMatrix, screenSize);

        entities.Add(entity);
    }
    // update render data

    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntities(entities);


    Thread.Sleep(1); // optional thread sleep if wanted

    // esp functions ends here -------------------------------------------------------------> Esp End
}

//[DllImport("user32.dll")]
//static extern short GetAsyncKeyState(int vKey);