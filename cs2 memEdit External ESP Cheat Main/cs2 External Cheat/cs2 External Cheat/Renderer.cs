using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ClickableTransparentOverlay;
using cs2ExternalCheat;
using MemEdit;
using ImGuiNET;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;



namespace cs2ExternalCheat  
{
    public class Renderer : Overlay
    {
        // importing user32.dll and GetAsyncKeyState for HOTKEY
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        // getting screensize
        public Vector2 screenSize = new Vector2(1366, 768);

        // entities copy , using more thread safe methods.

        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entitylock = new object();

        // Gui Elements

        // Private Bools        
        private bool enableEsp = true;
        private bool enableName = false;
        private bool enableLine = true;
        private bool enableBox = true;
        private bool enableHealthBar = true;
        private bool enableAntiFlashBang = false;
        private bool enableChams = false;
        private bool enableJumpShot = false;
        private bool enableBones = false;
        private bool enableRadar = false;
        //private bool showMenu = true;

        // Public Bools and ints
        public int fov = 60;

        // vector 4 colors
        private Vector4 enemyColor = new Vector4(1, 1, 1, 1); // default white
        private Vector4 teamColor = new Vector4(0, 0, 0, 1); //  default black
        private Vector4 nameColor = new Vector4(1, 1, 1, 1); //  default white
        private Vector4 boneColor = new Vector4(1, 1, 1, 1);

        float boneThickness = 4;

        // init MemEdit
        memEdit memEdit = new memEdit("cs2");

        //draw list
        ImDrawListPtr drawList;
        protected override void Render()
        {
            // ImGui  menu 
            ImGui.Begin("Counter Strike 2 External Cheat By NeoXa7 ");
            ImGui.Checkbox("Enable Esp", ref enableEsp);
            ImGui.Checkbox("Enable Lines", ref enableLine);
            ImGui.Checkbox("Enable Box", ref enableBox);
            ImGui.Checkbox("Entity Name", ref enableName);
            ImGui.Checkbox("Radar Hack", ref enableRadar);
            ImGui.Checkbox("Health Indicator", ref enableHealthBar);
            //ImGui.SliderInt("Fov Slider", ref fov, 58, 140);
            ImGui.Checkbox("Player Glow", ref enableChams);
            ImGui.Checkbox("Enable Skeletons", ref enableBones);
            ImGui.Checkbox("JumpShot (HOLD ALT)", ref enableJumpShot);
            ImGui.Checkbox("Anti FlashBang", ref enableAntiFlashBang);
            ImGui.SetWindowSize(new System.Numerics.Vector2(500, 300));


            // team esp color picker
            if (ImGui.CollapsingHeader("Team Color"))
                ImGui.ColorPicker4("##teamcolor", ref teamColor);

            // enemy esp color picker
            if (ImGui.CollapsingHeader("Enemy Color"))
                ImGui.ColorPicker4("##enemycolor", ref enemyColor);

            // name color picker
            if (ImGui.CollapsingHeader("Name Color"))
                ImGui.ColorPicker4("##namecolor", ref nameColor);

            // name color picker
            if (ImGui.CollapsingHeader("Bone Color"))
                ImGui.ColorPicker4("##bonecolor", ref boneColor);

            // draw overlay

            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();

            // getting client module
            IntPtr client = memEdit.GetModuleBase("client.dll");

            // offsets

            // offsets.Rs <-- last updated april 23 from FrezzyHook
            int dwLocalPlayerPawn = 0x1823A08; // line 18 
            int dwForceAttack = 0x1730020; // line 8 
            int dwEntityList = 0x19BDD58; // line 11 


            // client.dll.Rs <-- last updated april 23 from FrezzyHook

            int m_flFlashBangTime = 0x1348; // line 1959 
            int m_fFlags = 0x3CC; // line 612
            int m_vecAbsVelocity = 0x3D0; // line 1311
            int m_flDetectedByEnemySensorTime = 0x1440; // line 1931
            int m_iszPlayerName = 0x630; // line 97
            int m_hPlayerPawn = 0x7DC; // line 277
            int m_entitySpottedState = 0x1AA8;
            int m_bSpotted = 0x8;

            // getting localPlayer
            IntPtr localPlayerPawn = memEdit.ReadPointer(client, dwLocalPlayerPawn);

            // getting entity list
            IntPtr entityList = memEdit.ReadPointer(client, dwEntityList);

            // making entry
            IntPtr listEntry = memEdit.ReadPointer(entityList, 0x10);

            // radar hack function stats here

            if (enableRadar)
            {
                for (int i = 0; i < 64; i++)
                {
                    if (listEntry == IntPtr.Zero)
                        continue;

                    IntPtr currentController = memEdit.ReadPointer(listEntry, i * 0x78);
                    if (currentController == IntPtr.Zero)
                        continue;

                    int pawnHandle = memEdit.ReadInt(currentController, m_hPlayerPawn);
                    if (pawnHandle == 0)
                        continue;

                    IntPtr listEntry2 = memEdit.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
                    IntPtr currentPawn = memEdit.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));

                    string name = memEdit.ReadString(currentController, m_iszPlayerName, 16);
                    bool spotted = memEdit.ReadBool(currentPawn, m_entitySpottedState + m_bSpotted);

                    memEdit.WriteBool(currentPawn, m_entitySpottedState + m_bSpotted, true);

                    string spottedStatus = spotted == true ? "spotted" : " ";

                    Console.WriteLine($"{name}: {spottedStatus}");

                }
            }

