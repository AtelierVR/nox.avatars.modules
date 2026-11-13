using Nox.CCK.Language;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// Language entries of the parameter categories: the editors group the parameters by category
	/// (see <c>IParameter.Category</c>, in snake_case), then sort them by name inside each section.
	/// <para>
	/// A category resolves through <c>avatar.parameters.categories.&lt;category&gt;.label</c> and
	/// <c>avatar.parameters.categories.&lt;category&gt;.description</c>. A parameter without a category
	/// belongs to the « general » section (<see cref="General"/>), which resolves the same way.
	/// </para>
	/// </summary>
	public static class ParameterCategoryLanguage {
		/// <summary>Category of the parameters that declare none.</summary>
		public const string General = ParameterCategory.General;

		/// <summary>Prefix of the entries: <c>avatar.parameters.categories.&lt;category&gt;.</c></summary>
		public const string KeyPrefix = "avatar.parameters.categories.";

		/// <summary>Suffix of the label entry of a category.</summary>
		public const string LabelSuffix = ".label";

		/// <summary>Suffix of the description entry of a category.</summary>
		public const string DescriptionSuffix = ".description";

		/// <summary>
		/// Normalizes a declared category: trimmed, or <c>null</c> when the parameter declares none
		/// (empty or whitespace).
		/// </summary>
		public static string Normalize(string category)
			=> string.IsNullOrWhiteSpace(category) ? null : category.Trim();

		/// <summary>Category used by the editors: the normalized one, or <see cref="General"/>.</summary>
		public static string NormalizedOrDefault(string category)
			=> Normalize(category) ?? General;

		/// <summary>Language entry of the label of a category.</summary>
		public static string LabelKey(string category)
			=> KeyPrefix + NormalizedOrDefault(category) + LabelSuffix;

		/// <summary>Language entry of the description of a category.</summary>
		public static string DescriptionKey(string category)
			=> KeyPrefix + NormalizedOrDefault(category) + DescriptionSuffix;

		/// <summary>
		/// Label of a category, resolved from the language packs; falls back to the category itself in
		/// title case (see <see cref="DisplayName"/>) when the packs declare no entry.
		/// </summary>
		public static string GetLabel(string category) {
			var key = LabelKey(category);
			return LanguageManager.Has(key)
				? LanguageManager.Get(key)
				: DisplayName(NormalizedOrDefault(category));
		}

		/// <summary>
		/// Description of a category, resolved from the language packs, or <c>null</c> when the packs
		/// declare no entry.
		/// </summary>
		public static string GetDescription(string category) {
			var key = DescriptionKey(category);
			return LanguageManager.Has(key) ? LanguageManager.Get(key) : null;
		}

		/// <summary>
		/// Turns a snake_case category into a readable name (<c>locomotion_speed</c> → <c>Locomotion Speed</c>),
		/// used when a category has no language entry.
		/// </summary>
		public static string DisplayName(string category) {
			if (string.IsNullOrEmpty(category))
				return string.Empty;

			var words = category.Split('_');
			var text  = new System.Text.StringBuilder(category.Length);

			foreach (var word in words) {
				if (word.Length == 0)
					continue;

				if (text.Length > 0)
					text.Append(' ');

				text.Append(char.ToUpperInvariant(word[0]));
				if (word.Length > 1)
					text.Append(word, 1, word.Length - 1);
			}

			return text.ToString();
		}
	}
}
