using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using BepInEx.Logging;
using System.Linq;

namespace ConnectionRandomizer
{
    public static class RoomCameraExtension
    {
        public static ConditionalWeakTable<RoomCamera, List<RoomCamera.SpriteLeaser>> ppEffects = new ConditionalWeakTable<RoomCamera, List<RoomCamera.SpriteLeaser>>();

        public static void AddPPEffect(this RoomCamera cam, IDrawable drawable)
        {
            if (ppEffects.TryGetValue(cam, out List<RoomCamera.SpriteLeaser> effects))
            {
                //RainWorldCE.ME.Logger_p.Log(LogLevel.Debug, $"Adding PostProcess effect {drawable.GetType()} to camera");
                effects.Add(new RoomCamera.SpriteLeaser(drawable, cam));
            }
        }

        public static void RemovePPEffect(this RoomCamera cam, Type effectType)
        {
            if (ppEffects.TryGetValue(cam, out List<RoomCamera.SpriteLeaser> effects))
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i].drawableObject.GetType() == effectType)
                    {
                        //RainWorldCE.ME.Logger_p.Log(LogLevel.Debug, $"Removing PostProcess effect {effectType} from camera");
                        effects[i].CleanSpritesAndRemove();
                        effects.Remove(effects[i]);
                    }
                }
            }
        }

        public static void RoomCameraCtorHook(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            orig(self, game, cameraNumber);
            /*
            Array.Resize(ref self.SpriteLayers, self.SpriteLayers.Length + 1);
            int index = self.SpriteLayers.Length - 1;
            self.SpriteLayers[index] = new FContainer();
            Futile.stage.AddChild(self.SpriteLayers[index]);
            self.SpriteLayerIndex.Add("PostProcessing", index);
            ppEffects.Add(self, new List<RoomCamera.SpriteLeaser>());
            */
            //modified to put "PostProcessing" right before HUD
            /*
            int index = self.SpriteLayers.Length;
            if (true)//self.SpriteLayerIndex.TryGetValue("HUD", out int index))
            {
                FContainer[] newContainerArray = new FContainer[self.SpriteLayers.Length + 1];//2];
                for (int i = 0; i < self.SpriteLayers.Length; i++)
                {
                    if (i < index)
                        newContainerArray[i] = self.SpriteLayers[i];
                    else
                        newContainerArray[i + 1] = self.SpriteLayers[i];
                }
                newContainerArray[index] = new FContainer();
                self.SpriteLayers = newContainerArray;

                string[] spriteKeys = self.SpriteLayerIndex.Keys.ToArray();
                foreach (string key in spriteKeys)
                {
                    if (self.SpriteLayerIndex[key] >= index)
                        self.SpriteLayerIndex[key] = self.SpriteLayerIndex[key] + 1;
                }

                Futile.stage.AddChild(self.SpriteLayers[index]);
                self.SpriteLayerIndex.Add("PostProcessing", index);
            */
                /*
                index = self.SpriteLayers.Length - 1;
                self.SpriteLayers[index] = new FContainer();
                Futile.stage.AddChild(self.SpriteLayers[index]);
                self.SpriteLayerIndex.Add("PostProcessing", index);
                */  
            //}
            ppEffects.Add(self, new List<RoomCamera.SpriteLeaser>());
        }

        public static void RoomCameraDrawUpdateHook(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            //draw hud THEN mirror it
            //then temporarily set hud to null so it doesn't get redrawn
            /*
            HUD.HUD tempHUD = self.hud;
            if (self.hud != null)
            {
                self.hud.Draw(timeStacker);
                Vector2 vector1 = Vector2.Lerp(self.lastPos, self.pos, timeStacker);
                if (ppEffects.TryGetValue(self, out List<RoomCamera.SpriteLeaser> effects1))
                {
                    for (int i = 0; i < effects1.Count; i++)
                    {
                        effects1[i].Update(timeStacker, self, vector1);
                        if (effects1[i].deleteMeNextFrame)
                        {
                            effects1.RemoveAt(i);
                        }
                    }
                }
                tempHUD = self.hud;
                self.hud = null;
            }
            */
            //then re-mirror the camera

            orig(self, timeStacker, timeSpeed);
            Vector2 vector = Vector2.Lerp(self.lastPos, self.pos, timeStacker);
            if (ppEffects.TryGetValue(self, out List<RoomCamera.SpriteLeaser> effects))
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    effects[i].Update(timeStacker, self, vector);
                    if (effects[i].deleteMeNextFrame)
                    {
                        effects.RemoveAt(i);
                    }
                }
            }

            //self.hud = tempHUD;
        }
    }
}