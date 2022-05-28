using System;
using System.Runtime.InteropServices;
using Godot.NativeInterop;

namespace Godot.Bridge
{
    internal static class CSharpInstanceBridge
    {
        [UnmanagedCallersOnly]
        internal static unsafe godot_bool Call(IntPtr godotObjectGCHandle, godot_string_name* method,
            godot_variant** args, int argCount, godot_variant_call_error* refCallError, godot_variant* ret)
        {
            try
            {
                var godotObject = (Object)GCHandle.FromIntPtr(godotObjectGCHandle).Target;

                if (godotObject == null)
                {
                    *ret = default;
                    (*refCallError).Error = godot_variant_call_error_error.GODOT_CALL_ERROR_CALL_ERROR_INSTANCE_IS_NULL;
                    return godot_bool.False;
                }

                bool methodInvoked = godotObject.InvokeGodotClassMethod(CustomUnsafe.AsRef(method),
                    new NativeVariantPtrArgs(args),
                    argCount, out godot_variant retValue);

                if (!methodInvoked)
                {
                    *ret = default;
                    // This is important, as it tells Object::call that no method was called.
                    // Otherwise, it would prevent Object::call from calling native methods.
                    (*refCallError).Error = godot_variant_call_error_error.GODOT_CALL_ERROR_CALL_ERROR_INVALID_METHOD;
                    return godot_bool.False;
                }

                *ret = retValue;
                return godot_bool.True;
            }
            catch (Exception e)
            {
                ExceptionUtils.LogException(e);
                *ret = default;
                return godot_bool.False;
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe godot_bool Set(IntPtr godotObjectGCHandle, godot_string_name* name, godot_variant* value)
        {
            try
            {
                var godotObject = (Object)GCHandle.FromIntPtr(godotObjectGCHandle).Target;

                if (godotObject == null)
                    throw new InvalidOperationException();

                if (godotObject.SetGodotClassPropertyValue(CustomUnsafe.AsRef(name), CustomUnsafe.AsRef(value)))
                {
                    return godot_bool.True;
                }

                var nameManaged = StringName.CreateTakingOwnershipOfDisposableValue(
                    NativeFuncs.godotsharp_string_name_new_copy(CustomUnsafe.AsRef(name)));

                object valueManaged = Marshaling.ConvertVariantToManagedObject(CustomUnsafe.AsRef(value));

                return godotObject._Set(nameManaged, valueManaged).ToGodotBool();
            }
            catch (Exception e)
            {
                ExceptionUtils.LogException(e);
                return godot_bool.False;
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe godot_bool Get(IntPtr godotObjectGCHandle, godot_string_name* name,
            godot_variant* outRet)
        {
            try
            {
                var godotObject = (Object)GCHandle.FromIntPtr(godotObjectGCHandle).Target;

                if (godotObject == null)
                    throw new InvalidOperationException();

                if (godotObject.GetGodotClassPropertyValue(CustomUnsafe.AsRef(name), out godot_variant outRetValue))
                {
                    *outRet = outRetValue;
                    return godot_bool.True;
                }

                var nameManaged = StringName.CreateTakingOwnershipOfDisposableValue(
                    NativeFuncs.godotsharp_string_name_new_copy(CustomUnsafe.AsRef(name)));

                object ret = godotObject._Get(nameManaged);

                if (ret == null)
                {
                    *outRet = default;
                    return godot_bool.False;
                }

                *outRet = Marshaling.ConvertManagedObjectToVariant(ret);
                return godot_bool.True;
            }
            catch (Exception e)
            {
                ExceptionUtils.LogException(e);
                *outRet = default;
                return godot_bool.False;
            }
        }

        [UnmanagedCallersOnly]
        internal static void CallDispose(IntPtr godotObjectGCHandle, godot_bool okIfNull)
        {
            try
            {
                var godotObject = (Object)GCHandle.FromIntPtr(godotObjectGCHandle).Target;

                if (okIfNull.ToBool())
                    godotObject?.Dispose();
                else
                    godotObject!.Dispose();
            }
            catch (Exception e)
            {
                ExceptionUtils.LogException(e);
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe void CallToString(IntPtr godotObjectGCHandle, godot_string* outRes, godot_bool* outValid)
        {
            try
            {
                var self = (Object)GCHandle.FromIntPtr(godotObjectGCHandle).Target;

                if (self == null)
                {
                    *outRes = default;
                    *outValid = godot_bool.False;
                    return;
                }

                var resultStr = self.ToString();

                if (resultStr == null)
                {
                    *outRes = default;
                    *outValid = godot_bool.False;
                    return;
                }

                *outRes = Marshaling.ConvertStringToNative(resultStr);
                *outValid = godot_bool.True;
            }
            catch (Exception e)
            {
                ExceptionUtils.LogException(e);
                *outRes = default;
                *outValid = godot_bool.False;
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe godot_bool HasMethodUnknownParams(IntPtr godotObjectGCHandle, godot_string_name* method)
        {
            try
            {
                var godotObject = (Object)GCHandle.FromIntPtr(godotObjectGCHandle).Target;

                if (godotObject == null)
                    return godot_bool.False;

                return godotObject.HasGodotClassMethod(CustomUnsafe.AsRef(method)).ToGodotBool();
            }
            catch (Exception e)
            {
                ExceptionUtils.LogException(e);
                return godot_bool.False;
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe void SerializeState(
            IntPtr godotObjectGCHandle,
            godot_dictionary* propertiesState,
            godot_dictionary* signalEventsState
        )
        {
            try
            {
                var godotObject = (Object)GCHandle.FromIntPtr(godotObjectGCHandle).Target;

                if (godotObject == null)
                    return;

                // Call OnBeforeSerialize

                // ReSharper disable once SuspiciousTypeConversion.Global
                if (godotObject is ISerializationListener serializationListener)
                    serializationListener.OnBeforeSerialize();

                // Save instance state

                var info = new GodotSerializationInfo(
                    Collections.Dictionary<StringName, object>.CreateTakingOwnershipOfDisposableValue(
                        NativeFuncs.godotsharp_dictionary_new_copy(*propertiesState)),
                    Collections.Dictionary<StringName, Collections.Array>.CreateTakingOwnershipOfDisposableValue(
                        NativeFuncs.godotsharp_dictionary_new_copy(*signalEventsState)));

                godotObject.SaveGodotObjectData(info);
            }
            catch (Exception e)
            {
                ExceptionUtils.LogException(e);
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe void DeserializeState(
            IntPtr godotObjectGCHandle,
            godot_dictionary* propertiesState,
            godot_dictionary* signalEventsState
        )
        {
            try
            {
                var godotObject = (Object)GCHandle.FromIntPtr(godotObjectGCHandle).Target;

                if (godotObject == null)
                    return;

                // Restore instance state

                var info = new GodotSerializationInfo(
                    Collections.Dictionary<StringName, object>.CreateTakingOwnershipOfDisposableValue(
                        NativeFuncs.godotsharp_dictionary_new_copy(*propertiesState)),
                    Collections.Dictionary<StringName, Collections.Array>.CreateTakingOwnershipOfDisposableValue(
                        NativeFuncs.godotsharp_dictionary_new_copy(*signalEventsState)));

                godotObject.RestoreGodotObjectData(info);

                // Call OnAfterDeserialize

                // ReSharper disable once SuspiciousTypeConversion.Global
                if (godotObject is ISerializationListener serializationListener)
                    serializationListener.OnAfterDeserialize();
            }
            catch (Exception e)
            {
                ExceptionUtils.LogException(e);
            }
        }
    }
}