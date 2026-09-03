using System;
using UnityEngine;

namespace Nox.CCK.Avatars.Menus
{
    [CreateAssetMenu(fileName = "AvatarMenu", menuName = "Nox/Avatars/Menu", order = 1)]
    public class AvatarMenu : ScriptableObject
    {
        public MenuEntry[] entries = Array.Empty<MenuEntry>();
    }
}