#ifndef ECOSYSTEM_COMMON_HLSL
#define ECOSYSTEM_COMMON_HLSL

inline half Rand(half2 n)
{
    return sin(dot(n, half2(1233.224, 1743.335)));
}

inline half RandomNoise(half2 seed)
{
    return frac(sin(dot(seed, half2(127.1, 311.7))) * 43758.5453);
}

inline half RandomNoise(half x, half y)
{
    return RandomNoise(half2(x, y));
}

inline half RandomNoiseWithSpeed(half2 seed, half speed)
{
    return frac(sin(dot(seed * floor(_Time.y * speed), half2(17.13, 3.71))) * 43758.5453123);
}

inline half RandomNoiseWithSpeed(half seed, half speed)
{
    return RandomNoiseWithSpeed(half2(seed, 1.0), speed);
}

inline float4 Mod289(float4 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}
inline float3 Mod289( float3 x )
{
    return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0;
}

inline float2 Mod289( float2 x )
{
    return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0;
}

inline float3 Permute( float3 x )
{
    return Mod289( ( ( x * 34.0 ) + 1.0 ) * x );
}

inline float4 Permute(float4 x)
{
    return Mod289((x * 34.0 + 1.0) * x);
}

//SimplexNoise2D
half Snoise( float2 v )
{
    const half4 c = half4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
    half2 i = floor( v + dot( v, c.yy ) );
    half2 x0 = v - i + dot( i, c.xx );
    half2 i1;
    i1 = ( x0.x > x0.y ) ? half2( 1.0, 0.0 ) : half2( 0.0, 1.0 );
    half4 x12 = x0.xyxy + c.xxzz;
    x12.xy -= i1;
    i = Mod289( i );
    half3 p = Permute( Permute( i.y + half3( 0.0, i1.y, 1.0 ) ) + i.x + half3( 0.0, i1.x, 1.0 ) );
    half3 m = max( 0.5 - half3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
    m = m * m;
    m = m * m;
    half3 x = 2.0 * frac( p * c.www ) - 1.0;
    half3 h = abs( x ) - 0.5;
    half3 ox = floor( x + 0.5 );
    half3 a0 = x - ox;
    m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
    half3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot( m, g );
}

//SimplexNoise3D
half4 TaylorInvSqrt(half4 r)
{
    return 1.79284291400159 - 0.85373472095314 * r;
}

half Snoise3D(float3 v)
{
    const half2 C = half2(1.0 / 6.0, 1.0 / 3.0);

    // First corner
    half3 i  = floor(v + dot(v, C.yyy));
    half3 x0 = v   - i + dot(i, C.xxx);

    // Other corners
    half3 g = step(x0.yzx, x0.xyz);
    half3 l = 1.0 - g;
    half3 i1 = min(g.xyz, l.zxy);
    half3 i2 = max(g.xyz, l.zxy);

    // x1 = x0 - i1  + 1.0 * C.xxx;
    // x2 = x0 - i2  + 2.0 * C.xxx;
    // x3 = x0 - 1.0 + 3.0 * C.xxx;
    half3 x1 = x0 - i1 + C.xxx;
    half3 x2 = x0 - i2 + C.yyy;
    half3 x3 = x0 - 0.5;

    // Permutations
    i = Mod289(i); // Avoid truncation effects in permutation
    half4 p =
      Permute(Permute(Permute(i.z + half4(0.0, i1.z, i2.z, 1.0))
                            + i.y + half4(0.0, i1.y, i2.y, 1.0))
                            + i.x + half4(0.0, i1.x, i2.x, 1.0));

    // Gradients: 7x7 points over a square, mapped onto an octahedron.
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    half4 j = p - 49.0 * floor(p * (1.0 / 49.0));  // mod(p,7*7)

    half4 x_ = floor(j * (1.0 / 7.0));
    half4 y_ = floor(j - 7.0 * x_ );  // mod(j,N)

    half4 x = x_ * (2.0 / 7.0) + 0.5 / 7.0 - 1.0;
    half4 y = y_ * (2.0 / 7.0) + 0.5 / 7.0 - 1.0;

    half4 h = 1.0 - abs(x) - abs(y);

    half4 b0 = half4(x.xy, y.xy);
    half4 b1 = half4(x.zw, y.zw);

    //half4 s0 = half4(lessThan(b0, 0.0)) * 2.0 - 1.0;
    //half4 s1 = half4(lessThan(b1, 0.0)) * 2.0 - 1.0;
    half4 s0 = floor(b0) * 2.0 + 1.0;
    half4 s1 = floor(b1) * 2.0 + 1.0;
    half4 sh = -step(h, half4(0,0,0,0));

    half4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    half4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    half3 g0 = half3(a0.xy, h.x);
    half3 g1 = half3(a0.zw, h.y);
    half3 g2 = half3(a1.xy, h.z);
    half3 g3 = half3(a1.zw, h.w);

    // Normalise gradients
    half4 norm = TaylorInvSqrt(half4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
    g0 *= norm.x;
    g1 *= norm.y;
    g2 *= norm.z;
    g3 *= norm.w;

    // Mix final noise value
    half4 m = max(0.6 - half4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    m = m * m;

    half4 px = half4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
    return 42.0 * dot(m, px);
}

inline half Dither8x8Bayer( int x, int y )
{
    const half dither[ 64 ] = {
        1, 49, 13, 61,  4, 52, 16, 64,
       33, 17, 45, 29, 36, 20, 48, 32,
        9, 57,  5, 53, 12, 60,  8, 56,
       41, 25, 37, 21, 44, 28, 40, 24,
        3, 51, 15, 63,  2, 50, 14, 62,
       35, 19, 47, 31, 34, 18, 46, 30,
       11, 59,  7, 55, 10, 58,  6, 54,
       43, 27, 39, 23, 42, 26, 38, 22};
    int r = y * 8 + x;
    return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
}
#endif