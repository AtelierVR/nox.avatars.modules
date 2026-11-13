using Nox.Avatars.Parameters;
using Nox.CCK.Events;
using Nox.CCK.Network;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.CCK.Avatars.Parameters {
	/// <summary>
	/// A complex parameter (Color, Vector3, Quaternion) backed by one scalar Animator parameter per
	/// component (<c>Name.R</c>, <c>Name.G</c>, ...): the Animator has no vector or colour parameter,
	/// so the module composes this single, typed parameter from the scalars the avatar declares.
	/// <para>
	/// The component names and the composition live in the <see cref="ParameterTypeProvider"/> of the
	/// type: reading gathers the components (missing ones use the provider identity), writing spreads
	/// the value back over them. State behaviours and the network therefore keep working with the
	/// complex parameter, while the Animator only ever sees plain floats.
	/// </para>
	/// </summary>
	public class CompositeParameter : IParameter {
		private readonly string                _name;
		private readonly ParameterFlags        _flags;
		private readonly ParameterTypeProvider _provider;
		private readonly IParameter[]          _components;

		/// <summary>Reused buffer of the component values (a component may itself be a complex value).</summary>
		private readonly object[] _values;

		/// <summary>Raised when the value changes: a write on the composite, or on one of its components.</summary>
		public UnityEvent<IParameter> OnValueChanged { get; } = new NoxEvent<IParameter>();

		public CompositeParameter(
			string name,
			ParameterTypeProvider
			provider,
			IParameter[] components,
			ParameterFlags flags,
			string category = null
		) {
			_name       = name;
			_provider   = provider;
			_components = components;
			_flags      = flags;
			Category    = category;
			_values     = new object[provider.Components.Length];

			// A component written directly (a state behaviour, the network) changes the composite too.
			foreach (var component in components)
				component?.OnValueChanged?.AddListener(OnComponentChanged);
		}

		public string Name
			=> _name;

		/// <summary>Sync key: the CRC32 of the name, like every other parameter.</summary>
		public int Key
			=> _name.Hash();

		/// <summary>Category of the parameter, or <c>null</c> when it declares none (see <see cref="IParameter.Category"/>).</summary>
		public string Category { get; }

		public ParameterFlags Flags
			=> _flags;

		public ParameterType ValueType
			=> _provider.Type;

		public object Value {
			get {
				for (var i = 0; i < _values.Length; i++)
					_values[i] = _components[i]?.Value;

				return _provider.Compose(_values);
			}
				set {
					var values = _provider.Decompose(value);
					for (var i = 0; i < values.Length && i < _components.Length; i++)
						Write(i, values[i]);

					OnValueChanged.Invoke(this);
				}
		}

		/// <summary>A component moved: the value of the composite changed with it.</summary>
		private void OnComponentChanged(IParameter component)
			=> OnValueChanged.Invoke(this);

		/// <summary>Writes component <paramref name="index"/> when the avatar declares it.</summary>
		private void Write(int index, object value) {
			var component = _components[index];
			if (component == null)
				return;

			if (AreEqual(component.Value, value))
				return;

			component.Value = value;
		}

		/// <summary>Value equality of two component values (floats compare with a tolerance).</summary>
		private static bool AreEqual(object a, object b) {
			if (a == null || b == null)
				return a == b;

			return (a, b) switch {
				(float fa, float fb)          => Mathf.Approximately(fa, fb),
				(Vector3 va, Vector3 vb)      => va == vb,
				(Quaternion qa, Quaternion qb) => qa == qb,
				(Color ca, Color cb)          => ca == cb,
				_                             => a.Equals(b)
			};
		}
	}
}
