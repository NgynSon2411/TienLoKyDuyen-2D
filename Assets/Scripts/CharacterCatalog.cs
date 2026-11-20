using System;
using UnityEngine;

namespace TienLo
{
    [Serializable]
    internal sealed class CharacterCatalog
    {
        public Character[] characters;
    }

    [Serializable]
    internal sealed class Character
    {
        public string code;
        public string name;
        public string color;
        public string spec;
        public Frame[] idle;
        public Frame[] runL;
        public Frame[] runR;
        public Frame[] skillL;
        public Frame[] skillR;

        public Color Accent
        {
            get
            {
                ColorUtility.TryParseHtmlString(color, out Color result);
                return result;
            }
        }

        public Texture2D Texture(string animation, int index) =>
            Resources.Load<Texture2D>("TienLo/Characters/" + code + "/" + animation + index);
    }

    [Serializable]
    internal sealed class Frame
    {
        public int w;
        public int h;
        public int[] box;
    }
}
