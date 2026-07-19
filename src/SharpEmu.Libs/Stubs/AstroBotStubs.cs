// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;

namespace SharpEmu.Libs.Stubs;

// Astro Bot (PPSA21564) — success-returning stubs for NIDs the game calls
// during boot that the emulator does not otherwise implement. These are
// non-fatal helper / init calls (audio-scene register/submit, buffer
// setup, param lookups). Returning ORBIS_GEN2_OK for them lets boot proceed
// instead of the guest busy-waiting/polling on a NOT_FOUND error.
//
// NID strings are the literal values the guest binary imports; the cosmetic
// ExportName is unknown for some so it is left empty (no SHEM006, since the
// analyzer only warns when ExportName is non-empty and missing from the
// catalog). Behavior: return success; for calls that hand us an output
// struct pointer we zero a small, bounded region so the guest sees a
// sane (empty) result rather than garbage.
public static class AstroBotStubs
{
    private static int Ok(CpuContext ctx)
    {
        ctx[CpuRegister.Rax] = 0;
        return 0;
    }

    // Zero a bounded guest region if the address looks valid (never throw).
    private static void SafeZero(CpuContext ctx, ulong address, int bytes)
    {
        if (address == 0 || bytes <= 0 || bytes > 0x1000)
            return;
        for (var i = 0; i < bytes; i += 8)
        {
            var n = System.Math.Min(8, bytes - i);
            switch (n)
            {
                case 8:
                    _ = ctx.TryWriteUInt64(address + (ulong)i, 0);
                    break;
                case 4:
                    _ = ctx.TryWriteUInt32(address + (ulong)i, 0);
                    break;
                default:
                    _ = ctx.TryWriteUInt16(address + (ulong)i, 0);
                    break;
            }
        }
    }

    // dolOmWH+huQ — paired with fd5Bp5tGTgo (~30 calls before freeze).
    // rdi=stack rsi=bufA rdx=bufB rcx=struct r8=bufC r9=size.
    [SysAbiExport(
        Nid = "dolOmWH+huQ",
        ExportName = "",
        Target = Generation.Gen5,
        LibraryName = "libSceAudio3d")]
    public static int AstroDolOmWH(CpuContext ctx)
    {
        SafeZero(ctx, ctx[CpuRegister.Rcx], 0x80);
        return Ok(ctx);
    }

    // fd5Bp5tGTgo — pair submit. rdi=struct rsi=bufA rdx=bufB rcx=0 r8=bufC r9=size.
    [SysAbiExport(
        Nid = "fd5Bp5tGTgo",
        ExportName = "",
        Target = Generation.Gen5,
        LibraryName = "libSceAudio3d")]
    public static int AstroFd5Bp5tGTgo(CpuContext ctx)
    {
        SafeZero(ctx, ctx[CpuRegister.Rdi], 0x80);
        return Ok(ctx);
    }

    // amuBfI-AQc4 — buffer copy-like. rdi=dst rsi=0x800000 rdx=src rcx=0 r8=size(neg) r9=0x940.
    [SysAbiExport(
        Nid = "amuBfI-AQc4",
        ExportName = "",
        Target = Generation.Gen5,
        LibraryName = "libSceAudio3d")]
    public static int AstroAmuBfI(CpuContext ctx)
    {
        return Ok(ctx);
    }

    // Sygnk9dr5WQ — buffer fill/init. rdi=struct rsi=buf rdx=0x1FFF rcx=0 r8=0x32 r9=0.
    [SysAbiExport(
        Nid = "Sygnk9dr5WQ",
        ExportName = "",
        Target = Generation.Gen5,
        LibraryName = "libSceAudio3d")]
    public static int AstroSygnk9dr5WQ(CpuContext ctx)
    {
        SafeZero(ctx, ctx[CpuRegister.Rsi], 0x1FFF);
        return Ok(ctx);
    }

    // ZIXln2K3XMk — query returning pointer. rdi=struct rsi=stack rdx=0x80...4D3 rcx=struct r8=struct r9=-1.
    [SysAbiExport(
        Nid = "ZIXln2K3XMk",
        ExportName = "",
        Target = Generation.Gen5,
        LibraryName = "libSceAudio3d")]
    public static int AstroZIXln2K3XMk(CpuContext ctx)
    {
        SafeZero(ctx, ctx[CpuRegister.Rdi], 0x100);
        return Ok(ctx);
    }

    // 00oCq0RwSAY — array op. rdi=0x...180 rsi=0x80...210 rdx=0x...BD40 rcx=0x80...5E0 r8=-7 r9=7.
    [SysAbiExport(
        Nid = "00oCq0RwSAY",
        ExportName = "",
        Target = Generation.Gen5,
        LibraryName = "libSceAudio3d")]
    public static int Astro00oCq0RwSAY(CpuContext ctx)
    {
        SafeZero(ctx, ctx[CpuRegister.Rdx], 0x100);
        return Ok(ctx);
    }

    // n1-v6FgU7MQ — struct/string copy. rdi=0x...558 rsi=0x...570 rdx=0xF rcx=0xB0 r8=-15 r9=0xF.
    [SysAbiExport(
        Nid = "n1-v6FgU7MQ",
        ExportName = "",
        Target = Generation.Gen5,
        LibraryName = "libSceAudio3d")]
    public static int AstroN1v6FgU7MQ(CpuContext ctx)
    {
        SafeZero(ctx, ctx[CpuRegister.Rdi], 0x100);
        return Ok(ctx);
    }

    // BfBDZGbti7A — get string/param. rdi=buf rsi=0x5F4 rdx=0x4D rcx=0 r8=0x3B r9=0x87400000.
    // Returned ORBIS_GEN2_ERROR_INVALID_ARGUMENT before; produce an empty result.
    [SysAbiExport(
        Nid = "BfBDZGbti7A",
        ExportName = "",
        Target = Generation.Gen5,
        LibraryName = "libSceSystemService")]
    public static int AstroBfBDZGbti7A(CpuContext ctx)
    {
        SafeZero(ctx, ctx[CpuRegister.Rdi], 0x100);
        return Ok(ctx);
    }
}
