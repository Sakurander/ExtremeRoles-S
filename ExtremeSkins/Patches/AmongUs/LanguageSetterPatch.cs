﻿using ExtremeSkins.SkinManager;

namespace ExtremeSkins.Patches.AmongUs
{
    public static class LanguageSetterPatch
    {
        public static void Postfix()
        {
#if WITHHAT
            ExtremeHatManager.UpdateTranslation();
#endif
#if WITHNAMEPLATE
            ExtremeNamePlateManager.UpdateTranslation();
#endif
#if WITHNAMEPLATE
            ExtremeVisorManager.UpdateTranslation();
#endif
        }
    }
}
