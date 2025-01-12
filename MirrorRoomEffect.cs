using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ConnectionRandomizer
{

    /**
     * This code is just a copy of Gamer025's shader for flipping rooms in Rain World: Chaos Edition
     * Link to the GitHub repository: https://github.com/Gamer025/RainworldCE/blob/master/Events/FlipCamera.cs
     */

    public class MirrorRoomEffect : IDrawable
    {
        float xFlip = 0;
        bool done;

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            //rCam.ReturnFContainer("PostProcessing").AddChild(sLeaser.sprites[0]);

            //added to Bloom because this is the last FContainer BEFORE the HUD
            rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[0]);

            //rCam.ReturnFContainer("HUD_PostProcessing").AddChild(sLeaser.sprites[0]);
            if (rCam.hud != null)
            {
                //foreach (FContainer container in rCam.hud.fContainers)
                    //container.AddChild(sLeaser.sprites[1]);
                //if (rCam.hud.fContainers.Length >= 2)
                //rCam.hud.fContainers[1].AddChild(sLeaser.sprites[1]);
                //if (rCam.hud.map != null)
                    //rCam.hud.map.inFrontContainer.AddChild(sLeaser.sprites[1]);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            //I don't want a gradual flip
            /*
            if (!done)
            {
                xFlip += 0.020f;
                Shader.SetGlobalFloat("TheLazyCowboy1_XFlip", xFlip);

                if (xFlip > 1f)
                {
                    xFlip = 1f;
                    done = true;
                    Shader.SetGlobalFloat("TheLazyCowboy1_XFlip", xFlip);
                }
            }
            */
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White");
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["LazyCowboy_MirrorRoomPP"];
            sLeaser.sprites[0].scaleX = rCam.game.rainWorld.options.ScreenSize.x / 16f;
            sLeaser.sprites[0].scaleY = 48f;
            sLeaser.sprites[0].anchorX = 0f;
            sLeaser.sprites[0].anchorY = 0f;
            /*
            sLeaser.sprites[1] = new FSprite("Futile_White");
            sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["LazyCowboy_MirrorRoomPP2"];
            sLeaser.sprites[1].scaleX = rCam.game.rainWorld.options.ScreenSize.x / 16f;
            sLeaser.sprites[1].scaleY = 48f;
            sLeaser.sprites[1].anchorX = 0f;
            sLeaser.sprites[1].anchorY = 0f;
            */
            AddToContainer(sLeaser, rCam, null);
        }
    }
}
