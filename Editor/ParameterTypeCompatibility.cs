using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Menus;
using UnityEngine;

namespace Nox.CCK.Avatars.Modules.Editor
{
    /// <summary>
    /// Définit les types de paramètres compatibles avec chaque type d'entrée de menu,
    /// en se basant sur les attributs [Compatible] définis sur ParameterType.
    /// </summary>
    public static class ParameterTypeCompatibility
    {
        private static readonly IReadOnlyList<ParameterType> _allTypes;
        private static readonly Dictionary<ParameterType, string[]> _categories;
        private static readonly Dictionary<ParameterType, string> _displayNames;

        static ParameterTypeCompatibility()
        {
            var enumType = typeof(ParameterType);
            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var types = new List<ParameterType>();
            var categoriesMap = new Dictionary<ParameterType, string[]>();
            var displayNamesMap = new Dictionary<ParameterType, string>();

            foreach (var field in fields)
            {
                if (field.FieldType != enumType) continue;
                var value = (ParameterType)field.GetValue(null)!;
                types.Add(value);

                // Attribut Compatible
                var compatAttr = field.GetCustomAttribute<CompatibleAttribute>();
                categoriesMap[value] = compatAttr?.Categories ?? Array.Empty<string>();

                // Attribut InspectorName pour l'affichage
                var inspectorAttr = field.GetCustomAttribute<InspectorNameAttribute>();
                displayNamesMap[value] = inspectorAttr != null ? inspectorAttr.displayName : value.ToString();
            }

            // Sort by enum value so the order is deterministic and follows the
            // declaration order (Bool = 0 first), independent of reflection order.
            types.Sort((a, b) => a.CompareTo(b));
            _allTypes = types.AsReadOnly();
            _categories = categoriesMap;
            _displayNames = displayNamesMap;
        }

        /// <summary>
        /// Retourne la liste des types de paramètres compatibles avec le type d'entrée donné.
        /// </summary>
        public static IReadOnlyList<ParameterType> GetCompatibleTypes(EntryType entryType)
        {
            // Définit les catégories requises pour chaque EntryType
            var requiredCategories = entryType switch
            {
                EntryType.Toggle      => new[] { "numeric" },
                EntryType.Trigger     => new[] { "numeric" },
                EntryType.Axis1D      => new[] { "numeric" },
                EntryType.Axis2D      => new[] { "numeric" },
                EntryType.Choice      => new[] { "numeric" },
                EntryType.Text        => new[] { "string" },
                EntryType.Menu        => Array.Empty<string>(),
                EntryType.ColorPicker => Array.Empty<string>(),
                _                     => Array.Empty<string>(),
            };

            if (requiredCategories.Length == 0)
                return Array.Empty<ParameterType>();

            // Filtre les types qui ont au moins une catégorie correspondante
            var result = new List<ParameterType>();
            foreach (var type in _allTypes)
            {
                if (!_categories.TryGetValue(type, out var typeCategories))
                    continue;

                // Vérifie si l'une des catégories du type correspond à une catégorie requise
                if (requiredCategories.Any(required => typeCategories.Contains(required)))
                    result.Add(type);
            }

            return result;
        }

        /// <summary>
        /// Retourne le nom d'affichage d'un ParameterType en utilisant [InspectorName] si présent,
        /// sinon .ToString().
        /// </summary>
        public static string GetDisplayName(ParameterType type)
            => _displayNames.TryGetValue(type, out var name) 
                ? name 
                : type.ToString();
    }
}