            // radar hack function ends here

            // anti flash functions goes here ----------------------------------------------------> AntiFlash Start
            float flashDuration = memEdit.ReadFloat(localPlayerPawn, m_flFlashBangTime);

            if (enableAntiFlashBang)
            {
                if (flashDuration > 0)
                {
                    memEdit.WriteFloat(localPlayerPawn, m_flFlashBangTime, 0);
                }
            }

            // anti flash ends here  <-------------------------------------------------------------------- AntiFlash End

            // jump shot functions goes here -------------------------------------------------------------> Jumphot Start

            int fFlag = memEdit.ReadInt(localPlayerPawn, m_fFlags);

            // character constants for Jumpshot
            const int INAIR = 65664;
            const int PLUS = 65537;
            const int MINUS = 256;
            const int HOTKEY = 0x12;

            // getting vector3 velocity
            Vector3 velocity;

            if (enableJumpShot)
            {


                if (fFlag == INAIR && GetAsyncKeyState(HOTKEY) < 0)
                {
                    Thread.Sleep(100);
                    velocity = memEdit.ReadVec(localPlayerPawn, m_vecAbsVelocity);

                    while (velocity.Z > 18f || velocity.Z < -18f)
                    {
                        velocity = memEdit.ReadVec(localPlayerPawn, m_vecAbsVelocity);
                    }

                    memEdit.WriteInt(client, dwForceAttack, PLUS);
                    Thread.Sleep(150);
                    memEdit.WriteInt(client, dwForceAttack, MINUS);
                    Thread.Sleep(1000);
                }
            }

            // jumpshot functions ends here <------------------------------------------------------------- Jumphot End

            // chams function starts here  -------------------------------------------------------------> Jumphot Start

            if (enableChams)
            {
                // chams functions starts here
                for (int i = 0; i < 64; i++)
                {
                    if (listEntry == IntPtr.Zero)
                        continue;

                    IntPtr currentController = memEdit.ReadPointer(listEntry, i * 0x78);
                    if (currentController == IntPtr.Zero)
                        continue;

                    int pawnHandle = memEdit.ReadInt(currentController, m_hPlayerPawn);
                    if (pawnHandle == 0)
                        continue;

                    IntPtr listEntry2 = memEdit.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
                    IntPtr currentPawn = memEdit.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));

                    memEdit.WriteFloat(currentPawn, m_flDetectedByEnemySensorTime, 86400);

