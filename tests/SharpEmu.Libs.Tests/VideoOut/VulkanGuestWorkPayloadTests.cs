// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.Libs.Gpu;
using SharpEmu.Libs.VideoOut;
using Xunit;

namespace SharpEmu.Libs.Tests.VideoOut;

public sealed class VulkanGuestWorkPayloadTests
{
    [Fact]
    public void TexturePayloadCountsSharedSnapshotsOnce()
    {
        var sharedPixels = new byte[64];
        var distinctPixels = new byte[32];
        var textures = new GuestDrawTexture[]
        {
            CreateTexture(0x1000, sharedPixels),
            CreateTexture(0x1000, sharedPixels),
            CreateTexture(0x2000, distinctPixels),
            CreateTexture(0x3000, []),
        };

        Assert.Equal(
            96UL,
            VulkanVideoPresenter.GetTexturePayloadBytes(textures));
    }

    private static GuestDrawTexture CreateTexture(ulong address, byte[] pixels) =>
        new(
            address,
            Width: 4,
            Height: 4,
            Format: 10,
            NumberType: 0,
            pixels,
            IsFallback: false,
            IsStorage: false);
}
