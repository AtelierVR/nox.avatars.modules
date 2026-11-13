using System.Collections.Generic;
using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// A component of a complex parameter: a name and its own <see cref="ParameterType"/>, so a
	/// component is not necessarily a float and can itself be a complex type (a sub-composite).
	/// </summary>
	public sealed class ParameterComponent {
		public ParameterComponent(string name, ParameterType type) {
			Name = name;
			Type = type;
		}

		/// <summary>Name appended to the parameter name through the separator (<c>Name/Component</c>).</summary>
		public string Name { get; }

		/// <summary>Type of the component: a scalar, or another complex type (a sub-composite).</summary>
		public ParameterType Type { get; }
	}

	/// <summary>
	/// Splits a complex parameter type into its components and joins them back into a value
	/// (see <see cref="CompositeParameter"/>).
	/// <para>
	/// A provider is the only place that knows how a type is built: the avatar parameter module uses
	/// <see cref="Components"/> to resolve the component parameters (recursively for a complex
	/// component), and the composite parameter calls <see cref="Compose"/> / <see cref="Decompose"/>
	/// from its getter and setter.
	/// </para>
	/// </summary>
	public abstract class ParameterTypeProvider {
		/// <summary>Type of the complex parameter this provider builds.</summary>
		public abstract ParameterType Type { get; }

		/// <summary>Components of the value, in order (a component may be complex itself).</summary>
		public abstract ParameterComponent[] Components { get; }

		/// <summary>
		/// Builds the complex value from the component values, in <see cref="Components"/> order. A
		/// missing component is <c>null</c>: the provider falls back to the identity of the value.
		/// </summary>
		public abstract object Compose(object[] values);

		/// <summary>Splits a value into its component values, in <see cref="Components"/> order.</summary>
		public abstract object[] Decompose(object value);

		/// <summary>Component <paramref name="index"/> as a float, or <paramref name="fallback"/> when missing.</summary>
		protected static float Float(object[] values, int index, float fallback)
			=> values.Length > index && values[index] != null ? values[index].ToFloat() : fallback;

		/// <summary>Component <paramref name="index"/> as a vector, or <paramref name="fallback"/> when missing.</summary>
		protected static Vector3 Vector(object[] values, int index, Vector3 fallback)
			=> values.Length > index && values[index] != null ? values[index].ToVector3() : fallback;
	}

	/// <summary>Registry of the providers, by parameter type.</summary>
	public static class ParameterTypeProviders {
		private static readonly Dictionary<ParameterType, ParameterTypeProvider> _providers = new() {
			[ParameterType.Color]      = new ColorParameterProvider(),
			[ParameterType.Vector3]    = new Vector3ParameterProvider(),
			[ParameterType.Quaternion] = new QuaternionParameterProvider()
		};

		/// <summary>Provider of <paramref name="type"/>, or <c>false</c> when the type is not a complex one.</summary>
		public static bool TryGet(ParameterType type, out ParameterTypeProvider provider)
			=> _providers.TryGetValue(type, out provider);
	}

	/// <summary>Colour as four channels: <c>R</c>, <c>G</c>, <c>B</c>, <c>A</c> (alpha defaults to 1).</summary>
	public sealed class ColorParameterProvider : ParameterTypeProvider {
		private static readonly ParameterComponent[] _components = {
			new("R", ParameterType.Float),
			new("G", ParameterType.Float),
			new("B", ParameterType.Float),
			new("A", ParameterType.Float)
		};

		public override ParameterType Type
			=> ParameterType.Color;

		public override ParameterComponent[] Components
			=> _components;

		public override object Compose(object[] values)
			=> new Color(
				Float(values, 0, 0f),
				Float(values, 1, 0f),
				Float(values, 2, 0f),
				Float(values, 3, 1f)
			);

		public override object[] Decompose(object value) {
			var color = value.ToColor();
			return new object[] { color.r, color.g, color.b, color.a };
		}
	}

	/// <summary>
	/// Vector as three components (<c>X</c>, <c>Y</c>, <c>Z</c>) plus a derived <c>Magnitude</c>.
	/// <para>
	/// An animator only has scalar parameters, and a locomotion blend tree commonly blends on the speed:
	/// writing <c>Name/Magnitude</c> with the vector saves every consumer from recomputing it, and an
	/// avatar that does not declare it simply gets nothing written (the component is missing).
	/// </para>
	/// </summary>
	public sealed class Vector3ParameterProvider : ParameterTypeProvider {
		private static readonly ParameterComponent[] _components = {
			new("X", ParameterType.Float),
			new("Y", ParameterType.Float),
			new("Z", ParameterType.Float),
			new("Magnitude", ParameterType.Float)
		};

		public override ParameterType Type
			=> ParameterType.Vector3;

		public override ParameterComponent[] Components
			=> _components;

		/// <summary>Built from <c>X</c>, <c>Y</c> and <c>Z</c>: the magnitude is derived, never a source.</summary>
		public override object Compose(object[] values)
			=> new Vector3(
				Float(values, 0, 0f),
				Float(values, 1, 0f),
				Float(values, 2, 0f)
			);

		public override object[] Decompose(object value) {
			var vector = value.ToVector3();
			return new object[] { vector.x, vector.y, vector.z, vector.magnitude };
		}
	}

	/// <summary>
	/// Quaternion as its four components (<c>X</c>, <c>Y</c>, <c>Z</c>, <c>W</c>) plus an
	/// <c>Euler</c> component, which is itself a <see cref="ParameterType.Vector3"/> (a
	/// sub-composite: <c>Name/Euler/X</c>, ...). The composed value prefers the direct components;
	/// the euler one is only used when the avatar does not declare them, and both are written on
	/// assignment.
	/// </summary>
	public sealed class QuaternionParameterProvider : ParameterTypeProvider {
		private const int Euler = 4;

		private static readonly ParameterComponent[] _components = {
			new("X", ParameterType.Float),
			new("Y", ParameterType.Float),
			new("Z", ParameterType.Float),
			new("W", ParameterType.Float),
			new("Euler", ParameterType.Vector3)
		};

		public override ParameterType Type
			=> ParameterType.Quaternion;

		public override ParameterComponent[] Components
			=> _components;

		public override object Compose(object[] values) {
			var hasDirect = false;
			for (var i = 0; i < Euler; i++)
				hasDirect |= values.Length > i && values[i] != null;

			return hasDirect
				? new Quaternion(
					Float(values, 0, 0f),
					Float(values, 1, 0f),
					Float(values, 2, 0f),
					Float(values, 3, 1f)
				)
				: Quaternion.Euler(Vector(values, Euler, Vector3.zero));
		}

		public override object[] Decompose(object value) {
			var rotation = value.ToQuaternion();
			return new object[] { rotation.x, rotation.y, rotation.z, rotation.w, rotation.eulerAngles };
		}
	}
}
