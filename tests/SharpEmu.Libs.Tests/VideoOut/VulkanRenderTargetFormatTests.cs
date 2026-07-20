// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.Libs.VideoOut;
using SharpEmu.ShaderCompiler;
using Silk.NET.Vulkan;
using Xunit;

namespace SharpEmu.Libs.Tests.VideoOut;

public sealed class VulkanRenderTargetFormatTests
{
    [Fact]
    public void Rgba16FloatRenderTargetUsesMatchingVulkanFormat()
    {
        Assert.True(
            VulkanVideoPresenter.TryDecodeRenderTargetFormat(
                dataFormat: 12,
                numberType: 7,
                out var decoded));
        Assert.Equal(Format.R16G16B16A16Sfloat, decoded.Format);
        Assert.Equal(Gen5PixelOutputKind.Float, decoded.OutputKind);
    }

    [Fact]
    public void RenderedGuestImagesArePublishedToComputeConsumers()
    {
        Assert.True(
            VulkanVideoPresenter.GuestImageShaderReadStages.HasFlag(
                PipelineStageFlags.ComputeShaderBit));
        Assert.True(
            VulkanVideoPresenter.GuestImageShaderReadStages.HasFlag(
                PipelineStageFlags.FragmentShaderBit));
    }
}
