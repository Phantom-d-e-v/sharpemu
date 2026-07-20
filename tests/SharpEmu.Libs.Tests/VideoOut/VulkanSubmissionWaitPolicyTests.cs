// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.Libs.VideoOut;
using Xunit;

namespace SharpEmu.Libs.Tests.VideoOut;

public sealed class VulkanSubmissionWaitPolicyTests
{
    [Fact]
    public void DefaultSubmissionCapacityPolicyDoesNotProbeFence()
    {
        Assert.False(VulkanVideoPresenter.ShouldProbeGuestSubmissionCapacity(
            waitConfigured: false));
    }

    [Fact]
    public void ExplicitSubmissionCapacityPolicyProbesFence()
    {
        Assert.True(VulkanVideoPresenter.ShouldProbeGuestSubmissionCapacity(
            waitConfigured: true));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(4, 3)]
    public void FullDrainWaitsForNewestQueueSubmission(
        int pendingSubmissionCount,
        int expectedFenceIndex)
    {
        Assert.Equal(
            expectedFenceIndex,
            VulkanVideoPresenter.GetGuestSubmissionDrainFenceIndex(
                pendingSubmissionCount));
    }
}
