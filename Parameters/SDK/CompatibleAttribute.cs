using System;

namespace Nox.Avatars.Parameters
{
    /// <summary>
    /// Attribut pour marquer les types de paramètres avec des catégories de compatibilité.
    /// Exemples : [Compatible("numeric", "integer")], [Compatible("numeric", "floating")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class CompatibleAttribute : Attribute
    {
        public string[] Categories { get; }

        public CompatibleAttribute(params string[] categories)
            => Categories = categories ?? Array.Empty<string>();
    }
}