                    //Console.WriteLine($"{i}: {currentPawn}");
                }
            }



            // chams function ends here  <------------------------------------------------------------- Jumphot End

            // draw stuffs

            if (enableEsp)
            {
                foreach (var entity in entities)
                {
                    // check if entites on screen

                    if (EntityonScreen(entity))
                    {
                        // draw methods
                        DrawHealthBar(entity);
                        DrawName(entity, 20);
                        Drawbox(entity);
                        Drawline(entity);
                        DrawBones(entity);


                    }
                }
            }

        }

        // check position

        bool EntityonScreen(Entity entity)
        {
            if (entity.position2D.X > 0 && entity.position2D.X < screenSize.X && entity.position2D.Y > 0 && entity.position2D.Y < screenSize.Y)
            {
                return true;
            }
            return false;
        }


        // drawing methods

        private void DrawBones(Entity entity)
        {
            if (enableBones)
            {
                uint uintColor = ImGui.ColorConvertFloat4ToU32(boneColor);

                float currentBoneThickness = boneThickness / entity.distance;

                drawList.AddLine(entity.bones2d[1], entity.bones2d[2], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[1], entity.bones2d[3], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[1], entity.bones2d[6], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[3], entity.bones2d[4], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[6], entity.bones2d[7], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[4], entity.bones2d[5], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[7], entity.bones2d[8], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[1], entity.bones2d[0], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[0], entity.bones2d[9], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[0], entity.bones2d[11], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[9], entity.bones2d[10], uintColor, currentBoneThickness);
                drawList.AddLine(entity.bones2d[11], entity.bones2d[12], uintColor, currentBoneThickness);
                drawList.AddCircleFilled(entity.bones2d[2], 3 + currentBoneThickness, uintColor);
            }


        }

        private void DrawName(Entity entity, int yOffset)
        {
            if (enableName)
            {
                Vector2 textPosition = new Vector2(entity.viewPosition2D.X, entity.viewPosition2D.Y - yOffset);
                drawList.AddText(textPosition, ImGui.ColorConvertFloat4ToU32(nameColor), $"{entity.name}");
            }
        }

        private void Drawbox(Entity entity)
        {
            if (enableBox)
            {
                // calcualte box hieght
                float entityHieght = entity.position2D.Y - entity.viewPosition2D.Y;

                Vector2 rectTop = new Vector2(entity.viewPosition2D.X - entityHieght / 2, entity.viewPosition2D.Y);

                Vector2 rectBottom = new Vector2(entity.position2D.X + entityHieght / 2, entity.position2D.Y);

                // get correct color
                Vector4 boxColor = localPlayer.team == entity.team ? teamColor : enemyColor;
                Vector4 textColor = localPlayer.team == entity.team ? teamColor : enemyColor;

                drawList.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
            }


        }

        private void Drawline(Entity entity)
        {
            if (enableLine)
            {
                // get correct color
                Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;

                // draw line 

                drawList.AddLine(new Vector2(screenSize.X / 2, screenSize.Y / 2), entity.position2D, ImGui.ColorConvertFloat4ToU32(lineColor));
            }
        }



        private void DrawHealthBar(Entity entity)
        {
            if (enableHealthBar)
            {
                float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
                float boxLeft = entity.viewPosition2D.X - entityHeight / 2;
                float boxRight = entity.viewPosition2D.X + entityHeight / 3;

                float barPercentWidth = 0.05f;
                float barPixelWidth = barPercentWidth * (boxRight - boxLeft);
                float barHeight = entityHeight * (entity.health / 100f);

                Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.position2D.Y - barHeight);
                Vector2 barBottom = new Vector2(boxLeft, entity.position2D.Y);

                // bar color

                Vector4 barColor = new Vector4(0, 1, 0, 1);
                drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(barColor));
            }
        }




        // transfering entities mode

        // updateEntities
        public void UpdateEntities(IEnumerable<Entity> newEntities)
        {
            entities = new ConcurrentQueue<Entity>(newEntities);
        }


        // updateLocalPlayer
        public void UpdateLocalPlayer(Entity newEntity)
        {
            lock (entitylock)
            {
                localPlayer = newEntity;
            }
        }


        // get localplayer
        public Entity GetLocalPlayer()
        {
            lock (entitylock)
            {
                return localPlayer;
            }
        }

        void DrawOverlay(Vector2 screenSize) // overlay window
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
                );
        }

    }

}