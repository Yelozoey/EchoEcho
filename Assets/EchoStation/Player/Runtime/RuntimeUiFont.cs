using System;
using UnityEngine;

namespace EchoStation.Player
{
    public static class RuntimeUiFont
    {
        private static Font font;
        public static Font Get()
        {
            if(font)return font;font=Resources.Load<Font>("NotoSansCJKsc-Regular");
            if(!font)throw new InvalidOperationException("Bundled OFL CJK UI font is missing.");return font;
        }
    }
}
