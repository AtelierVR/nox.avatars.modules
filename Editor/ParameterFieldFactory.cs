using System;
using System.Text;
using Nox.CCK;
using Nox.CCK.Avatars.Parameters;
using Nox.Avatars.Parameters;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace Nox.CCK.Avatars.Modules.Editor
{
    /// <summary>
    /// Fabrique partagée de champs UI typés selon un <see cref="ParameterType"/>.
    /// Centralise la logique de création de champs et de rappel (callback) utilisée par
    /// <see cref="AvatarParametersEditor"/> et <see cref="AvatarParameterModuleEditor"/>,
    /// en s'appuyant sur <see cref="ParameterTypeCompatibility"/>.
    /// </summary>
    public static class ParameterFieldFactory
    {
        /// <summary>
        /// Crée un champ UI adapté au <see cref="ParameterType"/> donné et y associe un
        /// callback qui appelle <paramref name="onValueChanged"/> avec la nouvelle valeur
        /// encodée sous forme de <see cref="byte"/>.
        /// </summary>
        public static VisualElement CreateField(
            string label,
            ParameterType type,
            bool editable,
            object currentValue,
            Action<byte[]> onValueChanged)
        {
            if (type == ParameterType.ByteArray)
            {
                var textField = new TextField(label) { multiline = true, style = { flexGrow = 1 } };
                textField.SetValueWithoutNotify(currentValue is byte[] bytes ? Converter.ToBase64(bytes) : string.Empty);
                textField.RegisterValueChangedCallback(evt =>
                {
                    var decoded = Converter.FromBase64(evt.newValue);
                    if (decoded != null)
                        onValueChanged(decoded);
                });
                return textField;
            }

            var field = CreateFieldByType(type, label, currentValue, editable);
            if (field != null)
            {
                RegisterCallback(field, type, onValueChanged);
            }

            return field;
        }

        /// <summary>
        /// Crée un champ UI adapté au <see cref="ParameterType"/> donné sans callback
        /// (utilisé par <see cref="AvatarParametersEditor"/> pour les contrôles par défaut).
        /// </summary>
        public static VisualElement CreateFieldByType(ParameterType type, string label, object currentValue, bool editable)
        {
            return type switch
            {
                ParameterType.Bool => CreateBoolField(label, currentValue, editable),
                ParameterType.Byte or ParameterType.Short or ParameterType.UShort
                                or ParameterType.Int or ParameterType.UInt
                                or ParameterType.Long or ParameterType.ULong
                                => CreateIntegerField(label, type, currentValue, editable),
                ParameterType.Float => CreateFloatField(label, currentValue, editable),
                ParameterType.Double => CreateDoubleField(label, currentValue, editable),
                ParameterType.String => CreateStringField(label, currentValue, editable),
                ParameterType.Vector3 => CreateVector3Field(label, currentValue, editable),
                ParameterType.Quaternion => CreateQuaternionField(label, currentValue, editable),
                ParameterType.ByteArray => CreateByteArrayField(label, currentValue, editable),
                _ => CreateFallbackField(label, type),
            };
        }

        /// <summary>
        /// Associe un callback de changement de valeur au champ créé selon le <see cref="ParameterType"/>.
        /// </summary>
        public static void RegisterCallback(VisualElement field, ParameterType type, Action<byte[]> onValueChanged)
        {
            switch (type)
            {
                case ParameterType.Bool:
                    if (field is Toggle toggle)
                        toggle.RegisterValueChangedCallback(evt =>
                            onValueChanged(BitConverter.GetBytes(evt.newValue)));
                    break;

                case ParameterType.Byte:
                case ParameterType.Short:
                case ParameterType.UShort:
                case ParameterType.Int:
                case ParameterType.UInt:
                case ParameterType.Long:
                case ParameterType.ULong:
                    if (field is IntegerField intField)
                    {
                        intField.RegisterValueChangedCallback(evt =>
                        {
                            byte[] bytes = type switch
                            {
                                ParameterType.Byte   => new[] { ClampByte(evt.newValue) },
                                ParameterType.Short  => BitConverter.GetBytes((short)ClampShort(evt.newValue)),
                                ParameterType.UShort => BitConverter.GetBytes((ushort)ClampUShort(evt.newValue)),
                                ParameterType.Int    => BitConverter.GetBytes(ClampInt(evt.newValue)),
                                ParameterType.UInt   => BitConverter.GetBytes((uint)ClampUInt(evt.newValue)),
                                ParameterType.Long   => BitConverter.GetBytes(ClampLong(evt.newValue)),
                                ParameterType.ULong  => BitConverter.GetBytes((ulong)ClampULong(evt.newValue)),
                                _                    => Array.Empty<byte>()
                            };
                            onValueChanged(bytes);
                        });
                    }
                    break;

                case ParameterType.Float:
                    if (field is FloatField floatField)
                        floatField.RegisterValueChangedCallback(evt =>
                            onValueChanged(BitConverter.GetBytes((float)evt.newValue)));
                    break;

                case ParameterType.Double:
                    if (field is DoubleField doubleField)
                        doubleField.RegisterValueChangedCallback(evt =>
                            onValueChanged(BitConverter.GetBytes((double)evt.newValue)));
                    break;

                case ParameterType.String:
                    if (field is TextField textField)
                        textField.RegisterValueChangedCallback(evt =>
                            onValueChanged(Encoding.UTF8.GetBytes(evt.newValue ?? string.Empty)));
                    break;

                case ParameterType.Vector3:
                    if (field is Vector3Field vector3Field)
                        vector3Field.RegisterValueChangedCallback(evt =>
                        {
                            var vecBytes = Converter.ToBytes(evt.newValue);
                            onValueChanged(vecBytes);
                        });
                    break;

                case ParameterType.Quaternion:
                    if (field is Vector3Field quatField)
                        quatField.RegisterValueChangedCallback(evt =>
                        {
                            var quatBytes = Converter.ToBytes(Quaternion.Euler(evt.newValue));
                            onValueChanged(quatBytes);
                        });
                    break;

                case ParameterType.ByteArray:
                    // Géré séparément dans CreateField
                    break;
            }
        }

        /// <summary>
        /// Met à jour la valeur affichée par un champ sans déclencher de callback.
        /// </summary>
        public static void UpdateFieldValue(VisualElement field, ParameterType type, object value)
        {
            switch (type)
            {
                case ParameterType.Bool when field is Toggle toggle:
                    toggle.SetValueWithoutNotify(value.ToBool());
                    break;

                case ParameterType.Byte when field is IntegerField byteField:
                    byteField.SetValueWithoutNotify(value.ToByte());
                    break;
                case ParameterType.Short when field is IntegerField shortField:
                    shortField.SetValueWithoutNotify(value.ToShort());
                    break;
                case ParameterType.UShort when field is IntegerField ushortField:
                    ushortField.SetValueWithoutNotify(value.ToUShort());
                    break;
                case ParameterType.Int when field is IntegerField intField:
                    intField.SetValueWithoutNotify(value.ToInt());
                    break;
                case ParameterType.UInt when field is IntegerField uintField:
                    uintField.SetValueWithoutNotify((int)value.ToUInt());
                    break;
                case ParameterType.Long when field is IntegerField longField:
                    longField.SetValueWithoutNotify((int)value.ToLong());
                    break;
                case ParameterType.ULong when field is IntegerField ulongField:
                    ulongField.SetValueWithoutNotify((int)value.ToULong());
                    break;

                case ParameterType.Float when field is FloatField floatField:
                    floatField.SetValueWithoutNotify(value.ToFloat());
                    break;
                case ParameterType.Double when field is DoubleField doubleField:
                    doubleField.SetValueWithoutNotify(value.ToDouble());
                    break;

                case ParameterType.String when field is TextField stringField:
                    stringField.SetValueWithoutNotify(Converter.ToString(value));
                    break;

                case ParameterType.Vector3 when field is Vector3Field vector3Field:
                    vector3Field.SetValueWithoutNotify(value.ToVector3());
                    break;
                case ParameterType.Quaternion when field is Vector3Field quatField:
                    quatField.SetValueWithoutNotify(value.ToQuaternion().eulerAngles);
                    break;

                case ParameterType.ByteArray when field is TextField byteArrayField:
                    byteArrayField.SetValueWithoutNotify(value is byte[] bytes ? Converter.ToBase64(bytes) : string.Empty);
                    break;
            }
        }

        /// <summary>
        /// Crée un champ UI pour un <see cref="ParameterEntry"/> (utilisé par AvatarParametersEditor).
        /// Retourne le champ créé et associe le callback via le userData de l'élément.
        /// </summary>
        public static VisualElement CreateFieldForParameterEntry(
            ParameterEntry parameter, int index,
            Action<ParameterEntry, object> onValueChanged)
        {
            var type = parameter.type;
            object currentValue = null;
            if (!string.IsNullOrEmpty(parameter.defaultValue))
                currentValue = type switch
                {
                    ParameterType.Bool => parameter.GetDefaultValue<bool>(),
                    ParameterType.Byte => parameter.GetDefaultValue<byte>(),
                    ParameterType.Short => parameter.GetDefaultValue<short>(),
                    ParameterType.UShort => parameter.GetDefaultValue<ushort>(),
                    ParameterType.Int => parameter.GetDefaultValue<int>(),
                    ParameterType.UInt => parameter.GetDefaultValue<uint>(),
                    ParameterType.Long => parameter.GetDefaultValue<long>(),
                    ParameterType.ULong => parameter.GetDefaultValue<ulong>(),
                    ParameterType.Float => parameter.GetDefaultValue<float>(),
                    ParameterType.Double => parameter.GetDefaultValue<double>(),
                    ParameterType.String => parameter.GetDefaultValue<string>(),
                    ParameterType.Vector3 => parameter.GetDefaultValue<Vector3>(),
                    ParameterType.Quaternion => parameter.GetDefaultValue<Quaternion>(),
                    _ => null
                };
            var label = parameter.name ?? "Parameter";

            if (type == ParameterType.ByteArray)
            {
                var textField = new TextField(label) { multiline = true, style = { flexGrow = 1 } };
                textField.SetValueWithoutNotify(currentValue is byte[] bytes ? Converter.ToBase64(bytes) : string.Empty);
                textField.userData = index;
                textField.RegisterValueChangedCallback(evt =>
                {
                    var decoded = Converter.FromBase64(evt.newValue);
                    if (decoded != null)
                        onValueChanged(parameter, decoded);
                });
                return textField;
            }

            var field = CreateFieldByType(type, label, currentValue, true);
            if (field == null)
                return new Label($"{label}: Type {type} non supporté");

            field.userData = index;
            RegisterCallbackForParameterEntry(field, type, parameter, onValueChanged);
            return field;
        }

        private static void RegisterCallbackForParameterEntry(
            VisualElement field, ParameterType type, ParameterEntry parameter,
            Action<ParameterEntry, object> onValueChanged)
        {
            switch (type)
            {
                case ParameterType.Bool when field is Toggle toggle:
                    toggle.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, evt.newValue));
                    break;

                case ParameterType.Byte when field is IntegerField byteField:
                    byteField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, (byte)Math.Clamp(evt.newValue, byte.MinValue, byte.MaxValue)));
                    break;

                case ParameterType.Short when field is IntegerField shortField:
                    shortField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, (short)Math.Clamp(evt.newValue, short.MinValue, short.MaxValue)));
                    break;

                case ParameterType.UShort when field is IntegerField ushortField:
                    ushortField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, (ushort)Math.Clamp(evt.newValue, ushort.MinValue, ushort.MaxValue)));
                    break;

                case ParameterType.Int when field is IntegerField intField:
                    intField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, (int)Math.Clamp(evt.newValue, int.MinValue, int.MaxValue)));
                    break;

                case ParameterType.UInt when field is IntegerField uintField:
                    uintField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, (uint)Math.Clamp(evt.newValue, uint.MinValue, uint.MaxValue)));
                    break;

                case ParameterType.Long when field is IntegerField longField:
                    longField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, evt.newValue));
                    break;

                case ParameterType.ULong when field is IntegerField ulongField:
                    ulongField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, (ulong)Math.Max(0, evt.newValue)));
                    break;

                case ParameterType.Float when field is FloatField floatField:
                    floatField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, (float)evt.newValue));
                    break;

                case ParameterType.Double when field is DoubleField doubleField:
                    doubleField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, evt.newValue));
                    break;

                case ParameterType.String when field is TextField textField:
                    textField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, evt.newValue ?? string.Empty));
                    break;

                case ParameterType.Vector3 when field is Vector3Field vector3Field:
                    vector3Field.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, evt.newValue));
                    break;

                case ParameterType.Quaternion when field is Vector3Field quatField:
                    quatField.RegisterValueChangedCallback(evt =>
                        onValueChanged(parameter, Quaternion.Euler(evt.newValue)));
                    break;

                case ParameterType.ByteArray:
                    // Géré séparément dans CreateFieldForParameterEntry
                    break;
            }
        }

        /// <summary>
        /// Met à jour la valeur affichée d'un champ pour un ParameterEntry sans déclencher de callback.
        /// </summary>
        public static void UpdateFieldValue(VisualElement field, ParameterEntry parameter)
        {
            var type = parameter.type;
            var value = !string.IsNullOrEmpty(parameter.defaultValue)
                ? parameter.GetDefaultValue<object>()
                : null;
            UpdateFieldValue(field, type, value);
        }

        // -- Champs individuels ------------------------------------------------

        private static VisualElement CreateBoolField(string label, object currentValue, bool editable)
        {
            var toggle = new Toggle(label) { value = currentValue is bool b ? b : false };
            if (!editable)
                toggle.SetEnabled(false);
            return toggle;
        }

        private static VisualElement CreateIntegerField(string label, ParameterType type, object currentValue, bool editable)
        {
            var field = new IntegerField(label);
            field.value = (int)ConvertValue(type, currentValue);
            if (!editable)
                field.SetEnabled(false);

            // Clamping pour éviter les valeurs hors plage
            RegisterClampCallback(field, type);
            return field;
        }

        private static void RegisterClampCallback(IntegerField field, ParameterType type)
        {
            field.RegisterValueChangedCallback(evt =>
            {
                var clamped = Clamp(evt.newValue, type);
                if (clamped != evt.newValue)
                    field.SetValueWithoutNotify((int)clamped);
            });
        }

        private static long Clamp(long value, ParameterType type)
        {
            return type switch
            {
                ParameterType.Byte   => Math.Clamp(value, byte.MinValue, byte.MaxValue),
                ParameterType.Short  => Math.Clamp(value, short.MinValue, short.MaxValue),
                ParameterType.UShort => Math.Clamp(value, ushort.MinValue, ushort.MaxValue),
                ParameterType.Int    => Math.Clamp(value, int.MinValue, int.MaxValue),
                ParameterType.UInt   => Math.Clamp(value, uint.MinValue, uint.MaxValue),
                ParameterType.Long   => value,
                ParameterType.ULong  => Math.Max(0, value),
                _                    => value
            };
        }

        private static long ConvertValue(ParameterType type, object currentValue)
        {
            return type switch
            {
                ParameterType.Byte   => currentValue is byte b ? b : 0,
                ParameterType.Short  => currentValue is short s ? s : 0,
                ParameterType.UShort => currentValue is ushort us ? us : 0,
                ParameterType.Int    => currentValue is int i ? i : 0,
                ParameterType.UInt   => currentValue is uint ui ? (long)ui : 0,
                ParameterType.Long   => currentValue is long l ? l : 0,
                ParameterType.ULong  => currentValue is ulong ul ? (long)ul : 0,
                _                    => 0
            };
        }

        private static VisualElement CreateFloatField(string label, object currentValue, bool editable)
        {
            var field = new FloatField(label) { value = currentValue is float f ? f : 0f };
            if (!editable)
                field.SetEnabled(false);
            return field;
        }

        private static VisualElement CreateDoubleField(string label, object currentValue, bool editable)
        {
            var field = new DoubleField(label) { value = currentValue is double d ? d : 0.0 };
            if (!editable)
                field.SetEnabled(false);
            return field;
        }

        private static VisualElement CreateStringField(string label, object currentValue, bool editable)
        {
            var field = new TextField(label) { value = Converter.ToString(currentValue), tooltip = "Text value" };
            if (!editable)
                field.SetEnabled(false);
            return field;
        }

        private static VisualElement CreateVector3Field(string label, object currentValue, bool editable)
        {
            var field = new Vector3Field(label) { value = currentValue is Vector3 v ? v : Vector3.zero };
            if (!editable)
                field.SetEnabled(false);
            return field;
        }

        private static VisualElement CreateQuaternionField(string label, object currentValue, bool editable)
        {
            var field = new Vector3Field(label + " (Euler)")
            {
                value = currentValue is Quaternion q ? q.eulerAngles : Vector3.zero
            };
            if (!editable)
                field.SetEnabled(false);
            return field;
        }

        private static VisualElement CreateByteArrayField(string label, object currentValue, bool editable)
        {
            var field = new TextField(label) { multiline = true, style = { flexGrow = 1 } };
            field.SetValueWithoutNotify(currentValue is byte[] bytes ? Converter.ToBase64(bytes) : string.Empty);
            if (!editable)
                field.SetEnabled(false);
            return field;
        }

        private static VisualElement CreateFallbackField(string label, ParameterType type)
        {
            return new Label($"{label}: Type {type} non supporté");
        }

        // -- Helpers de clamp --------------------------------------------------

        private static byte ClampByte(long value) => (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
        private static short ClampShort(long value) => (short)Math.Clamp(value, short.MinValue, short.MaxValue);
        private static ushort ClampUShort(long value) => (ushort)Math.Clamp(value, ushort.MinValue, ushort.MaxValue);
        private static int ClampInt(long value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);
        private static uint ClampUInt(long value) => (uint)Math.Clamp(value, uint.MinValue, uint.MaxValue);
        private static long ClampLong(long value) => value;
        private static ulong ClampULong(long value) => (ulong)Math.Max(0, value);
    }
}