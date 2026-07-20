// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

namespace SharpEmu.ShaderCompiler.Vulkan;

public static class SpirvFixedShaders
{
    public static byte[] CreateFullscreenVertex(uint attributeCount)
    {
        var module = new SpirvModuleBuilder();
        module.AddCapability(SpirvCapability.Shader);

        var voidType = module.TypeVoid();
        var boolType = module.TypeBool();
        var uintType = module.TypeInt(32, signed: false);
        var floatType = module.TypeFloat(32);
        var vec4Type = module.TypeVector(floatType, 4);
        var inputUintPointer = module.TypePointer(SpirvStorageClass.Input, uintType);
        var outputVec4Pointer = module.TypePointer(SpirvStorageClass.Output, vec4Type);

        var vertexIndex = module.AddGlobalVariable(inputUintPointer, SpirvStorageClass.Input);
        module.AddName(vertexIndex, "vertexIndex");
        module.AddDecoration(
            vertexIndex,
            SpirvDecoration.BuiltIn,
            (uint)SpirvBuiltIn.VertexIndex);

        var position = module.AddGlobalVariable(outputVec4Pointer, SpirvStorageClass.Output);
        module.AddName(position, "position");
        module.AddDecoration(position, SpirvDecoration.BuiltIn, (uint)SpirvBuiltIn.Position);

        var attributes = new uint[attributeCount];
        for (uint index = 0; index < attributeCount; index++)
        {
            attributes[index] =
                module.AddGlobalVariable(outputVec4Pointer, SpirvStorageClass.Output);
            module.AddName(attributes[index], $"attr{index}");
            module.AddDecoration(attributes[index], SpirvDecoration.Location, index);
            module.AddDecoration(attributes[index], SpirvDecoration.NoPerspective);
        }

        var functionType = module.TypeFunction(voidType);
        var main = module.BeginFunction(voidType, functionType);
        module.AddName(main, "main");
        module.AddLabel();

        var indexValue = module.AddInstruction(SpirvOp.Load, uintType, vertexIndex);
        var one = module.Constant(uintType, 1);
        var two = module.Constant(uintType, 2);
        var shifted = module.AddInstruction(SpirvOp.ShiftLeftLogical, uintType, indexValue, one);
        var xBits = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, shifted, two);
        var yBits = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, indexValue, two);
        var x = module.AddInstruction(SpirvOp.ConvertUToF, floatType, xBits);
        var y = module.AddInstruction(SpirvOp.ConvertUToF, floatType, yBits);
        var zero = module.ConstantFloat(floatType, 0f);
        var oneFloat = module.ConstantFloat(floatType, 1f);
        var twoFloat = module.ConstantFloat(floatType, 2f);
        var xPosition = module.AddInstruction(SpirvOp.FMul, floatType, x, twoFloat);
        xPosition = module.AddInstruction(SpirvOp.FSub, floatType, xPosition, oneFloat);
        var yPosition = module.AddInstruction(SpirvOp.FMul, floatType, y, twoFloat);
        yPosition = module.AddInstruction(SpirvOp.FSub, floatType, yPosition, oneFloat);
        var positionValue = module.AddInstruction(
            SpirvOp.CompositeConstruct,
            vec4Type,
            xPosition,
            yPosition,
            zero,
            oneFloat);
        module.AddStatement(SpirvOp.Store, position, positionValue);

        var attributeValue = module.AddInstruction(
            SpirvOp.CompositeConstruct,
            vec4Type,
            x,
            y,
            zero,
            oneFloat);
        foreach (var attribute in attributes)
        {
            module.AddStatement(SpirvOp.Store, attribute, attributeValue);
        }

        module.AddStatement(SpirvOp.Return);
        module.EndFunction();

        var interfaces = new uint[2 + attributes.Length];
        interfaces[0] = vertexIndex;
        interfaces[1] = position;
        attributes.CopyTo(interfaces, 2);
        module.AddEntryPoint(SpirvExecutionModel.Vertex, main, "main", interfaces);
        _ = boolType;
        return module.Build();
    }

    public static byte[] CreateCopyFragment()
    {
        var module = new SpirvModuleBuilder();
        module.AddCapability(SpirvCapability.Shader);

        var voidType = module.TypeVoid();
        var floatType = module.TypeFloat(32);
        var vec2Type = module.TypeVector(floatType, 2);
        var vec4Type = module.TypeVector(floatType, 4);
        var inputVec4Pointer = module.TypePointer(SpirvStorageClass.Input, vec4Type);
        var outputVec4Pointer = module.TypePointer(SpirvStorageClass.Output, vec4Type);
        var imageType = module.TypeImage(
            floatType,
            SpirvImageDim.Dim2D,
            depth: false,
            arrayed: false,
            multisampled: false,
            sampled: 1,
            SpirvImageFormat.Unknown);
        var sampledImageType = module.TypeSampledImage(imageType);
        var sampledImagePointer =
            module.TypePointer(SpirvStorageClass.UniformConstant, sampledImageType);

        var attribute = module.AddGlobalVariable(inputVec4Pointer, SpirvStorageClass.Input);
        module.AddName(attribute, "attr0");
        module.AddDecoration(attribute, SpirvDecoration.Location, 0);

        var texture = module.AddGlobalVariable(
            sampledImagePointer,
            SpirvStorageClass.UniformConstant);
        module.AddName(texture, "tex0");
        module.AddDecoration(texture, SpirvDecoration.DescriptorSet, 0);
        module.AddDecoration(texture, SpirvDecoration.Binding, 1);

        var output = module.AddGlobalVariable(outputVec4Pointer, SpirvStorageClass.Output);
        module.AddName(output, "outColor");
        module.AddDecoration(output, SpirvDecoration.Location, 0);

        var functionType = module.TypeFunction(voidType);
        var main = module.BeginFunction(voidType, functionType);
        module.AddName(main, "main");
        module.AddLabel();

        var attributeValue = module.AddInstruction(SpirvOp.Load, vec4Type, attribute);
        var coordinates = module.AddInstruction(
            SpirvOp.VectorShuffle,
            vec2Type,
            attributeValue,
            attributeValue,
            0,
            1);
        var sampledImage = module.AddInstruction(SpirvOp.Load, sampledImageType, texture);
        var lod = module.ConstantFloat(floatType, 0f);
        var color = module.AddInstruction(
            SpirvOp.ImageSampleExplicitLod,
            vec4Type,
            sampledImage,
            coordinates,
            2,
            lod);
        module.AddStatement(SpirvOp.Store, output, color);
        module.AddStatement(SpirvOp.Return);
        module.EndFunction();

        module.AddEntryPoint(
            SpirvExecutionModel.Fragment,
            main,
            "main",
            [attribute, texture, output]);
        module.AddExecutionMode(main, SpirvExecutionMode.OriginUpperLeft);
        return module.Build();
    }

    /// <summary>
    /// Present fragment for A2B10/A2R10 guest images whose CopyImageToBuffer
    /// bytes need big-endian interpretation (Astro Bot scanout). Samples as
    /// R32UI, byte-swaps the texel, then unpacks packed10 to float RGBA.
    /// </summary>
    public static byte[] CreatePacked10BePresentFragment(bool redInLeastSignificantBits)
    {
        var module = new SpirvModuleBuilder();
        module.AddCapability(SpirvCapability.Shader);

        var voidType = module.TypeVoid();
        var uintType = module.TypeInt(32, signed: false);
        var intType = module.TypeInt(32, signed: true);
        var floatType = module.TypeFloat(32);
        var vec2Type = module.TypeVector(floatType, 2);
        var ivec2Type = module.TypeVector(intType, 2);
        var uvec2Type = module.TypeVector(uintType, 2);
        var uvec4Type = module.TypeVector(uintType, 4);
        var vec4Type = module.TypeVector(floatType, 4);
        var inputVec4Pointer = module.TypePointer(SpirvStorageClass.Input, vec4Type);
        var outputVec4Pointer = module.TypePointer(SpirvStorageClass.Output, vec4Type);
        var imageType = module.TypeImage(
            uintType,
            SpirvImageDim.Dim2D,
            depth: false,
            arrayed: false,
            multisampled: false,
            sampled: 1,
            SpirvImageFormat.R32ui);
        var sampledImageType = module.TypeSampledImage(imageType);
        var sampledImagePointer =
            module.TypePointer(SpirvStorageClass.UniformConstant, sampledImageType);

        var attribute = module.AddGlobalVariable(inputVec4Pointer, SpirvStorageClass.Input);
        module.AddName(attribute, "attr0");
        module.AddDecoration(attribute, SpirvDecoration.Location, 0);

        var texture = module.AddGlobalVariable(
            sampledImagePointer,
            SpirvStorageClass.UniformConstant);
        module.AddName(texture, "tex0");
        module.AddDecoration(texture, SpirvDecoration.DescriptorSet, 0);
        module.AddDecoration(texture, SpirvDecoration.Binding, 1);

        var output = module.AddGlobalVariable(outputVec4Pointer, SpirvStorageClass.Output);
        module.AddName(output, "outColor");
        module.AddDecoration(output, SpirvDecoration.Location, 0);

        var functionType = module.TypeFunction(voidType);
        var main = module.BeginFunction(voidType, functionType);
        module.AddName(main, "main");
        module.AddLabel();

        var attributeValue = module.AddInstruction(SpirvOp.Load, vec4Type, attribute);
        var uv = module.AddInstruction(
            SpirvOp.VectorShuffle,
            vec2Type,
            attributeValue,
            attributeValue,
            0,
            1);
        var sampledImage = module.AddInstruction(SpirvOp.Load, sampledImageType, texture);
        var image = module.AddInstruction(SpirvOp.Image, imageType, sampledImage);
        var lod = module.Constant(uintType, 0);
        var size = module.AddInstruction(SpirvOp.ImageQuerySizeLod, uvec2Type, image, lod);
        var sizeX = module.AddInstruction(SpirvOp.CompositeExtract, uintType, size, 0);
        var sizeY = module.AddInstruction(SpirvOp.CompositeExtract, uintType, size, 1);
        var sizeXf = module.AddInstruction(SpirvOp.ConvertUToF, floatType, sizeX);
        var sizeYf = module.AddInstruction(SpirvOp.ConvertUToF, floatType, sizeY);
        var uvX = module.AddInstruction(SpirvOp.CompositeExtract, floatType, uv, 0);
        var uvY = module.AddInstruction(SpirvOp.CompositeExtract, floatType, uv, 1);
        var coordXf = module.AddInstruction(SpirvOp.FMul, floatType, uvX, sizeXf);
        var coordYf = module.AddInstruction(SpirvOp.FMul, floatType, uvY, sizeYf);
        var coordX = module.AddInstruction(SpirvOp.ConvertFToU, uintType, coordXf);
        var coordY = module.AddInstruction(SpirvOp.ConvertFToU, uintType, coordYf);
        var one = module.Constant(uintType, 1);
        var maxX = module.AddInstruction(SpirvOp.ISub, uintType, sizeX, one);
        var maxY = module.AddInstruction(SpirvOp.ISub, uintType, sizeY, one);
        var boolType = module.TypeBool();
        var tooWide = module.AddInstruction(SpirvOp.UGreaterThan, boolType, coordX, maxX);
        var tooTall = module.AddInstruction(SpirvOp.UGreaterThan, boolType, coordY, maxY);
        var clampedX = module.AddInstruction(SpirvOp.Select, uintType, tooWide, maxX, coordX);
        var clampedY = module.AddInstruction(SpirvOp.Select, uintType, tooTall, maxY, coordY);
        var coords = module.AddInstruction(
            SpirvOp.CompositeConstruct,
            uvec2Type,
            clampedX,
            clampedY);
        // ImageOperands Lod = 0x2
        var fetched = module.AddInstruction(
            SpirvOp.ImageFetch,
            uvec4Type,
            image,
            coords,
            2,
            lod);
        var packedLe = module.AddInstruction(SpirvOp.CompositeExtract, uintType, fetched, 0);

        // bswap32 so BE guest/readback layout matches CPU packed10 decode.
        var maskFf = module.Constant(uintType, 0xFFu);
        var maskFf00 = module.Constant(uintType, 0xFF00u);
        var eight = module.Constant(uintType, 8);
        var twentyFour = module.Constant(uintType, 24);
        var b0 = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, packedLe, maskFf);
        b0 = module.AddInstruction(SpirvOp.ShiftLeftLogical, uintType, b0, twentyFour);
        var b1 = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, packedLe, maskFf00);
        b1 = module.AddInstruction(SpirvOp.ShiftLeftLogical, uintType, b1, eight);
        var b2 = module.AddInstruction(SpirvOp.ShiftRightLogical, uintType, packedLe, eight);
        b2 = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, b2, maskFf00);
        var b3 = module.AddInstruction(SpirvOp.ShiftRightLogical, uintType, packedLe, twentyFour);
        var packed = module.AddInstruction(SpirvOp.BitwiseOr, uintType, b0, b1);
        packed = module.AddInstruction(SpirvOp.BitwiseOr, uintType, packed, b2);
        packed = module.AddInstruction(SpirvOp.BitwiseOr, uintType, packed, b3);

        var ten = module.Constant(uintType, 10);
        var twenty = module.Constant(uintType, 20);
        var thirty = module.Constant(uintType, 30);
        var mask10 = module.Constant(uintType, 0x3FFu);
        var mask2 = module.Constant(uintType, 0x3u);
        var c0 = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, packed, mask10);
        var c1Shift = module.AddInstruction(SpirvOp.ShiftRightLogical, uintType, packed, ten);
        var c1 = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, c1Shift, mask10);
        var c2Shift = module.AddInstruction(SpirvOp.ShiftRightLogical, uintType, packed, twenty);
        var c2 = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, c2Shift, mask10);
        var aBits = module.AddInstruction(SpirvOp.ShiftRightLogical, uintType, packed, thirty);
        aBits = module.AddInstruction(SpirvOp.BitwiseAnd, uintType, aBits, mask2);

        uint redBits;
        uint blueBits;
        if (redInLeastSignificantBits)
        {
            redBits = c0;
            blueBits = c2;
        }
        else
        {
            redBits = c2;
            blueBits = c0;
        }

        var inv1023 = module.ConstantFloat(floatType, 1f / 1023f);
        var inv3 = module.ConstantFloat(floatType, 1f / 3f);
        var red = module.AddInstruction(SpirvOp.ConvertUToF, floatType, redBits);
        red = module.AddInstruction(SpirvOp.FMul, floatType, red, inv1023);
        var green = module.AddInstruction(SpirvOp.ConvertUToF, floatType, c1);
        green = module.AddInstruction(SpirvOp.FMul, floatType, green, inv1023);
        var blue = module.AddInstruction(SpirvOp.ConvertUToF, floatType, blueBits);
        blue = module.AddInstruction(SpirvOp.FMul, floatType, blue, inv1023);
        var alpha = module.AddInstruction(SpirvOp.ConvertUToF, floatType, aBits);
        alpha = module.AddInstruction(SpirvOp.FMul, floatType, alpha, inv3);
        var color = module.AddInstruction(
            SpirvOp.CompositeConstruct,
            vec4Type,
            red,
            green,
            blue,
            alpha);
        module.AddStatement(SpirvOp.Store, output, color);
        module.AddStatement(SpirvOp.Return);
        module.EndFunction();

        module.AddEntryPoint(
            SpirvExecutionModel.Fragment,
            main,
            "main",
            [attribute, texture, output]);
        module.AddExecutionMode(main, SpirvExecutionMode.OriginUpperLeft);
        _ = ivec2Type;
        return module.Build();
    }

    public static byte[] CreateSolidFragment(float red, float green, float blue, float alpha)
    {
        var module = new SpirvModuleBuilder();
        module.AddCapability(SpirvCapability.Shader);

        var voidType = module.TypeVoid();
        var floatType = module.TypeFloat(32);
        var vec4Type = module.TypeVector(floatType, 4);
        var outputVec4Pointer = module.TypePointer(SpirvStorageClass.Output, vec4Type);
        var output = module.AddGlobalVariable(outputVec4Pointer, SpirvStorageClass.Output);
        module.AddName(output, "outColor");
        module.AddDecoration(output, SpirvDecoration.Location, 0);

        var functionType = module.TypeFunction(voidType);
        var main = module.BeginFunction(voidType, functionType);
        module.AddName(main, "main");
        module.AddLabel();
        var color = module.ConstantComposite(
            vec4Type,
            module.ConstantFloat(floatType, red),
            module.ConstantFloat(floatType, green),
            module.ConstantFloat(floatType, blue),
            module.ConstantFloat(floatType, alpha));
        module.AddStatement(SpirvOp.Store, output, color);
        module.AddStatement(SpirvOp.Return);
        module.EndFunction();

        module.AddEntryPoint(SpirvExecutionModel.Fragment, main, "main", [output]);
        module.AddExecutionMode(main, SpirvExecutionMode.OriginUpperLeft);
        return module.Build();
    }

    /// <summary>
    /// Diagnostic fragment stage that exposes one interpolated vertex output
    /// directly as color. This keeps the real guest vertex/index/depth path
    /// intact while isolating fragment-shader translation from interface data.
    /// </summary>
    public static byte[] CreateAttributeFragment(uint location)
    {
        var module = new SpirvModuleBuilder();
        module.AddCapability(SpirvCapability.Shader);

        var voidType = module.TypeVoid();
        var floatType = module.TypeFloat(32);
        var vec4Type = module.TypeVector(floatType, 4);
        var inputPointer = module.TypePointer(SpirvStorageClass.Input, vec4Type);
        var outputPointer = module.TypePointer(SpirvStorageClass.Output, vec4Type);
        var input = module.AddGlobalVariable(inputPointer, SpirvStorageClass.Input);
        module.AddName(input, $"attr{location}");
        module.AddDecoration(input, SpirvDecoration.Location, location);
        var output = module.AddGlobalVariable(outputPointer, SpirvStorageClass.Output);
        module.AddName(output, "outColor");
        module.AddDecoration(output, SpirvDecoration.Location, 0);

        var functionType = module.TypeFunction(voidType);
        var main = module.BeginFunction(voidType, functionType);
        module.AddName(main, "main");
        module.AddLabel();
        var value = module.AddInstruction(SpirvOp.Load, vec4Type, input);
        module.AddStatement(SpirvOp.Store, output, value);
        module.AddStatement(SpirvOp.Return);
        module.EndFunction();

        module.AddEntryPoint(
            SpirvExecutionModel.Fragment,
            main,
            "main",
            [input, output]);
        module.AddExecutionMode(main, SpirvExecutionMode.OriginUpperLeft);
        return module.Build();
    }

    /// <summary>
    /// Minimal fragment stage for fixed-function depth-only passes.  The
    /// guest has no pixel shader and therefore cannot export colour; keeping
    /// this stage output-free preserves that contract while allowing Vulkan
    /// to run early/late depth tests for the translated vertex shader.
    /// </summary>
    public static byte[] CreateDepthOnlyFragment()
    {
        var module = new SpirvModuleBuilder();
        module.AddCapability(SpirvCapability.Shader);

        var voidType = module.TypeVoid();
        var functionType = module.TypeFunction(voidType);
        var main = module.BeginFunction(voidType, functionType);
        module.AddName(main, "main");
        module.AddLabel();
        module.AddStatement(SpirvOp.Return);
        module.EndFunction();

        module.AddEntryPoint(SpirvExecutionModel.Fragment, main, "main", []);
        module.AddExecutionMode(main, SpirvExecutionMode.OriginUpperLeft);
        return module.Build();
    }
}
