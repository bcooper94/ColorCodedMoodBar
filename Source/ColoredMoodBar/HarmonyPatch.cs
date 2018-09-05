using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Harmony;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MoodBarPatch {
    [StaticConstructorOnStartup]
    public class Main {
        public static MethodInfo drawSelectionOverlayOnGUIMethod;
        public static MethodInfo drawCaravanSelectionOverlayOnGUIMethod;
        public static MethodInfo getPawnTextureRectMethod;
        public static MethodInfo drawIconsMethod;
        public static FieldInfo pawnTextureCameraOffsetField;
        public static FieldInfo deadColonistTexField;
        public static FieldInfo pawnLabelsCacheField;

        public static Texture2D extremeBreakTex;
        public static Texture2D majorBreakTex;
        public static Texture2D minorBreakTex;
        public static Texture2D neutralTex;
        public static Texture2D contentTex;
        public static Texture2D happyTex;

        static Main() {
            var harmony = HarmonyInstance.Create("com.github.restive2k12.rimworld.mod.moodbar");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            drawSelectionOverlayOnGUIMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawSelectionOverlayOnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(Pawn), typeof(Rect) }, null);
            drawCaravanSelectionOverlayOnGUIMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawCaravanSelectionOverlayOnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(Caravan), typeof(Rect) }, null);
            getPawnTextureRectMethod = typeof(ColonistBarColonistDrawer).GetMethod("GetPawnTextureRect", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[] { typeof(float), typeof(float) }, null);

            drawIconsMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawIcons", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[] { typeof(Rect), typeof(Pawn) }, null);
            pawnTextureCameraOffsetField = typeof(ColonistBarColonistDrawer).GetField("PawnTextureCameraOffset",
                BindingFlags.Static | BindingFlags.NonPublic);
            deadColonistTexField = typeof(ColonistBarColonistDrawer).GetField("DeadColonistTex",
                    BindingFlags.Static | BindingFlags.NonPublic);
            pawnLabelsCacheField = typeof(ColonistBarColonistDrawer).GetField("pawnLabelsCache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float colorAlpha = 0.6f;
            Color red = Color.red;
            Color orange = new Color(1f, 0.5f, 0.31f, colorAlpha);
            Color yellow = Color.yellow;
            Color neutralColor = new Color(0.87f, 0.96f, 0.79f, colorAlpha);
            Color cyan = Color.cyan;
            Color happyColor = new Color(0.1f, 0.75f, 0.2f, colorAlpha);
            red.a = orange.a = yellow.a = cyan.a = colorAlpha;

            extremeBreakTex = SolidColorMaterials.NewSolidColorTexture(red);
            majorBreakTex = SolidColorMaterials.NewSolidColorTexture(orange);
            minorBreakTex = SolidColorMaterials.NewSolidColorTexture(yellow);
            neutralTex = SolidColorMaterials.NewSolidColorTexture(neutralColor);
            contentTex = SolidColorMaterials.NewSolidColorTexture(cyan);
            happyTex = SolidColorMaterials.NewSolidColorTexture(happyColor);
            LogMessage("initialized");
        }

        public static void LogMessage(string text) {
            Log.Message("[ColorCodedMoodBar] " + text);
        }
    }

    [HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawColonist")]
    public class MoodPatch {
        private static float ApplyEntryInAnotherMapAlphaFactor(Map map, float alpha) {
            //Unnecessary code?
            /*if (map == null) {
                if (!WorldRendererUtility.WorldRenderedNow) {
                    alpha = Mathf.Min(alpha, 0.4f);
                }
            }
            
            else if (map != Find.VisibleMap || WorldRendererUtility.WorldRenderedNow) {
                alpha = Mathf.Min(alpha, 0.4f);
            }*/

            return alpha;// Mathf.Min(alpha, 0.4f); 
        }

        public static bool Prefix(ColonistBarColonistDrawer __instance, ref Rect rect, ref Pawn colonist, ref Map pawnMap) {
            ColonistBar colonistBar = Find.ColonistBar;
            float entryRectAlpha = colonistBar.GetEntryRectAlpha(rect);
            entryRectAlpha = ApplyEntryInAnotherMapAlphaFactor(pawnMap, entryRectAlpha);

            bool flag = (!colonist.Dead) ? Find.Selector.SelectedObjects.Contains(colonist) : Find.Selector.SelectedObjects.Contains(colonist.Corpse);
            Color color = new Color(1f, 1f, 1f, entryRectAlpha);

            //pawn image background size
            Rect pawnBackgroundSize = rect.ExpandedBy(2.5f);
            //pawnBackgroundSize.width += 5f;
            //pawn image size
            const float newSizeVal = 0f;


            GUI.color = color;
            GUI.DrawTexture(pawnBackgroundSize, ColonistBar.BGTex);

            if (colonist.needs != null && colonist.needs.mood != null) {
                Rect position = pawnBackgroundSize.ContractedBy(2f);
                float num = position.height * colonist.needs.mood.CurLevelPercentage;
                position.yMin = position.yMax - num;
                position.height = num;

                
                float statValue = colonist.GetStatValue(StatDefOf.MentalBreakThreshold, true);

                float currentMoodLevel = colonist.needs.mood.CurLevel;


                // Extreme break threshold
                if (currentMoodLevel <= statValue) {
                    GUI.DrawTexture(position, Main.extremeBreakTex);
                }
                // Major break threshold
                else if (currentMoodLevel <= statValue + 0.15f) {
                    GUI.DrawTexture(position, Main.majorBreakTex);
                }
                // Minor break threshold
                else if (currentMoodLevel <= statValue + 0.3f) {
                    GUI.DrawTexture(position, Main.minorBreakTex);
                }
                // Neutral
                else if (currentMoodLevel <= 0.65f) {
                    GUI.DrawTexture(position, Main.neutralTex);
                }
                // Content
                else if (currentMoodLevel <= 0.9f) {
                    GUI.DrawTexture(position, Main.contentTex);

                }
                // Happy
                else {
                    GUI.DrawTexture(position, Main.happyTex);
                }

            }

            //Selection rectangle size
            Rect rect2 = rect.ContractedBy(-6f * colonistBar.Scale);

            if (flag && !WorldRendererUtility.WorldRenderedNow) {
                Main.drawSelectionOverlayOnGUIMethod.Invoke(__instance, new object[] { colonist, rect2 });

            } else if (WorldRendererUtility.WorldRenderedNow && colonist.IsCaravanMember() && Find.WorldSelector.IsSelected(colonist.GetCaravan())) {
                Main.drawCaravanSelectionOverlayOnGUIMethod.Invoke(__instance, new object[] { colonist.GetCaravan(), rect2 });

            }

            
            Rect pawnTexturePosition = __instance.GetPawnTextureRect(new Vector2(rect.x, rect.y)).ContractedBy(newSizeVal);//(Rect) Main.getPawnTextureRectMethod.Invoke(__instance, new object[] { rect.x, rect.y });
            pawnTexturePosition.y += newSizeVal-1f;

            if (pawnBackgroundSize.width % 2 == 0) {
                pawnTexturePosition.x -= 1f;
                //Log.Message("[ColorCodedMoodBar] EVEN " + pawnBackgroundSize.width);
            } else {
                //Log.Message("[ColorCodedMoodBar] UNEVEN " + pawnBackgroundSize.width);
                pawnTexturePosition.x -= 0.25f;
            }

            

            GUI.DrawTexture(pawnTexturePosition, PortraitsCache.Get(colonist, ColonistBarColonistDrawer.PawnTextureSize,
                ColonistBarColonistDrawer.PawnTextureCameraOffset,
                1.28205f));


            GUI.color = new Color(1f, 1f, 1f, entryRectAlpha * 0.8f);
            Main.drawIconsMethod.Invoke(__instance, new object[] { rect, colonist });

            GUI.color = color;

            if (colonist.Dead) {
                GUI.DrawTexture(rect, (Texture)Main.deadColonistTexField.GetValue(null));
            }

            float num2 = 4f * colonistBar.Scale;
            Vector2 pos = new Vector2(rect.center.x, rect.yMax - num2);
            GenMapUI.DrawPawnLabel(colonist, pos, entryRectAlpha,
                rect.width + colonistBar.SpaceBetweenColonistsHorizontal - 2f,
                (Dictionary<string, string>)Main.pawnLabelsCacheField.GetValue(__instance),
                GameFont.Tiny, true, true);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            return false;
        }
    }
}