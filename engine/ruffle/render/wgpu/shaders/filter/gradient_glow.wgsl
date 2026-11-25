// AQW build: the gradient glow filter (see `filters/gradient_glow.rs`).
struct Filter {
    // The 256-entry lookup table, RGBA8 (not premultiplied), four entries per vector.
    ramp: array<vec4<u32>, 64>,
    strength: f32,
    // 0: outer, 1: inner, 2: full.
    glow_type: u32,
    knockout: u32,
    _padding: u32,
}

@group(0) @binding(0) var texture: texture_2d<f32>;
@group(0) @binding(1) var texture_sampler: sampler;
@group(0) @binding(2) var<uniform> filter_args: Filter;
@group(0) @binding(3) var blurred: texture_2d<f32>;

struct VertexOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) source_uv: vec2<f32>,
    @location(1) blur_uv: vec2<f32>,
};

struct VertexInput {
    @location(0) position: vec2<f32>,
    @location(1) source_uv: vec2<f32>,
    @location(2) blur_uv: vec2<f32>,
};

@vertex
fn main_vertex(in: VertexInput) -> VertexOutput {
    let pos = vec4<f32>((in.position.x * 2.0 - 1.0), (1.0 - in.position.y * 2.0), 0.0, 1.0);
    return VertexOutput(pos, in.source_uv, in.blur_uv);
}

// The lookup table's entry for `value` (0..1), premultiplied.
fn lookup(value: f32) -> vec4<f32> {
    let index = u32(round(saturate(value) * 255.0));
    // No coverage, no glow, whatever the gradient's first colour is (Flash leaves an opaque
    // object untouched by an inner glow whose gradient starts opaque: avm2/filters_array_holes).
    if (index == 0u) {
        return vec4<f32>(0.0);
    }
    let entry = unpack4x8unorm(filter_args.ramp[index / 4u][index % 4u]);
    return vec4<f32>(entry.rgb * entry.a, entry.a);
}

@fragment
fn main_fragment(in: VertexOutput) -> @location(0) vec4<f32> {
    let knockout = filter_args.knockout > 0u;
    var blur = textureSample(blurred, texture_sampler, in.blur_uv).a;
    let dest = textureSample(texture, texture_sampler, in.source_uv);

    if (in.blur_uv.x < 0.0 || in.blur_uv.x > 1.0 || in.blur_uv.y < 0.0 || in.blur_uv.y > 1.0) {
        blur = 0.0;
    }

    if (filter_args.glow_type == 1u) {
        // Inner: inside the object, from its inverted (blurred) alpha.
        let glow = lookup((1.0 - blur) * filter_args.strength);
        if (knockout) {
            return glow * dest.a;
        }
        return glow * dest.a + dest * (1.0 - glow.a);
    }
    let glow = lookup(blur * filter_args.strength);
    if (filter_args.glow_type == 2u) {
        // Full: everywhere, on top of the object.
        if (knockout) {
            return glow;
        }
        return glow + dest * (1.0 - glow.a);
    }
    // Outer: under the object, outside it.
    if (knockout) {
        return glow * (1.0 - dest.a);
    }
    return dest + glow * (1.0 - dest.a);
}
