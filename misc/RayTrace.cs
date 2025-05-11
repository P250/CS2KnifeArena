using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace CS2KnifeArena.misc;

public class RayTrace
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate bool TraceShapeDelegate(
        nint GameTraceManager,
        nint vecStart,
        nint vecEnd,
        nint skip,
        ulong mask,
        byte a6,
        GameTrace* pGameTrace
    );

    private static TraceShapeDelegate? _traceShape;

    static public CBeam? DrawLaserBetween(BasePlugin parent, Vector startPos, Vector endPos, Color? color = null, float duration = 1, float width = 2)
    {
        CBeam? beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null) return null;

        beam.Render = color ?? Color.Red;
        beam.Width = width;

        beam.Teleport(startPos, new QAngle(), new Vector());
        beam.EndPos.Change(endPos);
        beam.DispatchSpawn();
        parent.AddTimer(duration, beam.Remove, TimerFlags.STOP_ON_MAPCHANGE);
        return beam;
    }

    private static nint TraceFunc = NativeAPI.FindSignature(Addresses.ServerPath, new(GameData.GetSignature("TraceFunc")));

    private static nint GameTraceManager = NativeAPI.FindSignature(Addresses.ServerPath, new(GameData.GetSignature("GameTraceManager")));

    public static unsafe Vector? TraceShape(BasePlugin parent, Vector _origin, QAngle _viewangles, bool drawResult = false, bool fromPlayer = false)
    {
        var _forward = new Vector();

        // Get forward vector from view angles
        NativeAPI.AngleVectors(_viewangles.Handle, _forward.Handle, 0, 0);
        var _endOrigin = new Vector(_origin.X + _forward.X * 8192, _origin.Y + _forward.Y * 8192, _origin.Z + _forward.Z * 8192);

        var d = 50;

        if (fromPlayer)
        {
            _origin.X += _forward.X * d;
            _origin.Y += _forward.Y * d;
            _origin.Z += _forward.Z * d + 64;
        }

        if (_origin == null)
        {
            throw new ArgumentNullException(nameof(_origin));
        }
        if (_endOrigin == null)
        {
            throw new ArgumentNullException(nameof(_endOrigin));
        }

        return TraceShape(parent, _origin, _endOrigin, drawResult) ?? throw new InvalidOperationException("TraceShape returned null.");
    }

    public static unsafe Vector? TraceShape(BasePlugin parent, Vector? _origin, Vector _endOrigin, bool drawResult = false)
    {
        var _gameTraceManagerAddress = Address.GetAbsoluteAddress(GameTraceManager, 3, 7);

        _traceShape = Marshal.GetDelegateForFunctionPointer<TraceShapeDelegate>(TraceFunc);

        // Console.WriteLine($"==== TraceFunc {TraceFunc} | GameTraceManager {GameTraceManager} | _gameTraceManagerAddress {_gameTraceManagerAddress} | _traceShape {_traceShape}");



        var _trace = stackalloc GameTrace[1];

        ulong mask = 0x1C1003;
        // var mask = 0xFFFFFFFF;
        if (_traceShape == null)
        {
            throw new InvalidOperationException("TraceShape delegate is not initialized.");
        }

        if (_gameTraceManagerAddress == IntPtr.Zero)
        {
            throw new InvalidOperationException("GameTraceManager address is null.");
        }

        if (_origin == null)
        {
            throw new ArgumentNullException(nameof(_origin));
        }

        var result = _traceShape(*(nint*)_gameTraceManagerAddress, _origin.Handle, _endOrigin.Handle, 0, mask, 4, _trace);

        //Console.WriteLine($"RESULT {result}");

        //Console.WriteLine($"StartPos: {_trace->StartPos}");
        //Console.WriteLine($"EndPos: {_trace->EndPos}");
        //Console.WriteLine($"HitEntity: {(uint)_trace->HitEntity}");
        //Console.WriteLine($"Fraction: {_trace->Fraction}");
        //Console.WriteLine($"AllSolid: {_trace->AllSolid}");
        //Console.WriteLine($"ViewAngles: {_viewangles}");

        var endPos = new Vector(_trace->EndPos.X, _trace->EndPos.Y, _trace->EndPos.Z);

        if (drawResult)
        {
            Color color = Color.FromName("Green");
            if (result)
            {
                color = Color.FromName("Red");
            }

            DrawLaserBetween(parent, _origin, endPos, color, 5);
        }

        if (result)
        {
            return endPos;
        }

        return null;
    }
}

internal static class Address
{
    static unsafe public nint GetAbsoluteAddress(nint addr, nint offset, int size)
    {
        if (addr == IntPtr.Zero)
        {
            throw new Exception("Failed to find RayTrace signature.");
        }

        int code = *(int*)(addr + offset);
        return addr + code + size;
    }

    static public nint GetCallAddress(nint a)
    {
        return GetAbsoluteAddress(a, 1, 5);
    }
}

[StructLayout(LayoutKind.Explicit, Size = 0x35)]
public unsafe struct Ray
{
    [FieldOffset(0)] public Vector3 Start;
    [FieldOffset(0xC)] public Vector3 End;
    [FieldOffset(0x18)] public Vector3 Mins;
    [FieldOffset(0x24)] public Vector3 Maxs;
    [FieldOffset(0x34)] public byte UnkType;
}

[StructLayout(LayoutKind.Explicit, Size = 0x44)]
public unsafe struct TraceHitboxData
{
    [FieldOffset(0x38)] public int HitGroup;
    [FieldOffset(0x40)] public int HitboxId;
}

[StructLayout(LayoutKind.Explicit, Size = 0xB8)]
public unsafe struct GameTrace
{
    [FieldOffset(0)] public void* Surface;
    [FieldOffset(0x8)] public void* HitEntity;
    [FieldOffset(0x10)] public TraceHitboxData* HitboxData;
    [FieldOffset(0x50)] public uint Contents;
    [FieldOffset(0x78)] public Vector3 StartPos;
    [FieldOffset(0x84)] public Vector3 EndPos;
    [FieldOffset(0x90)] public Vector3 Normal;
    [FieldOffset(0x9C)] public Vector3 Position;
    [FieldOffset(0xAC)] public float Fraction;
    [FieldOffset(0xB6)] public bool AllSolid;
}

[StructLayout(LayoutKind.Explicit, Size = 0x3a)]
public unsafe struct TraceFilter
{
    [FieldOffset(0)] public void* Vtable;
    [FieldOffset(0x8)] public ulong Mask;
    [FieldOffset(0x20)] public fixed uint SkipHandles[4];
    [FieldOffset(0x30)] public fixed ushort arrCollisions[2];
    [FieldOffset(0x34)] public uint Unk1;
    [FieldOffset(0x38)] public byte Unk2;
    [FieldOffset(0x39)] public byte Unk3;
}

public unsafe struct TraceFilterV2
{
    public ulong Mask;
    public fixed ulong V1[2];
    public fixed uint SkipHandles[4];
    public fixed ushort arrCollisions[2];
    public short V2;
    public byte V3;
    public byte V4;
    public byte V5;
}