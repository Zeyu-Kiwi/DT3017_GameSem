# Global PSX effects toggle

Use **Tools > PSX Effects > Enabled** in Unity to switch all PSX effects together.

The same default can be changed with **Effects Enabled** on
`Assets/PSXShader/Resources/PSXEffectsSettings.asset`. The game loads this asset
before the first scene. Changes through the Tools menu during Play mode are
temporary; leaving Play mode restores the saved default.

For a runtime settings menu, connect its bool callback to:

```csharp
Game.Visuals.PSXEffects.SetEnabled(enabled);
```

`PSXEffects.Enabled` reads the current state, and `PSXEffects.Toggle()` flips it.
No scene component or material references are required. New materials and objects
automatically use the current global state.

The toggle bypasses vertex snapping, UV pixelation, color quantization, dithering,
and screen-resolution sampling in all three URP graphs. It also bypasses the
imported PSX Shader Kit's wobble, affine warping, custom retro lighting, flat
shading, triangle depth sorting, depth debug, and camera post-processing. Each
effect's original settings stay intact. Base textures, lighting, transparency,
emission, and fog remain part of the material's normal appearance.

Shader Graph uses a non-exposed **Global** float `_PSXEffectsDisabled`, rather
than a per-material property. Zero retains the original effects, including when
no settings have loaded yet. One bypasses them. The full-screen renderer passes
remain installed and sample the original image when disabled.

## Colour quantization and fog

Select `Assets/PSXShader/Dither.mat` to adjust the full-screen colour effect:

- **Use Perceptual Quantization** switches methods: off restores the original linear/Bayer method, on uses the new perceptual method. Original is selected by default.
- **Color Res** controls the colour levels for either method. Lower values give a stronger retro palette.
- **Original Dither Res** keeps the original method's raw dither amount, independently of the new method.
- **Dither Strength** controls only the perceptual method, from 0 (no added noise) to 1 (noise spanning one colour step). Its default is 0.15 to reduce visible grain.

`PerceptualQuantization.hlsl` contains both methods. The perceptual method converts linear scene colour to sRGB for quantization, then converts it back, preserving HDR colour above 1. The global PSX toggle bypasses either method as before.