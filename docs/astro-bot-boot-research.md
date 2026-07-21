# Astro Bot (PPSA21564) boot / menu research

Last updated: 2026-07-21. Branch tip context: `fix/graphics-textures-v2`.

## Goal

Visible, usable boot/menu (not black, not teal static noise), with flips advancing and FPS better than ~0.2–0.4.

## Proven present path

- Default **CPU packed A2B10** present (`cpu_packed`, `force_gpu_packed=False`).
- Do **not** set `SHARPEMU_FORCE_GPU_PACKED_PRESENT=1` for interactive runs — GPU packed path produced teal static noise.
- Scoped wait: `WaitForGuestImageLastWrite` (not full-queue idle).
- Tip commits for present: `c2e57f8`, `af237f9`, `1128b3e`, `da64a2d`, deferred composite WIP `12e6304`.

## False positives (do not treat as success)

| Signal | Why it is wrong |
|--------|-----------------|
| Hash `0x8FC3AE4756B6366D` | Stable **teal noise** pattern (`uniqueR≈2`), not boot art |
| Gray `F05EB8` / LE `010101FF` | Historical bad A2B10 decode / blit |
| Flips advancing with flat `000000C0` | Composite runs but feeder is empty |

## Latest composite evidence (`boot-verify-composer8`)

| Metric | Value |
|--------|--------|
| Path | `cpu_packed`, `present_source=live` |
| `agc.deferred_composite` | Fires every flip |
| Flips | 8 |
| Scanout center | `000000C0` → BGRA `000030FF` |
| Dump | `uniqueR=2`, avgRGB≈52, maxChannel=211 |
| Feeder sample (`deferred_composite_src_pre`) | `sample_unique=1 sample_nonzero=0 center=0x0` |
| `vk.hdr_feeder_gpu_post` | Never logged (address mask bug; fixed after `12e6304`) |

## Causal chain

1. Present decode can be faithful while scanout content is still init teal / flat.
2. Deferred composite copies HDR feeder → scanout.
3. HDR feeder RT (`0x53D410000` / `0x53C2F0000`, `B10G11R11`) readback was empty.
4. Upstream: same guest address can hold filled `R16G16B16A16Sfloat` and empty `B10G11R11` variants (e.g. `0x53AA00000`). Alias scoring preferred empty exact-format variant → feeder draw sampled black inputs.

## Verify env

```
SHARPEMU_IGNORE_STACK_CHK=1
SHARPEMU_TRACE_GUEST_IMAGES=swapchain
SHARPEMU_SWAPCHAIN_DUMP_EVERY=5
# Do NOT set SHARPEMU_FORCE_GPU_PACKED_PRESENT
```

Eboot: `...\PPSA21564-app\eboot.bin`  
Exe: `artifacts\bin\Release\net10.0\win-x64\SharpEmu.exe` (rebuild after pull).

## Host HDR display setting

**No** — enabling Windows/monitor HDR does not feed the guest HDR feeder RT. Guest HDR here is an offscreen B10G11R11 render target inside the emulator, unrelated to host HDR output mode.

## Live WinX64 log note (`g:\message.txt`, 2026-07-21)

User ran build stamp `commit=28e37fa CANARY=PPSA21564-num7+6` (stale vs tip). Got to `main()`, splash, materials, DualSense, AutoCross, `RoomLoad_ATQT`, then `tbb_thead` execute faults + `Invalid Program: UnmanagedCallersOnly` fatal. **`flips=0`**. Rebuild from current tip before judging present/composite work.

## Success criteria

Non-flat scanout with `uniqueR` ≫ 2 (real composite had ~53 unique packed values), recognizable boot/menu art, sustained flips, no teal noise hash `0x8FC3AE…`.
